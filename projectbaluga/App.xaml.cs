using System;
using System.IO;
using System.Windows;

namespace projectbaluga
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var dependencyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "projectbaluga.dll");
            if (!File.Exists(dependencyPath))
            {
                MessageBox.Show("Required component missing: projectbaluga.dll. Application will exit.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }
    }
}
