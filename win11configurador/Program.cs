using System;
using win11configurador.coses;
// using win11configurador.Instaladors;


namespace WindowsConfigurator
{
    class Program
    {
        static void Main(string[] args)
        {
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
