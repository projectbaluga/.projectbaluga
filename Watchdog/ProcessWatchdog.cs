using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ProjectBaluga.Watchdog
{
    public static class ProcessWatchdog
    {
        private static Timer _timer;

        public static void Start(string exePath, TimeSpan? checkInterval = null)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("Executable path must be supplied.", nameof(exePath));

            var interval = checkInterval ?? TimeSpan.FromSeconds(5);
            _timer = new Timer(CheckProcess, exePath, TimeSpan.Zero, interval);
        }

        public static void Stop() => _timer?.Dispose();

        private static void CheckProcess(object state)
        {
            var exePath = (string)state;
            var processName = Path.GetFileNameWithoutExtension(exePath);

            if (Process.GetProcessesByName(processName).Length == 0)
            {
                try
                {
                    Process.Start("shutdown", "/s /t 0");
                }
                catch (Exception)
                {
                    // optional: log
                }
            }
        }
    }
}
