using System;
using System.Collections.Generic;
using System.Linq;
using win11configurador.plantillesjson;
using win11configurador.Managers;
using Spectre.Console;

namespace win11configurador.Installers
{
    public class Configurator
    {
        public void Run()
        {
            // Usar el método específico para configuraciones
            var groupedConfigs = LectorJson.LoadConfigurationsWithGroups("configuracions.json");

            if (groupedConfigs == null || groupedConfigs.Count == 0)
            {
                AnsiConsole.Write(
                    new Panel("[yellow]No se encontraron configuraciones disponibles.[/]")
                        .Border(BoxBorder.Rounded)
                        .Header("[bold red]Atención[/]", Justify.Center)
                        .Padding(1, 1));
                AnsiConsole.MarkupLine("[grey]Pulsa cualquier tecla para volver al menú principal.[/]");
                AnsiConsole.Console.Input.ReadKey(true);
                return;
            }

            // Mostrar tablas por categoría
            foreach (var group in groupedConfigs)
            {
                AnsiConsole.Write(new Rule($"[bold blue]{group.Key}[/]").RuleStyle("blue"));

                var distinctConfigs = group.Value
                    .GroupBy(p => p.Title ?? string.Empty)
                    .Select(g => g.First())
                    .ToList();

                if (distinctConfigs.Count == 0)
                {
                    AnsiConsole.MarkupLine("[italic grey]No hay configuraciones disponibles en esta categoría.[/]");
                    continue;
                }

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold]ID[/]")
                    .AddColumn("[bold]NOMBRE[/]")
                    .AddColumn("[bold]DESCRIPCION[/]")
                    .AddColumn("[bold]APLICADA ANTERIORMENTE[/]")      // Valor de PreviouslyApplied
                    .AddColumn("[bold]ESTADO[/]") // Resultado de check_command
                    .LeftAligned();

                foreach (var config in distinctConfigs)
                {
                    string status = config.PreviouslyApplied ? "[green]Aplicada[/]" : "[red]No aplicada[/]";
                    string realStatus = GetConfigRealStatus(config);

                    table.AddRow(
                        $"[cyan]{config.ObtenirID()}[/]",
                        config.Title ?? "",
                        config.Description ?? "",
                        status,
                        realStatus
                    );
                }

                AnsiConsole.Write(table);
            }

            // Preparar selección múltiple con agrupación por categorías y opción de seleccionar todas
            var configMap = new Dictionary<(string Categoria, string Titulo), ConfigurationItem>();
            var allConfigs = new List<(string Categoria, string Titulo)>();
            var categoryGroups = new Dictionary<string, List<string>>();

            foreach (var cat in groupedConfigs)
            {
                var catList = new List<string>();
                foreach (var conf in cat.Value)
                {
                    conf.Category = cat.Key;
                    var display = conf.Title ?? "";
                    configMap[(cat.Key, display)] = conf;
                    catList.Add(display);
                    allConfigs.Add((cat.Key, display));
                }
                if (catList.Count > 0)
                    categoryGroups[cat.Key] = catList;
            }

            var prompt = new MultiSelectionPrompt<string>()
                .Title("[bold]Selecciona las configuraciones a aplicar o revertir:[/]")
                .PageSize(12)
                .MoreChoicesText("[grey](Usa las flechas para navegar, [blue]<espacio>[/] para seleccionar, [green]<enter>[/] para confirmar)[/]")
                .InstructionsText("[grey](Presiona [blue]<espacio>[/] para alternar selección, [green]<enter>[/] para aceptar)[/]");

            const string todosKey = "(Selecciona solo esta casilla para aplicar todas)";
            prompt.AddChoiceGroup("Todas las configuraciones", new[] { todosKey });

            foreach (var cat in categoryGroups)
            {
                prompt.AddChoiceGroup(cat.Key, cat.Value.ToArray());
            }

            var selected = AnsiConsole.Prompt(prompt);

            List<ConfigurationItem> selectedConfigs;
            if (selected.Contains("Todas las configuraciones") || selected.Contains(todosKey))
            {
                selectedConfigs = allConfigs.Select(k => configMap[k]).Distinct().ToList();
            }
            else
            {
                selectedConfigs = selected
                    .SelectMany(sel =>
                        categoryGroups
                            .Where(cat => cat.Value.Contains(sel))
                            .Select(cat => (cat.Key, sel))
                    )
                    .Where(key => configMap.ContainsKey(key))
                    .Select(key => configMap[key])
                    .Distinct()
                    .ToList();
            }

            if (selectedConfigs.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No se seleccionaron configuraciones. Saliendo...[/]");
                return;
            }

            // Preguntar si se desea aplicar o revertir las configuraciones seleccionadas
            bool revertir = AnsiConsole.Confirm("[yellow]¿Deseas revertir las configuraciones seleccionadas en vez de aplicarlas?[/]", false);

            foreach (ConfigurationItem config in selectedConfigs)
            {
                string accion = revertir ? "Revirtiendo" : "Aplicando";
                string comando = revertir ? config.RevertCommand : config.Command;

                if (string.IsNullOrWhiteSpace(comando))
                {
                    AnsiConsole.MarkupLine($"[yellow]No hay comando de {(revertir ? "reversión" : "aplicación")} para: [bold]{config.Title}[/][/]");
                    continue;
                }

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start($"[yellow]{accion} configuración: {config.Title}[/]", ctx =>
                    {
                        PowerShellExecutor.ExecuteCommand(comando, true);
                    });
                AnsiConsole.MarkupLine($"[green]✔ {accion} configuración:[/] [bold]{config.Title}[/]");
            }

            AnsiConsole.Write(
                new Panel("[grey]Proceso finalizado. Pulsa cualquier tecla para volver al menú principal.[/]")
                    .Border(BoxBorder.Rounded)
                    .Header("[bold]Fin[/]", Justify.Center)
                    .Padding(1, 1));
            AnsiConsole.Console.Input.ReadKey(true);
            AnsiConsole.Console.Clear();
        }
        private string GetConfigRealStatus(ConfigurationItem config)
        {
            if (string.IsNullOrWhiteSpace(config.CheckCommand))
                return "[grey]No comprobable[/]";

            try
            {
                // Ejecutar el comando y obtener la salida
                string output = PowerShellExecutor.ExecuteCommand(config.CheckCommand, true)?.Trim();
                if (output != null && output.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return "[green]Aplicada[/]";
                else if (output != null && output.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return "[red]No aplicada[/]";
                else
                    return $"[yellow]{output}[/]";
            }
            catch (Exception ex)
            {
                return $"[red]Error[/]";
            }
        }
    }
}
