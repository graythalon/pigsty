using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Pigsty.Helpers;
using Pigsty.Models;
using Pigsty.Services;
using Pigsty.UI;

namespace Pigsty
{
    public partial class MainWindow : Window
    {
        private readonly ShellMtpDeviceService _mtpService;
        private IMtpDeviceService _currentService;

        private List<AndroidDevice> _connectedDevices;
        private AndroidDevice _selectedDevice;
        private List<AndroidFileItem> _allDiscoveredFiles;
        private List<AndroidFileItem> _currentCategoryFiles;
        private CategoryType _currentCategory;
        private bool _isInitialized;

        public MainWindow()
        {
            InitializeComponent();
            _isInitialized = true;

            _mtpService = new ShellMtpDeviceService();
            _currentService = _mtpService;

            _allDiscoveredFiles = new List<AndroidFileItem>();
            _currentCategoryFiles = new List<AndroidFileItem>();
            _currentCategory = CategoryType.Overview;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshDevicesAsync();
        }

        public async Task RefreshDevicesAsync()
        {
            StatusBarStatusText.Text = "Scanning for connected Android devices...";
            RefreshDevicesButton.IsEnabled = false;

            try
            {
                var realDevices = await Task.Factory.StartNew(new Func<List<AndroidDevice>>(() => _mtpService.DetectDevices()));

                DeviceComboBox.Items.Clear();

                if (realDevices != null && realDevices.Count > 0)
                {
                    _connectedDevices = realDevices;
                    _currentService = _mtpService;

                    foreach (var dev in realDevices)
                    {
                        var cbi = new ComboBoxItem { Content = dev.Name, Tag = dev };
                        DeviceComboBox.Items.Add(cbi);
                    }
                    DeviceComboBox.SelectedIndex = 0;
                    UpdateConnectionUI(true);
                }
                else
                {
                    _selectedDevice = null;
                    _allDiscoveredFiles.Clear();
                    _currentCategoryFiles.Clear();
                    UpdateConnectionUI(false);
                    StatusBarStatusText.Text = "No Android device detected. Connect via USB and choose 'File Transfer / MTP'.";
                    RefreshDevicesButton.IsEnabled = true;
                    return;
                }

                await ScanSelectedDeviceAsync();
            }
            catch (Exception ex)
            {
                StatusBarStatusText.Text = string.Format("Error scanning devices: {0}", ex.Message);
            }
            finally
            {
                RefreshDevicesButton.IsEnabled = true;
            }
        }

        private void UpdateConnectionUI(bool isConnected)
        {
            if (isConnected)
            {
                NoDeviceOverlay.Visibility = Visibility.Collapsed;
                StorageOverviewPanel.Visibility = Visibility.Visible;

                StatusLabel.Text = "Connected";
                StatusLabel.Foreground = (Brush)FindResource("SuccessBrush");
                StatusDot.Fill = (Brush)FindResource("SuccessBrush");
                ConnectionBadge.Background = new SolidColorBrush(Color.FromArgb(40, 16, 185, 129));
                ConnectionBadge.BorderBrush = (Brush)FindResource("SuccessBrush");
            }
            else
            {
                NoDeviceOverlay.Visibility = Visibility.Visible;
                StorageOverviewPanel.Visibility = Visibility.Collapsed;
                DashboardView.Visibility = Visibility.Collapsed;
                CategoryFileListView.Visibility = Visibility.Collapsed;

                StatusLabel.Text = "No Device";
                StatusLabel.Foreground = (Brush)FindResource("TextMutedBrush");
                StatusDot.Fill = (Brush)FindResource("TextMutedBrush");
                ConnectionBadge.Background = (Brush)FindResource("PanelAltBrush");
                ConnectionBadge.BorderBrush = (Brush)FindResource("BorderDarkBrush");
            }
        }

