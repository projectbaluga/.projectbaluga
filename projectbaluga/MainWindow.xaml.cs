using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using ProjectBaluga.Watchdog;
using projectbaluga.Helpers;

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
        private FallbackWindow fallbackWindow;

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
            RegistryHelper.SetActiveProbingDisabled(Properties.Settings.Default.DisableActiveProbing);
            ProcessWatchdog.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

            NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkAddressChanged;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ProcessWatchdog.Stop();
            fallbackWindow?.Close();
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

                    // If the machine starts without network connectivity, show the fallback UI
                    CheckInternetConnectionInstantly();
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

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow
            {
                Topmost = true,
            };

            settingsWindow.ShowDialog();
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
                CancelShutdownCountdown();
                currentState = AppState.LoggedIn;
                UpdateKeyboardHookState();
                HandlePostLogin();
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
                ShowFallback(true, GetNetworkStatusMessage());
                if (webView2?.CoreWebView2 != null)
                {
                    webView2.CoreWebView2.Navigate(LockScreenUrl);
                }
            }
            else
            {
                ShowFallback(false);
            }
        }

        private void ShowFallback(bool show, string status = null)
        {
            if (show)
            {
                if (fallbackWindow == null)
                {
                    fallbackWindow = new FallbackWindow();
                    fallbackWindow.Owner = this;
                    fallbackWindow.RetryRequested += RetryConnectivity_Click;
                }
                fallbackWindow.SetStatus(status);
                fallbackWindow.Show();
                this.Hide();
            }
            else
            {
                fallbackWindow?.Hide();
                this.Show();
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

        private string GetNetworkStatusMessage()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .ToList();
            if (interfaces.Count > 0)
            {
                var ni = interfaces[0];
                var ip = ni.GetIPProperties().UnicastAddresses.FirstOrDefault()?.Address?.ToString();
                if (!string.IsNullOrEmpty(ip))
                {
                    return $"{ni.Name} - {ip}";
                }
                return ni.Name;
            }
            return "No active network interfaces detected.";
        }
        private void NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!e.IsAvailable)
                {
                    ShowFallback(true, GetNetworkStatusMessage());
                    if (webView2?.CoreWebView2 != null)
                    {
                        webView2.CoreWebView2.Navigate(LockScreenUrl);
                    }
                }
                else
                {
                    ShowFallback(false);
                    if (IsInternetAvailable() && webView2?.CoreWebView2 != null)
                    {
                        webView2.CoreWebView2.Navigate(HotspotUrl);
                    }
                }
            });
        }

        private void RetryConnectivity_Click(object sender, RoutedEventArgs e)
        {
            if (IsInternetAvailable())
            {
                ShowFallback(false);
                if (webView2?.CoreWebView2 != null)
                {
                    webView2.CoreWebView2.Navigate(HotspotUrl);
                }
            }
            else
            {
                ShowFallback(true, GetNetworkStatusMessage());
            }
        }
        private void NetworkAddressChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                CheckInternetConnectionInstantly();
            });
        }
    }
}
