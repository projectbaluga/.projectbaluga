using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace projectbaluga
{
    public partial class PasswordDialog : Window
    {
        public string Password { get; private set; }
        private static int failedAttempts = 0;
        private static DateTime lockoutEnd = DateTime.MinValue;
        private static readonly string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "projectbaluga", "unlock.log");
        private const long MaxLogSizeBytes = 1 * 1024 * 1024; // 1 MB

        public PasswordDialog()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.NoResize;
            this.Topmost = true;

            if (DateTime.Now < lockoutEnd)
            {
                MessageBox.Show("Too many failed attempts. Please try again later.", "Locked", MessageBoxButton.OK, MessageBoxImage.Warning);
                DialogResult = false;
                Close();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (projectbaluga.Security.PasswordStore.VerifyPassword(PasswordBox.Password))
            {
                LogAttempt(true);
                failedAttempts = 0;
                Password = PasswordBox.Password;
                DialogResult = true;
                Close();
            }
            else
            {
                failedAttempts++;
                LogAttempt(false);
                if (failedAttempts >= 3)
                {
                    lockoutEnd = DateTime.Now.AddSeconds(30);
                }
                MessageBox.Show("Incorrect password. Access denied.", "Warning", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (projectbaluga.Security.PasswordStore.VerifyPassword(PasswordBox.Password))
            {
                failedAttempts = 0;
                LogAttempt(true);
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            }
            else
            {
                failedAttempts++;
                LogAttempt(false);
                if (failedAttempts >= 3)
                {
                    lockoutEnd = DateTime.Now.AddSeconds(30);
                }
                MessageBox.Show("Incorrect password. Access denied.", "Warning", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }

        private void LogAttempt(bool success)
        {
            var dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                if (File.Exists(logPath) && new FileInfo(logPath).Length > MaxLogSizeBytes)
                {
                    var archivePath = logPath + ".1";
                    if (File.Exists(archivePath))
                    {
                        File.Delete(archivePath);
                    }
                    File.Move(logPath, archivePath);
                }
            }
            catch (IOException)
            {
                // Ignore rotation failures
            }

            File.AppendAllText(logPath, $"{DateTime.Now:u} - {(success ? "Success" : "Fail")}{Environment.NewLine}");
        }
    }
}
