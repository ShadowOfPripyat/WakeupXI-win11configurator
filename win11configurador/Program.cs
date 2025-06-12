using System;
using win11configurador.Managers;
using win11configurador.Installers;
using win11configurador.plantillesjson;
using Spectre.Console; // Añadido

namespace Instalador
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            PowerShellExecutor.ExecuteCommand("ipconfig", true);
            menu();
        }

        static void menu()
        {
            bool sortir = false;
            do
            {
                Console.Clear();
                AnsiConsole.Write(
                    new FigletText("WakeupXII")
                        .Centered()
                        //.Color(Color.Cyan1)
                        );

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow]Selecciona una opción:[/]")
                        .AddChoices(new[]
                        {
                                "1. Configurar sistema Windows",
                                "2. Instalar programas con Winget",
                                "3. Instalar programas manualmente",
                                "4. Salir"
                        }));

                switch (choice)
                {
                    case "1. Configurar sistema Windows":
                        new Configurator().Run();
                        break;
                    case "2. Instalar programas con Winget":
                        new WingetInstaller().Run();
                        break;
                    case "3. Instalar programas manualmente":
                        new ManualInstaller().Run();
                        break;
                    case "4. Salir":
                        sortir = true;
                        break;
                }
            } while (!sortir);
        }
    }
}
