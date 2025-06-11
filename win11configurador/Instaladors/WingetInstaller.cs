using System.Diagnostics;
using win11configurador.plantillesjson;
using win11configurador.Managers;
using Spectre.Console;

namespace win11configurador.Installers
{
    public class WingetInstaller
    {
        public void Run()
        {           
            WingetCheckWizard(); // Comprobar si winget está instalado

            var groupedItems = LectorJson.LoadJsonWithGroups("winget.json");
            if (groupedItems == null || groupedItems.Count == 0)
            {
                AnsiConsole.Write(
                    new Panel("[yellow]No se encontraron wingets disponibles.[/]")
                        .Border(BoxBorder.Rounded)
                        .Header("[bold red]Atención[/]", Justify.Center)
                        .Padding(1, 1));
                AnsiConsole.MarkupLine("[grey]Pulsa cualquier tecla para volver al menú principal.[/]");
                AnsiConsole.Console.Input.ReadKey(true);
                return;
            }

            // Diccionario para mapear el string mostrado al objeto WingetProgram
            var programMap = new Dictionary<string, WingetProgram>();

            // Construir la estructura de grupos
            var allPrograms = new List<string>();
            var categoryGroups = new Dictionary<string, List<string>>();

            foreach (var cat in groupedItems)
            {
                var catList = new List<string>();
                foreach (var prog in cat.Value.Where(p => !p.PreviouslyInstalled))
                {
                    prog.Category = cat.Key;
                    var display = $"{prog.Name}";
                    var key = $"{cat.Key}::{display}";
                    programMap[key] = prog;
                    catList.Add(key);
                    allPrograms.Add(key);
                }
                if (catList.Count > 0)
                    categoryGroups[cat.Key] = catList;
            }

            // Prompt principal
            var prompt = new MultiSelectionPrompt<string>()
                .Title("[bold]Selecciona los programas a instalar:[/]")
                .PageSize(12)
                .MoreChoicesText("[grey](Usa las flechas para navegar, [blue]<espacio>[/] para seleccionar, [green]<enter>[/] para confirmar)[/]")
                .InstructionsText("[grey](Presiona [blue]<espacio>[/] para alternar selección, [green]<enter>[/] para aceptar)[/]");

            // Grupo raíz: Todos los programas
            prompt.AddChoiceGroup("Todos los programas", new[] { "(Selecciona solo esta casilla para instalarlos todos)" });

            // Subgrupos: categorías
            foreach (var cat in categoryGroups)
            {
                prompt.AddChoiceGroup(cat.Key, cat.Value.Select(p => p).ToArray());
            }

            // Mostrar el prompt
            var selected = AnsiConsole.Prompt(prompt);

            // Si el usuario selecciona "Todos los programas", selecciona todos los programas
            List<WingetProgram> selectedPrograms;
            if (selected.Contains("Todos los programas"))
            {
                selectedPrograms = allPrograms.Select(k => programMap[k]).Distinct().ToList();
            }
            else
            {
                selectedPrograms = selected
                    .Where(s => programMap.ContainsKey(s))
                    .Select(s => programMap[s])
                    .Distinct()
                    .ToList();
            }

            if (selectedPrograms.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No se seleccionaron programas. Saliendo...[/]");
                return;
            }

            foreach (WingetProgram program in selectedPrograms)
            {
                try
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start($"[yellow]Instalando: {program.Name}[/]", ctx =>
                        {
                            string idpaquet = program.WingetPackageId.ToString();
                            PowerShellExecutor.ExecuteCommand($"winget install --id {idpaquet} --exact --silent", true);
                        });
                    AnsiConsole.MarkupLine($"[green]✔ Instalado:[/] [bold]{program.Name}[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error instalando {program.Name}: {ex.Message}[/]");
                }
            }

            AnsiConsole.Write(
                new Panel("[grey]Proceso finalizado. Pulsa cualquier tecla para volver al menú principal.[/]")
                    .Border(BoxBorder.Rounded)
                    .Header("[bold]Fin[/]", Justify.Center)
                    .Padding(1, 1));
            AnsiConsole.Console.Input.ReadKey(true);
            AnsiConsole.Console.Clear();
        }



        private void WingetCheckWizard()
        {
            // Comprobar si winget está instalado
            if (!IsWingetInstalled())
            {
                AnsiConsole.Write(
                    new Panel("[red]Winget no está instalado o no se encuentra en el PATH.[/]")
                        .Border(BoxBorder.Rounded)
                        .Header("[bold red]Winget no encontrado[/]", Justify.Center)
                        .Padding(1, 1));

                bool instalar = AnsiConsole.Confirm("[yellow]¿Quieres instalar Winget automáticamente?[/]", true);

                if (instalar)
                {
                    var exito = InstalarWingetAutom();
                    if (!exito)
                    {
                        AnsiConsole.MarkupLine("[red]No se pudo instalar Winget automáticamente. Instálalo manualmente desde la Microsoft Store (App Installer).[/]");
                        AnsiConsole.MarkupLine("[grey]Pulsa cualquier tecla para volver al menú principal.[/]");
                        AnsiConsole.Console.Input.ReadKey(true);
                        AnsiConsole.Console.Clear();
                        return;
                    }
                    else
                    {
                        // Esperar unos segundos y volver a comprobar
                        AnsiConsole.MarkupLine("[green]Winget instalado correctamente. Reinicia la aplicación para continuar.[/]");
                        AnsiConsole.MarkupLine("[grey]Pulsa cualquier tecla para salir.[/]");
                        AnsiConsole.Console.Input.ReadKey(true);
                        AnsiConsole.Console.Clear();
                        return;
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]No se instaló Winget. Pulsa cualquier tecla para volver al menú principal.[/]");
                    AnsiConsole.Console.Input.ReadKey(true);
                    AnsiConsole.Console.Clear();
                    return;
                }
            }
        }
        private bool IsWingetInstalled()
        {
            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(2000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Intenta instalar Winget automáticamente usando PowerShell y la Microsoft Store.
        /// </summary>
        /// <returns>true si la instalación fue exitosa, false en caso contrario.</returns>
        private bool InstalarWingetAutom()
        {
            try
            {
                // Comando para instalar App Installer (que incluye winget) desde la Microsoft Store
                // Este comando requiere Windows 10 1809 o superior y acceso a la Store
                string psCommand = "Start-Process -FilePath 'ms-windows-store://pdp/?productid=9NBLGGH4NNS1'";

                // Intentar instalar de forma silenciosa usando PowerShellExecutor si tienes un método para ello
                // Si no, abrir la Store para que el usuario instale manualmente
                PowerShellExecutor.ExecuteCommand(psCommand, true);

                // Esperar unos segundos para dar tiempo a la instalación
                AnsiConsole.MarkupLine("[yellow]Se ha abierto la Microsoft Store para instalar Winget (App Installer). Instálalo y reinicia la aplicación.[/]");
                return false; // No se puede automatizar completamente la instalación desde la Store
            }
            catch
            {
                return false;
            }
        }
    }
}
