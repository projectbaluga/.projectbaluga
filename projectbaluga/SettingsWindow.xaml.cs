using System;
using System.Windows;
using System.Windows.Controls;

namespace projectbaluga
{
    public partial class SettingsWindow : Window
    {
        public string HotspotUrl => HotspotUrlBox.Text;
        public string PostLoginUrl => PostLoginUrlBox.Text;
        public string LockScreenUrl => LockScreenUrlBox.Text;
        public bool IsTopmost => TopmostCheckBox.IsChecked ?? false;
        public bool EnableAutoShutdown => AutoShutdownCheckBox.IsChecked ?? false;

        public SettingsWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.NoResize;
            this.Topmost = true;

            HotspotUrlBox.Text = Properties.Settings.Default.HotspotUrl;
            PostLoginUrlBox.Text = Properties.Settings.Default.PostLoginUrl;
            LockScreenUrlBox.Text = Properties.Settings.Default.LockScreenUrl;
            TopmostCheckBox.IsChecked = Properties.Settings.Default.IsTopmost;
            ShutdownTimeoutBox.Text = Properties.Settings.Default.ShutdownTimeoutMinutes.ToString();
            AutoShutdownCheckBox.IsChecked = Properties.Settings.Default.EnableAutoShutdown;
            // Initialize the shutdown timeout box state based on the auto-shutdown setting
            ShutdownTimeoutBox.IsEnabled = AutoShutdownCheckBox.IsChecked == true;

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(HotspotUrlBox.Text) ||
                string.IsNullOrWhiteSpace(PostLoginUrlBox.Text) ||
                string.IsNullOrWhiteSpace(LockScreenUrlBox.Text))
            {
                MessageBox.Show("Please fill in all the URL fields.");
                return;
            }

            if (!Uri.TryCreate(HotspotUrlBox.Text, UriKind.Absolute, out var hotUri) ||
                (hotUri.Scheme != Uri.UriSchemeHttp && hotUri.Scheme != Uri.UriSchemeHttps) ||
                !Uri.TryCreate(PostLoginUrlBox.Text, UriKind.Absolute, out var postUri) ||
                (postUri.Scheme != Uri.UriSchemeHttp && postUri.Scheme != Uri.UriSchemeHttps) ||
                !Uri.TryCreate(LockScreenUrlBox.Text, UriKind.Absolute, out var lockUri) ||
                (lockUri.Scheme != Uri.UriSchemeHttp && lockUri.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show("Please enter valid HTTP or HTTPS URLs.");
                return;
            }

            if ((!string.IsNullOrEmpty(NewPasswordBox.Password) || !string.IsNullOrEmpty(ConfirmPasswordBox.Password)) &&
                NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Passwords do not match. Please re-enter.");
                return;
            }

            if (!int.TryParse(ShutdownTimeoutBox.Text, out int timeoutMinutes) || timeoutMinutes <= 0)
            {
                MessageBox.Show("Please enter a valid shutdown timeout (in minutes).", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Properties.Settings.Default.HotspotUrl = HotspotUrlBox.Text;
            Properties.Settings.Default.PostLoginUrl = PostLoginUrlBox.Text;
            Properties.Settings.Default.LockScreenUrl = LockScreenUrlBox.Text;
            Properties.Settings.Default.IsTopmost = TopmostCheckBox.IsChecked ?? false;
            Properties.Settings.Default.ShutdownTimeoutMinutes = timeoutMinutes;
            Properties.Settings.Default.EnableAutoShutdown = AutoShutdownCheckBox.IsChecked == true;
            Properties.Settings.Default.Save();

            if (!string.IsNullOrEmpty(NewPasswordBox.Password))
            {
                projectbaluga.Security.PasswordStore.SetPassword(NewPasswordBox.Password);
            }

            MessageBox.Show("Settings have been saved!");

            this.DialogResult = true;
            this.Close();
        }

        private void AutoShutdownCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Enable the timeout box only when auto shutdown is enabled
            ShutdownTimeoutBox.IsEnabled = AutoShutdownCheckBox.IsChecked == true;
        }
        public int ShutdownTimeoutMinutes
        {
            get
            {
                if (int.TryParse(ShutdownTimeoutBox.Text, out int result) && result > 0)
                    return result;
                return 10; // fallback/default
            }
            set => ShutdownTimeoutBox.Text = value.ToString();
        }

    }
}
