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
                string.Format("Pigsty encountered an error:\n\n{0}\n\nStack:\n{1}", e.Exception.Message, e.Exception.StackTrace),
                "Pigsty Notice",
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
                    string.Format("Pigsty encountered a system error:\n\n{0}", ex.Message),
                    "Pigsty System Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
