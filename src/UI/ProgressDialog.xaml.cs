using System;
using System.Windows;
using System.Windows.Input;

namespace Pigsty.UI
{
    public partial class ProgressDialog : Window
    {
        private volatile bool _isCancelled;

        public bool IsCancelled
        {
            get { return _isCancelled; }
        }

        public ProgressDialog(string title)
        {
            InitializeComponent();
            OperationTitleBlock.Text = title;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        public void UpdateProgress(int current, int total, string currentItem)
        {
            if (Dispatcher.HasShutdownStarted) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isCancelled) return;
                if (total > 0)
                {
                    TaskProgressBar.Value = Math.Min(100, (current * 100) / total);
                    StatusDetailBlock.Text = string.Format("{0} / {1} • {2}", current, total, currentItem);
                }
                else
                {
                    StatusDetailBlock.Text = currentItem;
                }
            }));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;
            CancelButton.IsEnabled = false;
            StatusDetailBlock.Text = "Stopping...";
        }
    }
}
