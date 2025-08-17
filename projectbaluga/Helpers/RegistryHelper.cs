using System;
using Microsoft.Win32;

namespace projectbaluga.Helpers
{
    public static class RegistryHelper
    {
        private const string KeyPath = @"SYSTEM\\CurrentControlSet\\Services\\NlaSvc\\Parameters\\Internet";
        private const string ValueName = "EnableActiveProbing";

        public static void SetActiveProbingDisabled(bool disable)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true))
                {
                    if (key != null)
                    {
                        int value = disable ? 0 : 1;
                        key.SetValue(ValueName, value, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception)
            {
                // Ignore exceptions
            }
        }
    }
}
