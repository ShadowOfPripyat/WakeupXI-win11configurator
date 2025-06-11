using System;
using System.Diagnostics;

namespace win11configurador.Managers
{
    public static class PowerShellExecutor
    {
        public static string ExecuteCommand(string command, bool returnOutput = true)
        {
            try
            {
                var processInfo = new ProcessStartInfo("powershell.exe", $"-Command \"{command}\"")
                {
                    RedirectStandardOutput = returnOutput,
                    RedirectStandardError = returnOutput,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return "No se pudo iniciar el proceso de PowerShell.";
                }

                if (returnOutput)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit(); // Espera a que el proceso termine

                    if (!string.IsNullOrWhiteSpace(output))
                        return output;
                    else
                        return "Error: " + error;
                }
                else
                {
                    process.WaitForExit(); // Espera a que el proceso termine
                    return string.Empty;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return "Acceso denegado al intentar ejecutar PowerShell. Ejecuta la aplicación como administrador.";
            }
            catch (Exception ex)
            {
                return $"Error ejecutando PowerShell: {ex.Message}";
            }
        }
    }
}
