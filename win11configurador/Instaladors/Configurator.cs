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
            var groupedConfigs = LectorJson.LoadConfigurationsWithGroups("configuracions.json");

            if (groupedConfigs == null || groupedConfigs.Count == 0)
            {
                AnsiConsole.Write(
                    new Panel("[yellow]No se encontraron configuraciones disponibles.\nDebe haber un error en el archivo 'configuracions.json'.[/]")
                        .Border(BoxBorder.Rounded)
                        .Header("[bold red]Atención[/]", Justify.Center)
                        .Padding(1, 1));
                AnsiConsole.MarkupLine("[grey]Pulsa cualquier tecla para volver al menú principal.[/]");
                AnsiConsole.Console.Input.ReadKey(true);
                return;
            }

            // Mostrar tablas por categoría (igual que antes)
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

                // Comprobar estado real con spinner
                var realStatuses = new Dictionary<string, string>();
                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start("[yellow]Comprobando estado real de las configuraciones...[/]", ctx =>
                    {
                        foreach (var config in distinctConfigs)
                        {
                            realStatuses[config.ObtenirID()] = GetConfigRealStatus(config);
                        }
                    });

                Table table = new Table()
                    .Border(TableBorder.Rounded)
                    .AddColumn("[bold]ID[/]")
                    .AddColumn("[bold]NOMBRE[/]")
                    .AddColumn("[bold]DESCRIPCION[/]")
                    .AddColumn("[bold]TIPO OPERACION[/]")
                    .AddColumn("[bold]APLICADA ANTERIORMENTE[/]")
                    .AddColumn("[bold]ESTADO[/]")
                    .LeftAligned();

                int tableWidth = Math.Max(60, AnsiConsole.Console.Profile.Width - 4); // Mínimo 60 para evitar tablas demasiado pequeñas
                table.Width(tableWidth);

                foreach (var config in distinctConfigs)
                {
                    string status = config.PreviouslyApplied ? "[green]SI[/]" : "[red]NO[/]";
                    string realStatus = realStatuses.TryGetValue(config.ObtenirID(), out var rs) ? rs : "[grey]Desconocido[/]";
                    table.AddRow(
                        $"[cyan]{Markup.Escape(config.ObtenirID())}[/]",
                        Markup.Escape(config.Title ?? ""),
                        Markup.Escape(config.Description ?? ""),
                        Markup.Escape(config.OperationType ?? ""),
                        status,
                        realStatus
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
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

            // Marcar por defecto las configuraciones ya aplicadas
            var preselected = allConfigs
                .Where(k => configMap[k].PreviouslyApplied)
                .Select(k => k.Item2)
                .ToList();

            foreach (var cat in categoryGroups)
            {
                foreach (var display in cat.Value)
                {
                    if (preselected.Contains(display))
                        prompt.Select(display);
                }
            }

            var selected = AnsiConsole.Prompt(prompt);
            // Si el usuario seleccionó solo la opción para seleccionar todas, seleccionamos todo manualmente
            if (selected.Contains(todosKey) && selected.Count == 1)
            {
                selected = allConfigs.Select(k => k.Titulo).Distinct().ToList();
            }

            // Determinar qué configuraciones estaban aplicadas antes
            var appliedBefore = allConfigs
                .Where(k => configMap[k].PreviouslyApplied)
                .ToList();

            // Determinar seleccionadas y deseleccionadas
            var selectedKeys = selected
                .SelectMany(sel =>
                    categoryGroups
                        .Where(cat => cat.Value.Contains(sel))
                        .Select(cat => (cat.Key, sel))
                )
                .ToHashSet();

            var toApply = allConfigs
                .Where(k => selectedKeys.Contains(k) && !configMap[k].PreviouslyApplied)
                .Select(k => configMap[k])
                .Distinct()
                .ToList();

            var toRevert = appliedBefore
                .Where(k => !selectedKeys.Contains(k))
                .Select(k => configMap[k])
                .Distinct()
                .ToList();

            if (toApply.Count == 0 && toRevert.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No se seleccionaron cambios. Saliendo...[/]");
                return;
            }

            // Aplicar nuevas configuraciones
            foreach (ConfigurationItem config in toApply)
            {
                string comando = config.Command;
                if (string.IsNullOrWhiteSpace(comando))
                {
                    AnsiConsole.MarkupLine($"[yellow]No hay comando de aplicación para: [bold]{config.Title}[/][/]");
                    continue;
                }

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start($"[yellow]Aplicando configuración: {config.Title}[/]", ctx =>
                    {
                        PowerShellExecutor.ExecuteCommand(comando, true);
                    });
                AnsiConsole.MarkupLine($"[green]✔ Aplicada configuración:[/] [bold]{config.Title}[/]");
            }

            // Revertir configuraciones deseleccionadas
            foreach (ConfigurationItem config in toRevert)
            {
                string comando = config.RevertCommand;
                if (string.IsNullOrWhiteSpace(comando))
                {
                    AnsiConsole.MarkupLine($"[yellow]No hay comando de reversión para: [bold]{config.Title}[/][/]");
                    continue;
                }

                AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("yellow"))
                    .Start($"[yellow]Revirtiendo configuración: {config.Title}[/]", ctx =>
                    {
                        PowerShellExecutor.ExecuteCommand(comando, true);
                    });
                AnsiConsole.MarkupLine($"[green]✔ Revertida configuración:[/] [bold]{config.Title}[/]");
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
                string output = PowerShellExecutor.ExecuteCommand(config.CheckCommand, true)?.Trim();

                if (output != null && output.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return "[green]ACTIVADO[/]";
                else if (output != null && output.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return "[red]DESACTIVADO[/]";
                else if (!string.IsNullOrWhiteSpace(output) && output.ToLower().Contains("acceso denegado") || output.ToLower().Contains("access is denied"))
                {
                    // Preguntar al usuario si quiere elevar a administrador
                    bool elevar = AnsiConsole.Confirm("[yellow]Permiso denegado. ¿Quieres intentar ejecutar como administrador?[/]");
                    if (elevar)
                    {
                        // Aquí podrías relanzar el programa como admin o mostrar un mensaje
                        AnsiConsole.MarkupLine("[yellow]Por favor, reinicia la aplicación como administrador.[/]");
                        return "[yellow]Requiere privilegios de administrador[/]";
                    }
                    else
                    {
                        return "[yellow]Permiso denegado (no elevado)[/]";
                    }
                }
                else if (!string.IsNullOrWhiteSpace(output))
                {
                    // Escapar cualquier error para evitar markup
                    return $"[yellow]{Markup.Escape(output)}[/]";
                }
                else
                {
                    return "[grey]Desconocido[/]";
                }
            }
            catch (Exception ex)
            {
                return $"[red]Error![/] {Markup.Escape(ex.Message)}";
            }
        }
    }
}