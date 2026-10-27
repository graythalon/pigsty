using System;
using System.Windows;
using System.Windows.Controls;

namespace Pigsty.UI
{
    public partial class ImportDialog : Window
    {
        public string SelectedTargetFolder { get; private set; }
        public bool IsConfirmed { get; private set; }

        public ImportDialog(int fileCount)
        {
            InitializeComponent();
            FileCountLabel.Text = string.Format("Choose destination folder on device for {0} selected file{1}:",
                fileCount, fileCount == 1 ? "" : "s");
            SelectedTargetFolder = "Download";
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = TargetFolderCombo.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                SelectedTargetFolder = selectedItem.Tag.ToString();
            }
            else
            {
                SelectedTargetFolder = "Download";
            }

            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
