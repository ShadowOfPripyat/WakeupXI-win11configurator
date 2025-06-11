using System;
using System.Collections.Generic;
using System.Linq;
using win11configurador.coses;
using win11configurador.Managers;

namespace WindowsConfigurator
{
    public class Configurator
    {
        public void Run()
        {
            List<WingetProgram> items = LectorJson.LoadJson<WingetProgram>("configuracions.json");

            //CONTROLA que la llista no sigui null o buida
            if (items == null || items.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine("No se encontraron configuraciones disponibles. Pulsa cualquier tecla para volver al menú principal.");
                Console.ReadKey();
                return;
            }

            IEnumerable<IGrouping<string, WingetProgram>> grouped = items.GroupBy(i => i.Category);

            foreach (IGrouping<string, WingetProgram> group in grouped)
            {
                Console.WriteLine($"\n== {group.Key} ==");
                foreach (WingetProgram item in group)
                {
                    Console.WriteLine($"{item.GetDisplayId()}: {item.Name} [{(item.PreviouslyInstalled ? "Done" : "NoFet")}]");
                }
            }

            Console.WriteLine("\nEnter IDs to execute (comma-separated, e.g., e01,e03,e00): ");
            string[] input = Console.ReadLine().Split(',');

            foreach (string id in input)
            {
                IEnumerable<WingetProgram> matches = id.EndsWith("00")
                    ? items.Where(i => i.GetDisplayId().StartsWith(id[0].ToString()) && !i.PreviouslyInstalled)
                    : items.Where(i => i.GetDisplayId() == id && !i.PreviouslyInstalled);

                foreach (WingetProgram match in matches)
                {
                    Console.WriteLine($"Executing: {match.Name}");
                    PowerShellExecutor.ExecuteCommand(match.WingetPackageId);
                }
            }
        }
    }
}
