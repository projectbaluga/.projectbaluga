using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ProjectBaluga.Watchdog
{
    public static class ProcessWatchdog
    {
        private static Timer _timer;

        private class WatchdogState
        {
            public int ProcessId { get; set; }
            public string ExePath { get; }

            public WatchdogState(int processId, string exePath)
            {
                ProcessId = processId;
                ExePath = exePath;
            }
        }

        public static void Start(string exePath, TimeSpan? checkInterval = null)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("Executable path must be supplied.", nameof(exePath));

            if (!File.Exists(exePath))
                throw new FileNotFoundException("Executable not found.", exePath);

            _timer?.Dispose();

            var interval = checkInterval ?? TimeSpan.FromSeconds(5);

            var processName = Path.GetFileNameWithoutExtension(exePath);
            var processId = 0;

            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (string.Equals(process.MainModule.FileName, exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        processId = process.Id;
                        break;
                    }
                }
                catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
                {
                    // some processes may not allow inspection of MainModule
                }
                finally
                {
                    process.Dispose();
                }
            }

            var state = new WatchdogState(processId, exePath);
            _timer = new Timer(CheckProcess, state, TimeSpan.Zero, interval);
        }

        public static void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private static void CheckProcess(object state)
        {
            var info = (WatchdogState)state;
            var exePath = info.ExePath;
            var processName = Path.GetFileNameWithoutExtension(exePath);

            bool processExists = false;

            if (info.ProcessId > 0)
            {
                try
                {
                    using var process = Process.GetProcessById(info.ProcessId);
                    if (string.Equals(process.MainModule.FileName, exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        processExists = true;
                    }
                }
                catch (Exception ex) when (ex is ArgumentException || ex is Win32Exception || ex is InvalidOperationException)
                {
                    // process not found or inaccessible
                }
            }

            if (!processExists)
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        if (string.Equals(process.MainModule.FileName, exePath, StringComparison.OrdinalIgnoreCase))
                        {
                            info.ProcessId = process.Id;
                            processExists = true;
                            break;
                        }
                    }
                    catch (Exception ex) when (ex is Win32Exception || ex is InvalidOperationException)
                    {
                        // some processes may not allow inspection of MainModule
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }

            if (!processExists)
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
