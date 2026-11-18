using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pigsty.UI
{
    public partial class ImportDialog : Window
    {
        public string SelectedTargetFolder { get; private set; }
        public bool IsConfirmed { get; private set; }

        public ImportDialog(int fileCount, string defaultFolder = "Download")
        {
            InitializeComponent();
            HeaderLabel.Text = string.Format("Import ({0})", fileCount);
            SelectedTargetFolder = string.IsNullOrEmpty(defaultFolder) ? "Download" : defaultFolder;

            foreach (var obj in TargetFolderCombo.Items)
            {
                var cbi = obj as ComboBoxItem;
                if (cbi != null && cbi.Tag != null && cbi.Tag.ToString() == SelectedTargetFolder)
                {
                    TargetFolderCombo.SelectedItem = cbi;
                    break;
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
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
