using System;
using System.Windows;

namespace projectbaluga
{
    public partial class FallbackWindow : Window
    {
        public event RoutedEventHandler RetryRequested;

        public FallbackWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string status)
        {
            StatusText.Text = status ?? string.Empty;
        }

        private void OnRetryClicked(object sender, RoutedEventArgs e)
        {
            RetryRequested?.Invoke(sender, e);
        }
    }
}
