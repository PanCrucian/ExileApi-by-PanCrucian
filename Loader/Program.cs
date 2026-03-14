using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Loader
{
    internal class Program
    {
        private const string SingleInstanceMutexName = @"Global\ExileApiByPanCrucian.Loader";
        private const int SwRestore = 9;

        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
            if (!createdNew)
            {
                ActivateOrRestartExistingInstance();
                return;
            }

            var loader = new Loader();
            loader.Load(args);
        }

        private static void ActivateOrRestartExistingInstance()
        {
            var currentProcess = Process.GetCurrentProcess();
            var existingProcess = Process.GetProcessesByName(currentProcess.ProcessName)
                .Where(process => process.Id != currentProcess.Id)
                .OrderByDescending(SafeGetStartTimeUtc)
                .FirstOrDefault();

            if (existingProcess == null)
                return;

            if (existingProcess.MainWindowHandle != IntPtr.Zero)
            {
                ShowWindow(existingProcess.MainWindowHandle, SwRestore);
                SetForegroundWindow(existingProcess.MainWindowHandle);
                return;
            }

            try
            {
                existingProcess.Kill();
                existingProcess.WaitForExit(5000);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to terminate the old HUD process: {e.Message}", "ExileApi by PanCrucian");
                return;
            }

            try
            {
                var executablePath = currentProcess.MainModule?.FileName;
                if (string.IsNullOrEmpty(executablePath))
                    return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    $"The old HUD process was terminated, but the fresh instance failed to start: {e.Message}",
                    "ExileApi by PanCrucian");
            }
        }

        private static DateTime SafeGetStartTimeUtc(Process process)
        {
            try
            {
                return process.StartTime.ToUniversalTime();
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
