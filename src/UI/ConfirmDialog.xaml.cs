using System;
using System.Windows;

namespace Pigsty.UI
{
    public partial class ConfirmDialog : Window
    {
        public bool Confirmed { get; private set; }

        public ConfirmDialog(string title, string message, string confirmText = "Confirm", bool isDanger = true)
        {
            InitializeComponent();
            TitleBlock.Text = title;
            MessageBlock.Text = message;
            ConfirmButton.Content = confirmText;
            if (!isDanger)
            {
                ConfirmButton.Style = (Style)FindResource("PrimaryButtonStyle");
                IconBlock.Text = "ℹ️";
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
