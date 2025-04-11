using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using System.Windows.Input;

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
        private string adminPassword => Properties.Settings.Default.AdminPassword;

        private AppState currentState = AppState.Startup;
        private KeyboardHook keyboardHook;
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView2();

            UpdateKeyboardHookState();

            NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkAddressChanged;
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
                    webView2.CoreWebView2.Navigate(HotspotUrl);
                    webView2.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                    webView2.CoreWebView2.ContextMenuRequested += OnContextMenuRequested;
                });

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };

            bool? result = settingsWindow.ShowDialog();

            if (result == true)
            {
                Properties.Settings.Default.HotspotUrl = settingsWindow.HotspotUrl;
                Properties.Settings.Default.PostLoginUrl = settingsWindow.PostLoginUrl;
                Properties.Settings.Default.LockScreenUrl = settingsWindow.LockScreenUrl;
                Properties.Settings.Default.AdminPassword = settingsWindow.AdminPassword;
                Properties.Settings.Default.IsTopmost = settingsWindow.IsTopmost;

                Properties.Settings.Default.Save();

                MessageBox.Show("Settings have been saved!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
                Owner = this,      
            };

            bool? result = passwordDialog.ShowDialog();

            if (result == true && passwordDialog.Password == Properties.Settings.Default.AdminPassword)
            {
                Application.Current.Shutdown();
            }
            else
            {
                 MessageBox.Show("Incorrect password.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            this.IsEnabled = true;
            isUnlocking = false;
        }
        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string currentUrl = webView2.CoreWebView2.Source;

            if (currentUrl.Contains(LockScreenUrl))
            {
                if (currentState != AppState.Locked)
                {
                    currentState = AppState.Locked;
                    this.WindowState = WindowState.Maximized;
                    this.WindowStyle = WindowStyle.None;
                    Owner = this;

                    UpdateKeyboardHookState();
                }
            }
            else if (currentUrl.Contains(PostLoginUrl))
            {
                if (currentState != AppState.LoggedIn)
                {
                    currentState = AppState.LoggedIn;
                    UpdateKeyboardHookState();
                    HandlePostLogin();
                }
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
