using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using System.Windows.Input;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using ProjectBaluga.Watchdog;

namespace projectbaluga
{
    public enum AppState
    {
        Startup,
        LoggedIn,
        Locked
    }

    public partial class MainWindow : Window
    {
        private string HotspotUrl => Properties.Settings.Default.HotspotUrl;
        private string PostLoginUrl => Properties.Settings.Default.PostLoginUrl;
        private string LockScreenUrl => Properties.Settings.Default.LockScreenUrl;
        private HashSet<string> allowedHosts;

        private AppState currentState = AppState.Startup;
        private KeyboardHook keyboardHook;
        private CancellationTokenSource shutdownCancellationToken;

        public MainWindow()
        {
            InitializeComponent();
            if (!Security.PasswordStore.IsPasswordSet)
            {
                MessageBox.Show("No admin password found. Please set a password to continue.", "Security", MessageBoxButton.OK, MessageBoxImage.Warning);
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
                if (!Security.PasswordStore.IsPasswordSet)
                {
                    Application.Current.Shutdown();
                    return;
                }
            }
            ValidateUrls();
            InitializeWebView2();
            ShowDesktopIcons();
            UpdateKeyboardHookState();
            DeviceManagerHelper.EnableAllMouseDevices();
            ProcessWatchdog.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

            NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkAddressChanged;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ProcessWatchdog.Stop();
        }

