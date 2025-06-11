using System;
using System.Collections.Generic;
using System.Linq;
using win11configurador.coses;
using win11configurador.Managers;

namespace WindowsConfigurator
{
    public class WingetInstaller
    {
        public void Run()
        {
            List<WingetProgram> items = LectorJson.LoadJson<WingetProgram>("winget.json");
            if (items == null || items.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine("No se encontraron wingets disponibles. Pulsa cualquier tecla para volver al menú principal.");
                Console.ReadKey();
                return;
            }

            var grouped = items
                .GroupBy(i => i.Category);

            foreach (var group in grouped)
            {
                Console.WriteLine($"\n== {group.Key} ==");

                // Elimina duplicados por Name (puedes cambiarlo por otro campo si lo prefieres)
                var distinctPrograms = group
                    .GroupBy(p => p.Name)
                    .Select(g => g.First())
                    .ToList();

                int columns = 3;
                int count = 0;
                foreach (var prog in distinctPrograms)
                {
                    string status = prog.PreviouslyInstalled ? "Installed" : "NoFet";
                    Console.Write($"{prog.GetDisplayId(),-6}: {prog.Name,-25} [{status,-9}]  ");
                    count++;
                    if (count % columns == 0)
                        Console.WriteLine();
                }
                if (count % columns != 0)
                    Console.WriteLine();
            }

            Console.WriteLine("\nEnter IDs to install (comma-separated): ");
            string[] input = Console.ReadLine().Split(',');

            foreach (string id in input)
            {
                IEnumerable<WingetProgram> matches = id.EndsWith("00")
                    ? items.Where(i => i.GetDisplayId().StartsWith(id[0].ToString()) && !i.PreviouslyInstalled)
                    : items.Where(i => i.GetDisplayId() == id && !i.PreviouslyInstalled);

                foreach (WingetProgram match in matches)
                {
                    Console.WriteLine($"Installing: {match.Name}");
                    PowerShellExecutor.ExecuteCommand($"winget install --id {match.WingetPackageId} -e --silent");
                }
            }
        }
    }
}
