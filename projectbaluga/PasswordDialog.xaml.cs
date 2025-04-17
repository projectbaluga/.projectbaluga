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
            this.Topmost = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Password != Properties.Settings.Default.AdminPassword)
            {
                MessageBox.Show("Incorrect password. Access denied.", "Warning", MessageBoxButton.OK, MessageBoxImage.Hand);
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
                MessageBox.Show("Incorrect password. Access denied.", "Warning", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
        }
    }
}