        private void ValidateUrls()
        {
            var urls = new[] { HotspotUrl, PostLoginUrl, LockScreenUrl };
            allowedHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var url in urls)
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    MessageBox.Show($"Invalid URL or unsupported scheme: {url}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
                allowedHosts.Add(uri.Host);
            }
        }
        private void UpdateKeyboardHookState()
        {
            if (currentState == AppState.LoggedIn)
            {
                keyboardHook?.StopHook();
                keyboardHook = null;
            }
            else
            {
                if (keyboardHook == null)
                {
                    keyboardHook = new KeyboardHook();
                    keyboardHook.StartHook();
                }
            }
        }
        private void InitializeWebView2()
        {
            webView2.EnsureCoreWebView2Async(null).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error initializing WebView2: {t.Exception.Message}"));
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    var settings = webView2.CoreWebView2.Settings;
                    settings.AreDefaultContextMenusEnabled = false;
                    settings.IsPasswordAutosaveEnabled = false;
                    settings.AreDevToolsEnabled = false;

                    webView2.CoreWebView2.Navigate(HotspotUrl);
                    webView2.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                    webView2.CoreWebView2.ContextMenuRequested += OnContextMenuRequested;
                    webView2.CoreWebView2.NavigationStarting += OnNavigationStarting;
                });

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_COMMAND = 0x111;
        private const int TOGGLE_DESKTOP_ICONS = 0x7402;
        private void ShowDesktopIcons()
        {
            IntPtr progman = FindWindow("Progman", null);
            IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (defView == IntPtr.Zero)
            {
                IntPtr workerW = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
                while (workerW != IntPtr.Zero && defView == IntPtr.Zero)
                {
                    defView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                    workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
                }
            }

            if (defView != IntPtr.Zero)
            {
                SendMessage(defView, WM_COMMAND, TOGGLE_DESKTOP_ICONS, 0);
            }
        }

        public class DeviceManagerHelper
        {
            private const int DIGCF_PRESENT = 0x00000002;
            private const int DIGCF_ALLCLASSES = 0x00000004;
            private const int DICS_ENABLE = 1;
            private const int DICS_FLAG_GLOBAL = 1;
            private const int DIF_PROPERTYCHANGE = 0x12;

            [StructLayout(LayoutKind.Sequential)]
            public struct SP_DEVINFO_DATA
            {
                public int cbSize;
                public Guid ClassGuid;
                public int DevInst;
                public IntPtr Reserved;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SP_CLASSINSTALL_HEADER
            {
                public int cbSize;
                public int InstallFunction;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SP_PROPCHANGE_PARAMS
            {
                public SP_CLASSINSTALL_HEADER ClassInstallHeader;
                public int StateChange;
                public int Scope;
                public int HwProfile;
            }

            [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SetupDiGetClassDevs(
                ref Guid ClassGuid,
                [MarshalAs(UnmanagedType.LPTStr)] string Enumerator,
                IntPtr hwndParent,
                int Flags);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiEnumDeviceInfo(
                IntPtr DeviceInfoSet,
                int MemberIndex,
                ref SP_DEVINFO_DATA DeviceInfoData);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiSetClassInstallParams(
                IntPtr DeviceInfoSet,
                ref SP_DEVINFO_DATA DeviceInfoData,
                ref SP_PROPCHANGE_PARAMS ClassInstallParams,
                int ClassInstallParamsSize);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiCallClassInstaller(
                int InstallFunction,
                IntPtr DeviceInfoSet,
                ref SP_DEVINFO_DATA DeviceInfoData);

            [DllImport("setupapi.dll", SetLastError = true)]
            public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

            public static void EnableAllMouseDevices()
            {
                Guid mouseGuid = new Guid("{4D36E96F-E325-11CE-BFC1-08002BE10318}");

                IntPtr deviceInfoSet = SetupDiGetClassDevs(
                    ref mouseGuid,
                    null,
                    IntPtr.Zero,
                    DIGCF_PRESENT);

                if (deviceInfoSet == IntPtr.Zero)
                    return;

                int index = 0;
                SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                while (SetupDiEnumDeviceInfo(deviceInfoSet, index, ref deviceInfoData))
                {
                    SP_PROPCHANGE_PARAMS propChangeParams = new SP_PROPCHANGE_PARAMS();
                    propChangeParams.ClassInstallHeader = new SP_CLASSINSTALL_HEADER
                    {
                        cbSize = Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER)),
                        InstallFunction = DIF_PROPERTYCHANGE
                    };
                    propChangeParams.StateChange = DICS_ENABLE;
                    propChangeParams.Scope = DICS_FLAG_GLOBAL;
                    propChangeParams.HwProfile = 0;

                    SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData, ref propChangeParams, Marshal.SizeOf(propChangeParams));
                    SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, deviceInfoSet, ref deviceInfoData);

                    index++;
                }

                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }
        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow
            {
                Topmost = true,
            };

            settingsWindow.ShowDialog();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();

            this.DialogResult = true;
            this.Close();
        }

        private bool isUnlocking = false;

        public void Unlock()
        {
            if (isUnlocking) return;

            isUnlocking = true;
            this.IsEnabled = false;

            var passwordDialog = new PasswordDialog
            {
                Topmost = true,
            };

            bool? result = passwordDialog.ShowDialog();

            if (result == true && projectbaluga.Security.PasswordStore.VerifyPassword(passwordDialog.Password))
            {
                Application.Current.Shutdown();
                CancelShutdownCountdown();
            }
            else
            {
                Close();
            }

            this.IsEnabled = true;
            isUnlocking = false;
        }

        private void StartShutdownCountdown()
        {
            CancelShutdownCountdown();

            shutdownCancellationToken = new CancellationTokenSource();
            var token = shutdownCancellationToken.Token;

            int timeout = Properties.Settings.Default.ShutdownTimeoutMinutes;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(timeout), token);
                    if (!token.IsCancellationRequested && Properties.Settings.Default.EnableAutoShutdown)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            System.Diagnostics.Process.Start("shutdown", "/s /t 0");
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // Shutdown was cancelled
                }
            }, token);
        }

        private void CancelShutdownCountdown()
        {
            if (shutdownCancellationToken != null)
            {
                shutdownCancellationToken.Cancel();
                shutdownCancellationToken.Dispose();
                shutdownCancellationToken = null;
            }
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string currentUrl = webView2.CoreWebView2.Source;
            Uri currentUri;
            if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out currentUri)) return;

            if (Uri.Compare(currentUri, new Uri(LockScreenUrl), UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (currentState != AppState.Locked)
                {
                    currentState = AppState.Locked;
                    this.WindowState = WindowState.Maximized;
                    this.WindowStyle = WindowStyle.None;
                    this.Topmost = true;

                    UpdateKeyboardHookState();
                    StartShutdownCountdown();
                }
            }
            else if (Uri.Compare(currentUri, new Uri(PostLoginUrl), UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (currentState != AppState.LoggedIn)
                {
                    currentState = AppState.LoggedIn;
                    UpdateKeyboardHookState();
                    HandlePostLogin();
                    CancelShutdownCountdown();
                }
            }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                !allowedHosts.Contains(uri.Host))
            {
                e.Cancel = true;
            }
        }
        private void OnContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            e.Handled = true;
        }
        private void HandlePostLogin()
        {
            this.Topmost = false;
            AdjustWindowForPostLogin();
        }
        private void AdjustWindowForPostLogin()
        {
            keyboardHook?.StopHook();
            keyboardHook = null;

            double screenHeight = SystemParameters.WorkArea.Height;
            double screenWidth = SystemParameters.WorkArea.Width;

            this.Width = 350;
            this.Height = screenHeight;
            this.WindowState = WindowState.Normal;

            this.Left = screenWidth - this.Width - 1;
            this.Top = 0;

            if (FindName("MyScrollViewer") is ScrollViewer myScrollViewer)
            {
                myScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                myScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }
        private void CheckInternetConnectionInstantly()
        {
            bool isConnected = IsInternetAvailable();

            if (!isConnected)
            {
                webView2.CoreWebView2.Navigate(LockScreenUrl);
            }
        }
        private bool IsInternetAvailable()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties properties = ni.GetIPProperties();
                    if (properties.GatewayAddresses.Count > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private void NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!e.IsAvailable)
                {
                    webView2.CoreWebView2.Navigate(LockScreenUrl);
                }
                else
                {
                    CheckInternetConnectionInstantly();
                }
            });
        }
        private void NetworkAddressChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                CheckInternetConnectionInstantly();
            });
        }
    }
    public class KeyboardHook
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(int hookHandle);

        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int hookHandle, int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public const int WH_KEYBOARD_LL = 13;
        public const int VK_LWIN = 0x5B;
        public const int VK_RWIN = 0x5C;
        public const int VK_MENU = 0x12;
        public const int VK_SHIFT = 0x10;
        public const int VK_TAB = 0x09;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_F1 = 0x70;
        public const int VK_F2 = 0x71;
        public const int VK_F3 = 0x72;
        public const int VK_F4 = 0x73;
        public const int VK_F5 = 0x74;
        public const int VK_F6 = 0x75;
        public const int VK_F7 = 0x76;
        public const int VK_F8 = 0x77;
        public const int VK_F9 = 0x78;
        public const int VK_F10 = 0x79;
        public const int VK_F11 = 0x7A;
        public const int VK_F12 = 0x7B;
        public const int VK_CONTROL = 0x11;
        private const int VK_SPACE = 0x20;

        public delegate int LowLevelKeyboardProc(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam);
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public uint dwExtraInfo;
        }

        private int _hookHandle = 0;
        private LowLevelKeyboardProc _proc;

        public void StartHook()
        {
            _proc = new LowLevelKeyboardProc(HookCallback);
            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle("user32.dll"), 0);
        }

        public void StopHook()
        {
            UnhookWindowsHookEx(_hookHandle);
        }

        private int HookCallback(int nCode, int wParam, ref KBDLLHOOKSTRUCT lParam)
        {
            if (nCode >= 0)
            {
                if (IsUnlockShortcutPressed(lParam.vkCode, wParam))
                {
                    UnlockApplication();
                    return 1;
                }

                if (IsBlockedKey(lParam.vkCode))
                {
                    return 1;
                }
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, ref lParam);
        }

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        private bool IsUnlockShortcutPressed(uint vkCode, int wParam)
        {
            if (wParam != 256) return false;

            if (vkCode == 0x55)
            {
                bool isCtrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0;
                bool isAltPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0;

                if (isCtrlPressed && isAltPressed)
                {
                    return true;
                }
            }

            return false;
        }
        private void UnlockApplication()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                (Application.Current.MainWindow as MainWindow)?.Unlock();
            });
        }
        private bool IsBlockedKey(uint vkCode)
        {
            return vkCode == VK_LWIN || vkCode == VK_RWIN || vkCode == VK_MENU ||
                   vkCode == VK_SHIFT || vkCode == VK_TAB || vkCode == VK_ESCAPE ||
                   vkCode == VK_F1 || vkCode == VK_F2 || vkCode == VK_F3 || vkCode == VK_F4 ||
                   vkCode == VK_F5 || vkCode == VK_F6 || vkCode == VK_F7 || vkCode == VK_F8 ||
                   vkCode == VK_F9 || vkCode == VK_F10 || vkCode == VK_F11 || vkCode == VK_F12 ||
                   vkCode == VK_SPACE || vkCode == VK_CONTROL;
        }
    }

}
