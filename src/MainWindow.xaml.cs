using System;
using System.Collections.Generic;
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
        private readonly IMtpDeviceService _mtpService;

        private List<AndroidDevice> _connectedDevices;
        private AndroidDevice _selectedDevice;
        private List<AndroidFileItem> _allDiscoveredFiles;
        private List<AndroidFileItem> _currentCategoryFiles;
        private CategoryType _currentCategory;
        private bool _isInitialized;
        private bool _isPopulatingCombo;

        public MainWindow()
        {
            InitializeComponent();
            _isInitialized = true;

            _mtpService = new ShellMtpDeviceService();

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
            StatusBarStatusText.Text = "Scanning...";
            RefreshDevicesButton.IsEnabled = false;

            try
            {
                var realDevices = await _mtpService.DetectDevicesAsync();

                _isPopulatingCombo = true;
                DeviceComboBox.Items.Clear();

                if (realDevices != null && realDevices.Count > 0)
                {
                    _connectedDevices = realDevices;

                    foreach (var dev in realDevices)
                    {
                        var cbi = new ComboBoxItem { Content = dev.Name, Tag = dev };
                        DeviceComboBox.Items.Add(cbi);
                    }
                    DeviceComboBox.SelectedIndex = 0;
                    _selectedDevice = realDevices[0];
                    _isPopulatingCombo = false;

                    UpdateConnectionUI(true);
                    await ScanSelectedDeviceAsync();
                }
                else
                {
                    _isPopulatingCombo = false;
                    _selectedDevice = null;
                    _allDiscoveredFiles.Clear();
                    _currentCategoryFiles.Clear();
                    UpdateConnectionUI(false);
                    StatusBarStatusText.Text = "No device";
                }
            }
            catch (Exception ex)
            {
                _isPopulatingCombo = false;
                StatusBarStatusText.Text = ex.Message;
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
                DeviceComboBox.Visibility = Visibility.Visible;

                StatusDot.Fill = (Brush)FindResource("SuccessBrush");
                StatusDot.ToolTip = "Connected";
            }
            else
            {
                NoDeviceOverlay.Visibility = Visibility.Visible;
                StorageOverviewPanel.Visibility = Visibility.Collapsed;
                DashboardView.Visibility = Visibility.Collapsed;
                CategoryFileListView.Visibility = Visibility.Collapsed;
                DeviceComboBox.Visibility = Visibility.Collapsed;

                StatusDot.Fill = (Brush)FindResource("TextMutedBrush");
                StatusDot.ToolTip = "Disconnected";

                ResetSidebarBadges();
            }
        }

        private void ResetSidebarBadges()
        {
            PhotosBadge.Text = "0";
            VideosBadge.Text = "0";
            AudioBadge.Text = "0";
            DocsBadge.Text = "0";
            ApksBadge.Text = "0";
            DownloadsBadge.Text = "0";
            ArchivesBadge.Text = "0";
            LargeFilesBadge.Text = "0";
            AllFilesBadge.Text = "0";
            StatusBarSelectionText.Text = "";
        }

        private async Task ScanSelectedDeviceAsync()
        {
            if (_selectedDevice == null) return;

            StatusBarStatusText.Text = "Indexing...";

            try
            {
                var files = await _mtpService.ScanFilesAsync(_selectedDevice, (status, pct) =>
                {
                    if (!Dispatcher.HasShutdownStarted)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            StatusBarStatusText.Text = status;
                        }));
                    }
                });

                _allDiscoveredFiles = files ?? new List<AndroidFileItem>();
                UpdateStorageOverview();
                ApplyCategoryFilter();
                StatusBarStatusText.Text = string.Format("{0} items • {1}",
                    _allDiscoveredFiles.Count, SizeFormatter.FormatBytes(_allDiscoveredFiles.Sum(f => f.SizeBytes)));
            }
            catch (Exception ex)
            {
                StatusBarStatusText.Text = ex.Message;
            }
        }

        private void UpdateStorageOverview()
        {
            if (!_isInitialized || _selectedDevice == null || DeviceTitleText == null || StorageBarGrid == null) return;

            DeviceTitleText.Text = _selectedDevice.Name;
            StorageVolumeText.Text = string.IsNullOrEmpty(_selectedDevice.StorageName) ? "" : _selectedDevice.StorageName;

            var stats = StorageStats.Calculate(_allDiscoveredFiles);

            PhotosBadge.Text = stats.PhotosCount.ToString();
            VideosBadge.Text = stats.VideosCount.ToString();
            AudioBadge.Text = stats.AudioCount.ToString();
            DocsBadge.Text = stats.DocumentsCount.ToString();
            ApksBadge.Text = stats.ApksCount.ToString();
            DownloadsBadge.Text = stats.DownloadsCount.ToString();
            ArchivesBadge.Text = stats.ArchivesCount.ToString();
            LargeFilesBadge.Text = stats.LargeFilesCount.ToString();
            AllFilesBadge.Text = stats.TotalFileCount.ToString();

            LegendPhotosText.Text = SizeFormatter.FormatBytes(stats.PhotosSizeBytes);
            LegendVideosText.Text = SizeFormatter.FormatBytes(stats.VideosSizeBytes);
            LegendAudioText.Text = SizeFormatter.FormatBytes(stats.AudioSizeBytes);
            LegendDocsText.Text = SizeFormatter.FormatBytes(stats.DocumentsSizeBytes);
            LegendApksText.Text = SizeFormatter.FormatBytes(stats.ApksSizeBytes);
            LegendDownloadsText.Text = SizeFormatter.FormatBytes(stats.DownloadsSizeBytes);
            LegendArchivesText.Text = SizeFormatter.FormatBytes(stats.ArchivesSizeBytes);

            long totalCap = _selectedDevice.TotalSpaceBytes;
            long freeSpace = Math.Max(0, _selectedDevice.FreeSpaceBytes);
            long usedSpace = _selectedDevice.UsedSpaceBytes;

            if (totalCap <= 0)
            {
                usedSpace = stats.TotalSizeBytes;
                totalCap = Math.Max(usedSpace, 1);
                freeSpace = 0;
                StorageCapacityText.Text = SizeFormatter.FormatBytes(usedSpace);
                LegendFreeText.Text = "";
            }
            else
            {
                if (usedSpace <= 0 && stats.TotalSizeBytes > 0)
                {
                    usedSpace = stats.TotalSizeBytes;
                    freeSpace = Math.Max(0, totalCap - usedSpace);
                }

                StorageCapacityText.Text = string.Format("{0} / {1}",
                    SizeFormatter.FormatBytes(usedSpace),
                    SizeFormatter.FormatBytes(totalCap));
                LegendFreeText.Text = string.Format("({0} free)", SizeFormatter.FormatBytes(freeSpace));
            }

            double totalDouble = totalCap > 0 ? (double)totalCap : 1.0;
            ColPhotos.Width = new GridLength(Math.Max(0.2, (stats.PhotosSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColVideos.Width = new GridLength(Math.Max(0.2, (stats.VideosSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColAudio.Width = new GridLength(Math.Max(0.2, (stats.AudioSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColDocs.Width = new GridLength(Math.Max(0.2, (stats.DocumentsSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColApks.Width = new GridLength(Math.Max(0.2, (stats.ApksSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColDownloads.Width = new GridLength(Math.Max(0.2, (stats.DownloadsSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColArchives.Width = new GridLength(Math.Max(0.2, (stats.ArchivesSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColOther.Width = new GridLength(Math.Max(0.2, (stats.OtherSizeBytes / totalDouble) * 100.0), GridUnitType.Star);
            ColFree.Width = new GridLength(Math.Max(0.5, (freeSpace / totalDouble) * 100.0), GridUnitType.Star);

            DashMediaSize.Text = SizeFormatter.FormatBytes(stats.PhotosSizeBytes + stats.VideosSizeBytes);
            DashMediaCount.Text = (stats.PhotosCount + stats.VideosCount).ToString();

            DashApksSize.Text = SizeFormatter.FormatBytes(stats.ApksSizeBytes);
            DashApksCount.Text = stats.ApksCount.ToString();

            DashLargeSize.Text = SizeFormatter.FormatBytes(stats.LargeFilesSizeBytes);
            DashLargeCount.Text = stats.LargeFilesCount.ToString();

            DashDownloadsSize.Text = SizeFormatter.FormatBytes(stats.DownloadsSizeBytes);
            DashDownloadsCount.Text = stats.DownloadsCount.ToString();

            var top10 = _allDiscoveredFiles
                .Where(f => !f.IsFolder)
                .OrderByDescending(f => f.SizeBytes)
                .Take(10)
                .ToList();
            TopFilesGrid.ItemsSource = top10;
        }

        private void SelectCategoryByTag(string tagName)
        {
            foreach (var item in CategoryListBox.Items)
            {
                var lbi = item as ListBoxItem;
                if (lbi != null && lbi.Tag != null && lbi.Tag.ToString() == tagName)
                {
                    CategoryListBox.SelectedItem = lbi;
                    break;
                }
            }
        }

        private void CardMedia_Click(object sender, RoutedEventArgs e)
        {
            SelectCategoryByTag("Photos");
        }

        private void CardApks_Click(object sender, RoutedEventArgs e)
        {
            SelectCategoryByTag("Apks");
        }

        private void CardLarge_Click(object sender, RoutedEventArgs e)
        {
            SelectCategoryByTag("LargeFiles");
        }

        private void CardDownloads_Click(object sender, RoutedEventArgs e)
        {
            SelectCategoryByTag("Downloads");
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
            if (_selectedDevice == null) return;

            if (_currentCategory == CategoryType.Overview)
            {
                DashboardView.Visibility = Visibility.Visible;
                CategoryFileListView.Visibility = Visibility.Collapsed;
                StatusBarSelectionText.Text = "";
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
                    CategoryViewTitle.Text = "Photos";
                    break;
                case CategoryType.Videos:
                    filtered = filtered.Where(f => f.Category == CategoryType.Videos);
                    CategoryViewIcon.Text = "🎬";
                    CategoryViewTitle.Text = "Videos";
                    break;
                case CategoryType.Audio:
                    filtered = filtered.Where(f => f.Category == CategoryType.Audio);
                    CategoryViewIcon.Text = "🎵";
                    CategoryViewTitle.Text = "Audio";
                    break;
                case CategoryType.Documents:
                    filtered = filtered.Where(f => f.Category == CategoryType.Documents);
                    CategoryViewIcon.Text = "📄";
                    CategoryViewTitle.Text = "Docs";
                    break;
                case CategoryType.Apks:
                    filtered = filtered.Where(f => f.Category == CategoryType.Apks);
                    CategoryViewIcon.Text = "📦";
                    CategoryViewTitle.Text = "APKs";
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
                    CategoryViewTitle.Text = "Archives";
                    break;
                case CategoryType.LargeFiles:
                    filtered = filtered.Where(f => f.IsLargeFile);
                    CategoryViewIcon.Text = "⚠️";
                    CategoryViewTitle.Text = "Large";
                    break;
                case CategoryType.AllFiles:
                default:
                    CategoryViewIcon.Text = "📁";
                    CategoryViewTitle.Text = "All";
                    break;
            }

            if (SearchBox != null && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string query = SearchBox.Text.Trim();
                filtered = filtered.Where(f =>
                    (f.Name != null && f.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (f.DirectoryPath != null && f.DirectoryPath.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            int sortIndex = SortComboBox != null ? SortComboBox.SelectedIndex : 0;
            switch (sortIndex)
            {
                case 1:
                    filtered = filtered.OrderByDescending(f => f.SizeBytes);
                    break;
                case 2:
                    filtered = filtered.OrderBy(f => f.SizeBytes);
                    break;
                case 3:
                    filtered = filtered.OrderByDescending(f => f.DateModified ?? DateTime.MinValue);
                    break;
                case 4:
                    filtered = filtered.OrderBy(f => f.DateModified ?? DateTime.MinValue);
                    break;
                default:
                    filtered = filtered.OrderBy(f => f.Name);
                    break;
            }

            _currentCategoryFiles = filtered.ToList();
            FilesDataGrid.ItemsSource = _currentCategoryFiles;

            long totalBytes = _currentCategoryFiles.Sum(f => f.SizeBytes);
            CategoryViewCountSize.Text = string.Format("{0} • {1}", _currentCategoryFiles.Count, SizeFormatter.FormatBytes(totalBytes));

            UpdateSelectionStatus();
        }

        private void UpdateSelectionStatus()
        {
            var selected = _currentCategoryFiles.Where(f => f.IsSelected).ToList();
            if (selected.Count > 0)
            {
                long selectedBytes = selected.Sum(f => f.SizeBytes);
                StatusBarSelectionText.Text = string.Format("{0} selected ({1})", selected.Count, SizeFormatter.FormatBytes(selectedBytes));
            }
            else
            {
                StatusBarSelectionText.Text = "";
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
            UpdateSelectionStatus();
        }

        private void HeaderCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            bool check = cb != null && cb.IsChecked.HasValue && cb.IsChecked.Value;

            foreach (var item in _currentCategoryFiles)
            {
                item.IsSelected = check;
            }
            UpdateSelectionStatus();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _currentCategoryFiles)
            {
                item.IsSelected = true;
            }
            UpdateSelectionStatus();
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _currentCategoryFiles)
            {
                item.IsSelected = false;
            }
            UpdateSelectionStatus();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = _currentCategoryFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0 && FilesDataGrid.SelectedItems.Count > 0)
            {
                selectedFiles = FilesDataGrid.SelectedItems.OfType<AndroidFileItem>().ToList();
            }

            if (selectedFiles.Count == 0)
            {
                if (_currentCategoryFiles.Count > 0)
                {
                    var dlg = new ConfirmDialog("Export All",
                        string.Format("Export all {0} files ({1})?",
                            _currentCategoryFiles.Count, SizeFormatter.FormatBytes(_currentCategoryFiles.Sum(f => f.SizeBytes))),
                        "Export", false);
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
                    return;
                }
            }

            await ExportFileListAsync(selectedFiles);
        }

        private async Task ExportFileListAsync(List<AndroidFileItem> filesToExport)
        {
            if (filesToExport == null || filesToExport.Count == 0) return;

            var folderDlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select destination folder",
                ShowNewFolderButton = true
            };

            if (folderDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(folderDlg.SelectedPath))
            {
                return;
            }

            string destFolder = folderDlg.SelectedPath;
            var progressDlg = new ProgressDialog("Exporting...");
            progressDlg.Owner = this;

            var exportTask = _mtpService.ExportFilesAsync(
                filesToExport,
                destFolder,
                (curr, tot, item) => progressDlg.UpdateProgress(curr, tot, item),
                () => progressDlg.IsCancelled);

            progressDlg.Show();
            int count = await exportTask;
            progressDlg.Close();

            StatusBarStatusText.Text = string.Format("Exported {0} to {1}", count, destFolder);
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDevice == null) return;

            var openDlg = new OpenFileDialog
            {
                Title = "Select files to import",
                Multiselect = true,
                Filter = "All Files (*.*)|*.*|Photos|*.jpg;*.jpeg;*.png;*.webp;*.heic|Videos|*.mp4;*.mkv;*.mov|Audio|*.mp3;*.m4a;*.wav;*.flac|Docs|*.pdf;*.docx;*.xlsx;*.txt|APKs|*.apk;*.xapk"
            };

            if (openDlg.ShowDialog(this) != true || openDlg.FileNames.Length == 0)
            {
                return;
            }

            await ImportFileListAsync(openDlg.FileNames.ToList());
        }

        private async Task ImportFileListAsync(List<string> filePaths)
        {
            if (_selectedDevice == null || filePaths == null || filePaths.Count == 0) return;

            string defaultTarget = "Download";
            switch (_currentCategory)
            {
                case CategoryType.Photos: defaultTarget = "DCIM"; break;
                case CategoryType.Videos: defaultTarget = "Movies"; break;
                case CategoryType.Audio: defaultTarget = "Music"; break;
                case CategoryType.Documents: defaultTarget = "Documents"; break;
            }

            var importDlg = new ImportDialog(filePaths.Count, defaultTarget);
            importDlg.Owner = this;
            if (importDlg.ShowDialog() != true || !importDlg.IsConfirmed)
            {
                return;
            }

            string targetFolder = importDlg.SelectedTargetFolder;

            var progressDlg = new ProgressDialog("Importing...");
            progressDlg.Owner = this;

            var importTask = _mtpService.ImportFilesAsync(
                _selectedDevice,
                filePaths,
                targetFolder,
                (curr, tot, item) => progressDlg.UpdateProgress(curr, tot, item),
                () => progressDlg.IsCancelled);

            progressDlg.Show();
            int importedCount = await importTask;
            progressDlg.Close();

            await ScanSelectedDeviceAsync();
            StatusBarStatusText.Text = string.Format("Imported {0} to {1}", importedCount, targetFolder);
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedFiles = _currentCategoryFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0 && FilesDataGrid.SelectedItems.Count > 0)
            {
                selectedFiles = FilesDataGrid.SelectedItems.OfType<AndroidFileItem>().ToList();
            }

            if (selectedFiles.Count == 0) return;

            await DeleteFileListAsync(selectedFiles);
        }

        private async Task DeleteFileListAsync(List<AndroidFileItem> filesToDelete)
        {
            var safeFiles = filesToDelete.Where(f => PathSanitizer.IsSafeDeletionTarget(f)).ToList();
            if (safeFiles.Count == 0)
            {
                StatusBarStatusText.Text = "Protected item cannot be deleted";
                return;
            }

            long totalBytes = safeFiles.Sum(f => f.SizeBytes);
            string msg = string.Format("Delete {0} file{1} ({2})?",
                safeFiles.Count, safeFiles.Count == 1 ? "" : "s", SizeFormatter.FormatBytes(totalBytes));

            var confirmDlg = new ConfirmDialog("Delete", msg, "Delete", true);
            confirmDlg.Owner = this;
            if (confirmDlg.ShowDialog() != true || !confirmDlg.Confirmed)
            {
                return;
            }

            var progressDlg = new ProgressDialog("Deleting...");
            progressDlg.Owner = this;

            var deleteTask = _mtpService.DeleteFilesAsync(
                safeFiles,
                (curr, tot, item) => progressDlg.UpdateProgress(curr, tot, item),
                () => progressDlg.IsCancelled);

            progressDlg.Show();
            int deletedCount = await deleteTask;
            progressDlg.Close();

            await ScanSelectedDeviceAsync();
            StatusBarStatusText.Text = string.Format("Deleted {0} ({1} freed)", deletedCount, SizeFormatter.FormatBytes(totalBytes));
        }

        private void CategoryFileListView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void CategoryFileListView_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) || _selectedDevice == null) return;

            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            var validFiles = files.Where(f => File.Exists(f)).ToList();
            if (validFiles.Count == 0) return;

            await ImportFileListAsync(validFiles);
        }

        private async void TopFileExport_Click(object sender, RoutedEventArgs e)
        {
            var selected = TopFilesGrid.SelectedItems.OfType<AndroidFileItem>().ToList();
            if (selected.Count > 0)
            {
                await ExportFileListAsync(selected);
            }
        }

        private async void TopFileDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = TopFilesGrid.SelectedItems.OfType<AndroidFileItem>().ToList();
            if (selected.Count > 0)
            {
                await DeleteFileListAsync(selected);
            }
        }

        private void TopFileCopyName_Click(object sender, RoutedEventArgs e)
        {
            var item = TopFilesGrid.SelectedItem as AndroidFileItem;
            if (item != null && !string.IsNullOrEmpty(item.Name))
            {
                Clipboard.SetText(item.Name);
                StatusBarStatusText.Text = "Copied";
            }
        }

        private void TopFileCopyPath_Click(object sender, RoutedEventArgs e)
        {
            var item = TopFilesGrid.SelectedItem as AndroidFileItem;
            if (item != null)
            {
                Clipboard.SetText(string.Format("{0}\\{1}", item.DirectoryPath, item.Name));
                StatusBarStatusText.Text = "Copied";
            }
        }

        private async void RefreshDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDevicesAsync();
        }

        private async void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _isPopulatingCombo) return;
            var selectedItem = DeviceComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                var dev = selectedItem.Tag as AndroidDevice;
                if (dev != null && dev != _selectedDevice)
                {
                    _selectedDevice = dev;
                    await ScanSelectedDeviceAsync();
                }
            }
        }

        private void CopyFileName_Click(object sender, RoutedEventArgs e)
        {
            var item = FilesDataGrid.SelectedItem as AndroidFileItem;
            if (item != null && !string.IsNullOrEmpty(item.Name))
            {
                Clipboard.SetText(item.Name);
                StatusBarStatusText.Text = "Copied";
            }
        }

        private void CopyFilePath_Click(object sender, RoutedEventArgs e)
        {
            var item = FilesDataGrid.SelectedItem as AndroidFileItem;
            if (item != null)
            {
                Clipboard.SetText(string.Format("{0}\\{1}", item.DirectoryPath, item.Name));
                StatusBarStatusText.Text = "Copied";
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            string info = "Pigsty v1.0.3\n" +
                          "Android MTP Storage Manager\n\n" +
                          "https://github.com/graythalon/pigsty.git";

            MessageBox.Show(this, info, "Pigsty", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
