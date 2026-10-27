using System;
using System.Windows;

namespace Pigsty.UI
{
    public partial class ProgressDialog : Window
    {
        public bool IsCancelled { get; private set; }

        public ProgressDialog(string title)
        {
            InitializeComponent();
            OperationTitleBlock.Text = title;
        }

        public void UpdateProgress(int current, int total, string currentItem)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (total > 0)
                {
                    TaskProgressBar.Value = Math.Min(100, (current * 100) / total);
                    StatusDetailBlock.Text = string.Format("Processing {0} of {1}: {2}", current, total, currentItem);
                }
                else
                {
                    StatusDetailBlock.Text = currentItem;
                }
            }));
        }

        public void SetIndeterminate(bool isIndeterminate)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                TaskProgressBar.IsIndeterminate = isIndeterminate;
            }));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            CancelButton.IsEnabled = false;
            StatusDetailBlock.Text = "Cancelling...";
        }
    }
}
