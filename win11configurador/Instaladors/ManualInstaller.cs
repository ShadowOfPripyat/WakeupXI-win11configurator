using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using win11configurador.coses;
using win11configurador.Managers;

namespace WindowsConfigurator
{
    public class ManualInstaller
    {
        private readonly string downloadPath = "manualprograms"; // Folder to save downloaded files

        public void Run()
        {
            List<ManualProgram> items = LectorJson.LoadJson<ManualProgram>("programes.json");

            if (items == null || items.Count == 0)
            {
                Console.WriteLine("\nNo se encontraron programas disponibles. Pulsa cualquier tecla para volver al menú principal.");
                Console.ReadKey();
                return;
            }

            Directory.CreateDirectory(downloadPath);

            List<ManualProgram> msiList = new();
            List<ManualProgram> exeList = new();

            foreach (ManualProgram program in items.Where(i => !i.PreviouslyInstalled))
            {
                string fullPath = Path.Combine(downloadPath, program.FileName);

                // Download if not already downloaded
                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"Downloading: {program.Name}...");
                    try
                    {
                        using var client = new WebClient();
                        client.DownloadFile(program.DownloadUrl, fullPath);
                        Console.WriteLine($"Downloaded: {program.FileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to download {program.Name}: {ex.Message}");
                        continue;
                    }
                }

                // Unzip or classify
                if (fullPath.EndsWith(".zip"))
                {
                    string extractPath = Path.Combine(downloadPath, $"{program.Name}_extracted");
                    try
                    {
                        ZipFile.ExtractToDirectory(fullPath, extractPath, true);
                        string exe = Directory.GetFiles(extractPath, "*.exe", SearchOption.AllDirectories).FirstOrDefault();

                        if (exe != null)
                        {
                            Console.WriteLine($"\nFound installer: {exe}. Proceed? (y/n)");
                            if (Console.ReadLine()?.ToLower() == "y")
                                Process.Start(exe).WaitForExit();
                        }
                        else
                        {
                            Console.WriteLine($"No .exe found in extracted folder for {program.Name}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extracting {program.Name}: {ex.Message}");
                    }
                }
                else if (fullPath.EndsWith(".msi"))
                {
                    msiList.Add(program);
                }
                else if (fullPath.EndsWith(".exe"))
                {
                    exeList.Add(program);
                }
            }

            // Install MSIs silently
            foreach (var msi in msiList)
            {
                string path = Path.Combine(downloadPath, msi.FileName);
                Console.WriteLine($"Installing silently: {msi.Name}");
                Process.Start("msiexec", $"/i \"{path}\" /quiet").WaitForExit();
            }

            // Prompt user for each EXE
            foreach (var exe in exeList)
            {
                string path = Path.Combine(downloadPath, exe.FileName);
                Console.WriteLine($"\nRun installer for: {exe.Name}? (y/n)");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    Process.Start(path).WaitForExit();
                }
            }

            Console.WriteLine("\nInstalació manual completada.");
            Console.ReadKey();
        }
    }
}
