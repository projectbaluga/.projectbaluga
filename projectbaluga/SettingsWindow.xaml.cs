using System;
using System.Windows;
using System.Windows.Controls;

namespace projectbaluga
{
    public partial class SettingsWindow : Window
    {
        // Expose the properties to get/set the current values
        public string HotspotUrl => HotspotUrlBox.Text;
        public string PostLoginUrl => PostLoginUrlBox.Text;
        public string LockScreenUrl => LockScreenUrlBox.Text;
        public string AdminPassword => PasswordBox.Password;
        public bool IsTopmost => TopmostCheckBox.IsChecked ?? false;

        public SettingsWindow()
        {
            InitializeComponent();

            // Load the current settings when the window is opened
            HotspotUrlBox.Text = Properties.Settings.Default.HotspotUrl;
            PostLoginUrlBox.Text = Properties.Settings.Default.PostLoginUrl;
            LockScreenUrlBox.Text = Properties.Settings.Default.LockScreenUrl;
            PasswordBox.Password = Properties.Settings.Default.AdminPassword;
            TopmostCheckBox.IsChecked = Properties.Settings.Default.IsTopmost;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate the input fields before saving
            if (string.IsNullOrWhiteSpace(HotspotUrlBox.Text) ||
                string.IsNullOrWhiteSpace(PostLoginUrlBox.Text) ||
                string.IsNullOrWhiteSpace(LockScreenUrlBox.Text))
            {
                MessageBox.Show("Please fill in all the URL fields.");
                return;
            }

            Uri tempUri;

            // Validate URLs
            if (!Uri.TryCreate(HotspotUrlBox.Text, UriKind.Absolute, out tempUri) ||
                !Uri.TryCreate(PostLoginUrlBox.Text, UriKind.Absolute, out tempUri) ||
                !Uri.TryCreate(LockScreenUrlBox.Text, UriKind.Absolute, out tempUri))
            {
                MessageBox.Show("Please enter valid URLs.");
                return;
            }

            // Save the settings
            Properties.Settings.Default.HotspotUrl = HotspotUrlBox.Text;
            Properties.Settings.Default.PostLoginUrl = PostLoginUrlBox.Text;
            Properties.Settings.Default.LockScreenUrl = LockScreenUrlBox.Text;
            Properties.Settings.Default.AdminPassword = PasswordBox.Password;
            Properties.Settings.Default.IsTopmost = TopmostCheckBox.IsChecked ?? false;

            // Save settings to disk
            Properties.Settings.Default.Save();

            // Notify the user
            MessageBox.Show("Settings have been saved!");

            // Close the window and return a success result
            this.DialogResult = true;
            this.Close();
        }
    }
}
