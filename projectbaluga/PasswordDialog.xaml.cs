using System.Windows;
using System.Windows.Controls;

namespace projectbaluga
{
    public partial class PasswordDialog : Window
    {
        public string Password { get; private set; }

        public PasswordDialog()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.NoResize;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Password cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Password = PasswordBox.Password;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password == Properties.Settings.Default.AdminPassword)
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Incorrect password. Access denied.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
