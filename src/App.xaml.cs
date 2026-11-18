using System;
using System.Windows;
using System.Windows.Threading;

namespace Pigsty
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                e.Exception != null ? e.Exception.Message : "Unexpected error.",
                "Pigsty",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                MessageBox.Show(
                    ex.Message,
                    "Pigsty",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
