using System.Diagnostics;

namespace win11configurador.Managers
{
    public static class PowerShellExecutor
    {
         
        public static void ExecuteCommand(string command)
        {
            ProcessStartInfo ConfiguracioDelProcess = new ProcessStartInfo("powershell.exe", command)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process InstanciaPowershell = Process.Start(ConfiguracioDelProcess);
            InstanciaPowershell.WaitForExit();
        }
    }
}
