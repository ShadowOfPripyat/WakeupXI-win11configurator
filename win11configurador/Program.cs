using System;
using win11configurador.Managers;
using win11configurador.Installers;
using win11configurador.plantillesjson;


namespace Instalador
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; // Ensure UTF-8 encoding for console output
            PowerShellExecutor.ExecuteCommand("ipconfig", true); // Example command to test PowerShell execution
            menu();
        }
        static void menu()
        {
           bool sortir =false;
            do {
                Console.Clear();
                Console.WriteLine("=== Windows Configurator ===");
                Console.WriteLine("1. Configure Windows System");
                Console.WriteLine("2. Install Programs via Winget");
                Console.WriteLine("3. Install Programs Manually");
                Console.WriteLine("4. Exit");
                Console.Write("Select an option: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        new Configurator().Run();
                        break;
                    case "2":
                        new WingetInstaller().Run();
                        break;
                    case "3":
                        new ManualInstaller().Run();
                        break;
                    case "4":
                        sortir = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        menu();
                        break;
                }
            } while (!sortir);
        }
    }
}