        private async Task ScanSelectedDeviceAsync()
        {
            if (_selectedDevice == null) return;

            StatusBarStatusText.Text = string.Format("Reading storage structure on {0}...", _selectedDevice.Name);

            try
            {
                var files = await _currentService.ScanFilesAsync(_selectedDevice, (status, pct) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        StatusBarStatusText.Text = status;
                    }));
                });

                _allDiscoveredFiles = files ?? new List<AndroidFileItem>();
                UpdateStorageOverview();
                ApplyCategoryFilter();
                StatusBarStatusText.Text = string.Format("Ready • {0} items indexed ({1})",
                    _allDiscoveredFiles.Count, SizeFormatter.FormatBytes(_allDiscoveredFiles.Sum(f => f.SizeBytes)));
            }
            catch (Exception ex)
            {
                StatusBarStatusText.Text = string.Format("Scan error: {0}", ex.Message);
            }
        }

        private void UpdateStorageOverview()
        {
            if (!_isInitialized || _selectedDevice == null || DeviceTitleText == null || StorageBarGrid == null) return;

            DeviceTitleText.Text = _selectedDevice.Name;
            StorageVolumeText.Text = string.IsNullOrEmpty(_selectedDevice.StorageName) ? "Internal shared storage" : _selectedDevice.StorageName;

            var stats = StorageStats.Calculate(_allDiscoveredFiles);

            // Update Badges in Sidebar
            PhotosBadge.Text = stats.PhotosCount.ToString();
            VideosBadge.Text = stats.VideosCount.ToString();
            AudioBadge.Text = stats.AudioCount.ToString();
            DocsBadge.Text = stats.DocumentsCount.ToString();
            ApksBadge.Text = stats.ApksCount.ToString();
            DownloadsBadge.Text = stats.DownloadsCount.ToString();
            ArchivesBadge.Text = stats.ArchivesCount.ToString();
            LargeFilesBadge.Text = stats.LargeFilesCount.ToString();
            AllFilesBadge.Text = stats.TotalFileCount.ToString();

            // Update Legend texts
            LegendPhotosText.Text = SizeFormatter.FormatBytes(stats.PhotosSizeBytes);
            LegendVideosText.Text = SizeFormatter.FormatBytes(stats.VideosSizeBytes);
            LegendAudioText.Text = SizeFormatter.FormatBytes(stats.AudioSizeBytes);
            LegendDocsText.Text = SizeFormatter.FormatBytes(stats.DocumentsSizeBytes);
            LegendApksText.Text = SizeFormatter.FormatBytes(stats.ApksSizeBytes);
            LegendDownloadsText.Text = SizeFormatter.FormatBytes(stats.DownloadsSizeBytes);
            LegendArchivesText.Text = SizeFormatter.FormatBytes(stats.ArchivesSizeBytes);

            // Storage capacity summary
            long totalCap = _selectedDevice.TotalSpaceBytes;
            if (totalCap <= 0) totalCap = 64L * 1024L * 1024L * 1024L;

            long freeSpace = _selectedDevice.FreeSpaceBytes;
            if (freeSpace < 0) freeSpace = 0;

            long usedSpace = _selectedDevice.UsedSpaceBytes;
            if (usedSpace <= 0 && stats.TotalSizeBytes > 0)
            {
                usedSpace = stats.TotalSizeBytes;
                freeSpace = Math.Max(0, totalCap - usedSpace);
            }

            StorageCapacityText.Text = string.Format("{0} used of {1} ({2} free)",
                SizeFormatter.FormatBytes(usedSpace),
                SizeFormatter.FormatBytes(totalCap),
                SizeFormatter.FormatBytes(freeSpace));
            LegendFreeText.Text = SizeFormatter.FormatBytes(freeSpace);

            // Calculate Proportions for Storage Bar
            double totalDouble = totalCap > 0 ? (double)totalCap : 1.0;
            ColPhotos.Width = new GridLength(Math.Max(0.5, (stats.PhotosSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColVideos.Width = new GridLength(Math.Max(0.5, (stats.VideosSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColAudio.Width = new GridLength(Math.Max(0.5, (stats.AudioSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColDocs.Width = new GridLength(Math.Max(0.5, (stats.DocumentsSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColApks.Width = new GridLength(Math.Max(0.5, (stats.ApksSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColDownloads.Width = new GridLength(Math.Max(0.5, (stats.DownloadsSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColArchives.Width = new GridLength(Math.Max(0.5, (stats.ArchivesSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColOther.Width = new GridLength(Math.Max(0.5, (stats.OtherSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColFree.Width = new GridLength(Math.Max(1.0, (freeSpace / totalDouble) * 100.0), GridUnitType.Star);

            // Dashboard cards
            DashMediaSize.Text = SizeFormatter.FormatBytes(stats.PhotosSizeBytes + stats.VideosSizeBytes);
            DashMediaCount.Text = string.Format("{0} photos & videos", stats.PhotosCount + stats.VideosCount);

            DashApksSize.Text = SizeFormatter.FormatBytes(stats.ApksSizeBytes);
            DashApksCount.Text = string.Format("{0} package files", stats.ApksCount);

            DashLargeSize.Text = SizeFormatter.FormatBytes(stats.LargeFilesSizeBytes);
            DashLargeCount.Text = string.Format("{0} files > 50MB", stats.LargeFilesCount);

            DashDownloadsSize.Text = SizeFormatter.FormatBytes(stats.DownloadsSizeBytes);
            DashDownloadsCount.Text = string.Format("{0} files in Download", stats.DownloadsCount);

            // Top 10 largest files for dashboard
            var top10 = _allDiscoveredFiles
                .Where(f => !f.IsFolder)
                .OrderByDescending(f => f.SizeBytes)
                .Take(10)
                .ToList();
            TopFilesGrid.ItemsSource = top10;
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            var selectedItem = CategoryListBox.SelectedItem as ListBoxItem;
            if (selectedItem == null || selectedItem.Tag == null) return;

            string tag = selectedItem.Tag.ToString();
            CategoryType cat;

            if (Enum.TryParse(tag, out cat))
            {
                _currentCategory = cat;
            }
            else
            {
                _currentCategory = CategoryType.Overview;
            }

            ApplyCategoryFilter();
        }

        private void ApplyCategoryFilter()
        {
            if (!_isInitialized || DashboardView == null || CategoryFileListView == null) return;
            if (_currentCategory == CategoryType.Overview)
            {
                DashboardView.Visibility = Visibility.Visible;
                CategoryFileListView.Visibility = Visibility.Collapsed;
                return;
            }

            DashboardView.Visibility = Visibility.Collapsed;
            CategoryFileListView.Visibility = Visibility.Visible;

            IEnumerable<AndroidFileItem> filtered = _allDiscoveredFiles;

            switch (_currentCategory)
            {
                case CategoryType.Photos:
                    filtered = filtered.Where(f => f.Category == CategoryType.Photos);
                    CategoryViewIcon.Text = "📸";
                    CategoryViewTitle.Text = "Photos & Images";
                    break;
                case CategoryType.Videos:
                    filtered = filtered.Where(f => f.Category == CategoryType.Videos);
                    CategoryViewIcon.Text = "🎬";
                    CategoryViewTitle.Text = "Videos & Movies";
                    break;
                case CategoryType.Audio:
                    filtered = filtered.Where(f => f.Category == CategoryType.Audio);
                    CategoryViewIcon.Text = "🎵";
                    CategoryViewTitle.Text = "Music & Audio";
                    break;
                case CategoryType.Documents:
                    filtered = filtered.Where(f => f.Category == CategoryType.Documents);
                    CategoryViewIcon.Text = "📄";
                    CategoryViewTitle.Text = "Documents & PDFs";
                    break;
                case CategoryType.Apks:
                    filtered = filtered.Where(f => f.Category == CategoryType.Apks);
                    CategoryViewIcon.Text = "📦";
                    CategoryViewTitle.Text = "APKs & Installers";
                    break;
                case CategoryType.Downloads:
                    filtered = filtered.Where(f => f.Category == CategoryType.Downloads ||
                        (f.DirectoryPath != null && f.DirectoryPath.IndexOf("Download", StringComparison.OrdinalIgnoreCase) >= 0));
                    CategoryViewIcon.Text = "📥";
                    CategoryViewTitle.Text = "Downloads";
                    break;
                case CategoryType.Archives:
                    filtered = filtered.Where(f => f.Category == CategoryType.Archives);
                    CategoryViewIcon.Text = "🗃️";
                    CategoryViewTitle.Text = "Archives (.zip/.rar)";
                    break;
                case CategoryType.LargeFiles:
                    filtered = filtered.Where(f => f.IsLargeFile);
                    CategoryViewIcon.Text = "⚠️";
                    CategoryViewTitle.Text = "Large Files (> 50MB)";
                    break;
                case CategoryType.AllFiles:
                default:
                    CategoryViewIcon.Text = "📁";
                    CategoryViewTitle.Text = "All Files";
                    break;
            }

            // Search filtering
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string query = SearchBox.Text.Trim();
                filtered = filtered.Where(f =>
                    (f.Name != null && f.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (f.DirectoryPath != null && f.DirectoryPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            // Sorting
            int sortIndex = SortComboBox != null ? SortComboBox.SelectedIndex : 0;
            switch (sortIndex)
            {
                case 1: // Size largest first
                    filtered = filtered.OrderByDescending(f => f.SizeBytes);
                    break;
                case 2: // Size smallest first
                    filtered = filtered.OrderBy(f => f.SizeBytes);
                    break;
                case 3: // Date newest first
                    filtered = filtered.OrderByDescending(f => f.DateModified ?? DateTime.MinValue);
                    break;
                case 4: // Date oldest first
                    filtered = filtered.OrderBy(f => f.DateModified ?? DateTime.MinValue);
                    break;
                default: // Name
                    filtered = filtered.OrderBy(f => f.Name);
                    break;
            }

            _currentCategoryFiles = filtered.ToList();
            FilesDataGrid.ItemsSource = _currentCategoryFiles;

            long totalBytes = _currentCategoryFiles.Sum(f => f.SizeBytes);
            CategoryViewCountSize.Text = string.Format("{0} items • {1}", _currentCategoryFiles.Count, SizeFormatter.FormatBytes(totalBytes));

            UpdateSelectionBanner();
        }

        private void UpdateSelectionBanner()
        {
            var selected = _currentCategoryFiles.Where(f => f.IsSelected).ToList();
            if (selected.Count > 0)
            {
                long selectedBytes = selected.Sum(f => f.SizeBytes);
                SelectionBanner.Visibility = Visibility.Visible;
                SelectionBannerText.Text = string.Format("✓ {0} file{1} selected ({2})",
                    selected.Count, selected.Count == 1 ? "" : "s", SizeFormatter.FormatBytes(selectedBytes));
                StatusBarSelectionText.Text = string.Format("{0} selected ({1})", selected.Count, SizeFormatter.FormatBytes(selectedBytes));
            }
            else
            {
                SelectionBanner.Visibility = Visibility.Collapsed;
                StatusBarSelectionText.Text = "0 items selected";
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            ApplyCategoryFilter();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            ApplyCategoryFilter();
        }

        private void FileCheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectionBanner();
        }

        private void HeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            bool check = cb != null && cb.IsChecked.HasValue && cb.IsChecked.Value;

            foreach (var item in _currentCategoryFiles)
            {
                item.IsSelected = check;
            }
            UpdateSelectionBanner();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _currentCategoryFiles)
            {
                item.IsSelected = true;
            }
            UpdateSelectionBanner();
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _currentCategoryFiles)
            {
                item.IsSelected = false;
            }
            UpdateSelectionBanner();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = _currentCategoryFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0)
            {
                if (_currentCategoryFiles.Count > 0)
                {
                    var dlg = new ConfirmDialog("Export Category",
                        string.Format("No individual files selected. Do you want to export all {0} file(s) in this category ({1})?",
                            _currentCategoryFiles.Count, SizeFormatter.FormatBytes(_currentCategoryFiles.Sum(f => f.SizeBytes))),
                        "Export All", false);
                    dlg.Owner = this;
                    if (dlg.ShowDialog() == true && dlg.Confirmed)
                    {
                        selectedFiles = _currentCategoryFiles;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    MessageBox.Show(this, "No files available to export.", "Pigsty", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            // Prompt user for destination folder on PC
            var folderDlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select destination folder on PC to export selected Android files",
                ShowNewFolderButton = true
            };

            if (folderDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(folderDlg.SelectedPath))
            {
                return;
            }

            string destFolder = folderDlg.SelectedPath;
            var progressDlg = new ProgressDialog("Exporting files to PC...");
            progressDlg.Owner = this;

            var exportTask = _currentService.ExportFilesAsync(selectedFiles, destFolder, (curr, tot, item) =>
            {
                progressDlg.UpdateProgress(curr, tot, item);
            });

            progressDlg.Show();
            int count = await exportTask;
            progressDlg.Close();

            StatusBarStatusText.Text = string.Format("Exported {0} file(s) to {1}", count, destFolder);

            var result = MessageBox.Show(this,
                string.Format("Successfully exported {0} file(s) to:\n{1}\n\nDo you want to open the destination folder in Windows Explorer?", count, destFolder),
                "Export Complete", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                Process.Start("explorer.exe", destFolder);
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null)
            {
                MessageBox.Show(this, "No Android device is currently selected.", "Pigsty", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var openDlg = new OpenFileDialog
            {
                Title = "Select files from PC to import to Android device",
                Multiselect = true,
                Filter = "All Files (*.*)|*.*|Photos & Images|*.jpg;*.png;*.webp;*.heic|Videos|*.mp4;*.mkv;*.mov|Music & Audio|*.mp3;*.m4a;*.wav;*.flac|Documents|*.pdf;*.docx;*.xlsx;*.txt|APKs|*.apk;*.xapk"
            };

            if (openDlg.ShowDialog(this) != true || openDlg.FileNames.Length == 0)
            {
                return;
            }

            var selectedFilePaths = openDlg.FileNames.ToList();

            // Ask for target folder on phone
            var importDlg = new ImportDialog(selectedFilePaths.Count);
            importDlg.Owner = this;
            if (importDlg.ShowDialog() != true || !importDlg.IsConfirmed)
            {
                return;
            }

            string targetFolder = importDlg.SelectedTargetFolder;

            var progressDlg = new ProgressDialog("Importing files to Android...");
            progressDlg.Owner = this;

            var importTask = _currentService.ImportFilesAsync(_selectedDevice, selectedFilePaths, targetFolder, (curr, tot, item) =>
            {
                progressDlg.UpdateProgress(curr, tot, item);
            });

            progressDlg.Show();
            int importedCount = await importTask;
            progressDlg.Close();

            StatusBarStatusText.Text = string.Format("Imported {0} file(s) to Android ({1})", importedCount, targetFolder);

            MessageBox.Show(this,
                string.Format("Successfully imported {0} file(s) to '{1}' on your Android device.", importedCount, targetFolder),
                "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            await ScanSelectedDeviceAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = _currentCategoryFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one file to delete.", "Pigsty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // SAFETY CHECK: Filter out protected system and directory items
            var safeFilesToDelete = selectedFiles.Where(f => PathSanitizer.IsSafeDeletionTarget(f)).ToList();
            if (safeFilesToDelete.Count == 0)
            {
                MessageBox.Show(this, "The selected item(s) cannot be deleted because they are protected system folders or directories. Only files can be deleted.", "Protected Target", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            long totalBytes = safeFilesToDelete.Sum(f => f.SizeBytes);
            string msg = string.Format("Permanently delete {0} file(s) ({1}) from your Android device?\n\nThis will free up internal storage on your phone. This action cannot be undone.",
                safeFilesToDelete.Count, SizeFormatter.FormatBytes(totalBytes));

            if (safeFilesToDelete.Count < selectedFiles.Count)
            {
                msg = string.Format("Notice: {0} protected folder(s) were excluded from deletion.\n\n" + msg,
                    selectedFiles.Count - safeFilesToDelete.Count);
            }

            var confirmDlg = new ConfirmDialog("Confirm Permanent Deletion", msg, "Delete Permanently", true);
            confirmDlg.Owner = this;
            if (confirmDlg.ShowDialog() != true || !confirmDlg.Confirmed)
            {
                return;
            }

            var progressDlg = new ProgressDialog("Deleting files from Android...");
            progressDlg.Owner = this;

            var deleteTask = _currentService.DeleteFilesAsync(safeFilesToDelete, (curr, tot, item) =>
            {
                progressDlg.UpdateProgress(curr, tot, item);
            });

            progressDlg.Show();
            int deletedCount = await deleteTask;
            progressDlg.Close();

            StatusBarStatusText.Text = string.Format("Deleted {0} file(s), freed {1}", deletedCount, SizeFormatter.FormatBytes(totalBytes));

            await ScanSelectedDeviceAsync();

            MessageBox.Show(this,
                string.Format("Successfully deleted {0} file(s) from device, freeing {1}.", deletedCount, SizeFormatter.FormatBytes(totalBytes)),
                "Deletion Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CategoryFileListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async void CategoryFileListView_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            if (_selectedDevice == null)
            {
                MessageBox.Show(this, "No Android device is currently selected.", "Pigsty", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var validFiles = files.Where(f => File.Exists(f)).ToList();
            if (validFiles.Count == 0) return;

            string targetFolder = "Download";
            switch (_currentCategory)
            {
                case CategoryType.Photos: targetFolder = "DCIM"; break;
                case CategoryType.Videos: targetFolder = "Movies"; break;
                case CategoryType.Audio: targetFolder = "Music"; break;
                case CategoryType.Documents: targetFolder = "Documents"; break;
                default: targetFolder = "Download"; break;
            }

            var importDlg = new ImportDialog(validFiles.Count);
            importDlg.Owner = this;
            if (importDlg.ShowDialog() != true || !importDlg.IsConfirmed)
            {
                return;
            }

            targetFolder = importDlg.SelectedTargetFolder;

            var progressDlg = new ProgressDialog("Importing dropped files to Android...");
            progressDlg.Owner = this;

            var importTask = _currentService.ImportFilesAsync(_selectedDevice, validFiles, targetFolder, (curr, tot, item) =>
            {
                progressDlg.UpdateProgress(curr, tot, item);
            });

            progressDlg.Show();
            int importedCount = await importTask;
            progressDlg.Close();

            StatusBarStatusText.Text = string.Format("Imported {0} file(s) to Android ({1})", importedCount, targetFolder);

            MessageBox.Show(this,
                string.Format("Successfully imported {0} file(s) to '{1}' on your Android device.", importedCount, targetFolder),
                "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            await ScanSelectedDeviceAsync();
        }

        private void ViewAllLargeFilesButton_Click(object sender, RoutedEventArgs e)
        {
            // Switch category list to LargeFiles
            foreach (var item in CategoryListBox.Items)
            {
                var lbi = item as ListBoxItem;
                if (lbi != null && lbi.Tag != null && lbi.Tag.ToString() == "LargeFiles")
                {
                    CategoryListBox.SelectedItem = lbi;
                    break;
                }
            }
        }

        private async void RefreshDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDevicesAsync();
        }

        private void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            var selectedItem = DeviceComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                _selectedDevice = selectedItem.Tag as AndroidDevice;
                if (_selectedDevice != null)
                {
                    UpdateStorageOverview();
                    ApplyCategoryFilter();
                }
            }
        }

        private void CopyFileName_Click(object sender, RoutedEventArgs e)
        {
            var item = FilesDataGrid.SelectedItem as AndroidFileItem;
            if (item != null && !string.IsNullOrEmpty(item.Name))
            {
                Clipboard.SetText(item.Name);
                StatusBarStatusText.Text = string.Format("Copied to clipboard: {0}", item.Name);
            }
        }

        private void CopyFilePath_Click(object sender, RoutedEventArgs e)
        {
            var item = FilesDataGrid.SelectedItem as AndroidFileItem;
            if (item != null)
            {
                string full = string.Format("{0}\\{1}", item.DirectoryPath, item.Name);
                Clipboard.SetText(full);
                StatusBarStatusText.Text = string.Format("Copied path to clipboard: {0}", full);
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            string info = "Pigsty - Android Storage Manager for Windows\n\n" +
                          "Manage files on your Android device over USB without Developer Options.\n\n" +
                          "• Native Windows MTP integration\n" +
                          "• Categorized storage view & fast file operations\n" +
                          "• Safe file export, import, and deletion\n\n" +
                          "Repository: https://github.com/graythalon/pigsty.git";

            MessageBox.Show(this, info, "About Pigsty", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
