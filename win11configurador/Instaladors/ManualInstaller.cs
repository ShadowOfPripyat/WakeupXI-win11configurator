using System.Diagnostics;
using System.IO.Compression;
using Spectre.Console;
using win11configurador.Managers;
using win11configurador.plantillesjson;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;

namespace win11configurador.Installers
{
    public class ManualInstaller
    {
        private readonly string downloadPath = "manualprograms";

        public async Task Run()
        {
            // Llama correctamente al método estático de LectorJson y usa la ruta completa
            var items = LectorJson.LoadManualPrograms(".\\Dades\\programes.json");

            if (items == null || items.Count == 0)
            {
                AnsiConsole.Write(
                    new Panel("[yellow]No se encontraron programas manuales disponibles.[/]")
                        .Border(BoxBorder.Rounded)
                        .Header("[bold red]Atención[/]", Justify.Center)
                        .Padding(1, 1));
                AnsiConsole.MarkupLine("[grey]Pulsa cualquier tecla para volver al menú principal.[/]");
                AnsiConsole.Console.Input.ReadKey(true);
                return;
            }

            if (!Directory.Exists(downloadPath))
                Directory.CreateDirectory(downloadPath);

            // Mostrar selección de programas
            var noInstalados = items.Where(i => !i.PreviouslyInstalled).ToList();
            if (noInstalados.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Todos los programas ya se instalaron previamente.[/]");
                return;
            }

            var prompt = new MultiSelectionPrompt<ManualProgram>()
                .Title("[bold]Selecciona los programas a instalar manualmente:[/]")
                .PageSize(12)
                .MoreChoicesText("[grey](Usa las flechas para navegar, <espacio> para seleccionar, <enter> para confirmar)[/]")
                .InstructionsText("[grey](Presiona <espacio> para alternar selección, <enter> para aceptar)[/]")
                .UseConverter(p => $"{p.Name} [grey]({p.Description})[/]");

            prompt.AddChoices(noInstalados);

            var seleccionados = AnsiConsole.Prompt(prompt);
            if (seleccionados.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No se seleccionaron programas. Saliendo...[/]");
                return;
            }

            using HttpClient client = new();

            foreach (var program in seleccionados)
            {
                string fullPath = Path.Combine(downloadPath, program.FileName);

                // Descargar si no existe
                if (!File.Exists(fullPath))
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start($"[yellow]Descargando: {program.Name}[/]", async ctx =>
                        {
                            try
                            {
                                using var response = await client.GetAsync(program.DownloadUrl);
                                response.EnsureSuccessStatusCode();
                                using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                                await response.Content.CopyToAsync(fs);
                                AnsiConsole.MarkupLine($"[green]✔ Descargado:[/] [bold]{program.FileName}[/]");
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error al descargar {program.Name}: {ex.Message}[/]");
                            }
                        });
                }

                // Procesar según extensión
                if (fullPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || fullPath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                {
                    string extractPath = Path.Combine(downloadPath, $"{Path.GetFileNameWithoutExtension(program.FileName)}_extracted");
                    try
                    {
                        if (fullPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            ZipFile.ExtractToDirectory(fullPath, extractPath, true);
                        }
                        else if (fullPath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                        {
                            using var archive = SevenZipArchive.Open(fullPath);
                            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                            {
                                entry.WriteToDirectory(extractPath, new ExtractionOptions()
                                {
                                    ExtractFullPath = true,
                                    Overwrite = true
                                });
                            }
                        }

                        // Buscar ejecutable
                        var exes = Directory.GetFiles(extractPath, "*.exe", SearchOption.AllDirectories);
                        string exeToRun = null;

                        // Buscar por nombre esperado
                        exeToRun = exes.FirstOrDefault(e => Path.GetFileName(e).Equals(program.FileName, StringComparison.OrdinalIgnoreCase));
                        if (exeToRun == null && exes.Length == 1)
                        {
                            exeToRun = exes[0];
                        }
                        else if (exeToRun == null && exes.Length > 1)
                        {
                            // Preguntar al usuario cuál ejecutar
                            exeToRun = AnsiConsole.Prompt(
                                new SelectionPrompt<string>()
                                    .Title($"[yellow]No se encontró un instalador exacto para [bold]{program.Name}[/]. ¿Cuál de estos ejecutables quieres lanzar?[/]")
                                    .AddChoices(exes)
                            );
                        }

                        if (exeToRun != null)
                        {
                            bool ejecutar = AnsiConsole.Confirm($"[yellow]¿Quieres ejecutar el instalador encontrado?[/]\n[grey]{exeToRun}[/]");
                            if (ejecutar)
                            {
                                Process.Start(new ProcessStartInfo(exeToRun) { UseShellExecute = true }).WaitForExit();
                            }
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[red]No se encontró ningún instalador ejecutable para {program.Name}.[/]");
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error extrayendo {program.Name}: {ex.Message}[/]");
                    }
                }
                else if (fullPath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start($"[yellow]Instalando silenciosamente: {program.Name}[/]", ctx =>
                        {
                            Process.Start("msiexec", $"/i \"{fullPath}\" /quiet").WaitForExit();
                        });
                    AnsiConsole.MarkupLine($"[green]✔ Instalado:[/] [bold]{program.Name}[/]");
                }
                else if (fullPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    bool ejecutar = AnsiConsole.Confirm($"[yellow]¿Quieres ejecutar el instalador para [bold]{program.Name}[/]?[/]");
                    if (ejecutar)
                    {
                        Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true }).WaitForExit();
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Formato de archivo no soportado para {program.Name}.[/]");
                }
            }

            AnsiConsole.Write(
                new Panel("[grey]Instalación manual completada. Pulsa cualquier tecla para volver al menú principal.[/]")
                    .Border(BoxBorder.Rounded)
                    .Header("[bold]Fin[/]", Justify.Center)
                    .Padding(1, 1));
            AnsiConsole.Console.Input.ReadKey(true);
            AnsiConsole.Console.Clear();
        }
    }
}
