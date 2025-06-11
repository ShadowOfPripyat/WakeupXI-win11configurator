using System;
using System.Collections.Generic;
using System.Linq;
using win11configurador.plantillesjson;
using win11configurador.Managers;
using Spectre.Console;

namespace win11configurador.Installers
{
    public class ConfiguratoroldSelectionMethod
    {
        public void Run()
        {
            List<ConfigurationItem> items = LectorJson.LoadJson<ConfigurationItem>("configuracions.json");
            if (items == null || items.Count == 0)
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

            var grouped = items.GroupBy(i => i.Category);

            foreach (var group in grouped)
            {
                AnsiConsole.Write(new Rule($"[bold blue]{group.Key}[/]").RuleStyle("blue"));

                // Elimina duplicados por Title
                List<ConfigurationItem> distinctConfigs = group
                    .GroupBy(p => p.Title)
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
                    .AddColumn("[bold]ESTADO[/]")
                    .LeftAligned();

                foreach (var config in distinctConfigs)
                {
                    string status = config.AlreadyDone ? "[green]Aplicada[/]" : "[red]No aplicada[/]";
                    table.AddRow($"[cyan]{config.ObtenirID()}[/]", config.Title, config.Description, status);
                }

                AnsiConsole.Write(table);
            }

            // Solicitar IDs a aplicar
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold]Introduce los IDs de configuración a aplicar (separados por comas):[/]")
                    .PromptStyle("green")
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(input))
            {
                AnsiConsole.MarkupLine("[yellow]No se introdujeron IDs. Saliendo...[/]");
                return;
            }

            string[] ids = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string id in ids)
            {
                IEnumerable<ConfigurationItem> matches = items.Where(i => i.ObtenirID() == id && !i.AlreadyDone);

                foreach (ConfigurationItem match in matches)
                {
                    AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow"))
                        .Start($"[yellow]Aplicando configuración: {match.Title}[/]", ctx =>
                        {
                            string comanda = match.Command.ToString();
                            //AnsiConsole.MarkupLine(comanda); // Debugging line to show the command
                            PowerShellExecutor.ExecuteCommand(match.Command, true);
                        });
                    AnsiConsole.MarkupLine($"[green]✔ Configuración aplicada:[/] [bold]{match.Title}[/]");
                }
            }
           /////
            AnsiConsole.Write(
                new Panel("[grey]Proceso finalizado. Pulsa cualquier tecla para volver al menú principal.[/]")
                    .Border(BoxBorder.Rounded)
                    .Header("[bold]Fin[/]", Justify.Center)
                    .Padding(1, 1));
            AnsiConsole.Console.Input.ReadKey(true);
            AnsiConsole.Console.Clear();
        }
    }
}
