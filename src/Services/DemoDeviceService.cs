using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Pigsty.Helpers;
using Pigsty.Models;

namespace Pigsty.Services
{
    public class DemoDeviceService : IMtpDeviceService
    {
        private List<AndroidFileItem> _cachedDemoFiles;
        private AndroidDevice _demoDevice;

        public DemoDeviceService()
        {
            InitializeDemoData();
        }

        private void InitializeDemoData()
        {
            _demoDevice = new AndroidDevice
            {
                Id = "DEMO-PIXEL-8-PRO",
                Name = "Google Pixel 8 Pro (Demo Mode)",
                StorageName = "Internal shared storage",
                IsDemo = true,
                TotalSpaceBytes = 128L * 1024L * 1024L * 1024L, // 128 GB
                FreeSpaceBytes = 43L * 1024L * 1024L * 1024L + 800L * 1024L * 1024L // ~43.8 GB free
            };

            var vol = new StorageVolumeInfo
            {
                Name = "Internal shared storage",
                TotalBytes = _demoDevice.TotalSpaceBytes,
                FreeBytes = _demoDevice.FreeSpaceBytes
            };
            _demoDevice.StorageVolumes.Add(vol);

            _cachedDemoFiles = new List<AndroidFileItem>
            {
                // Photos
                CreateFile("PXL_20261024_124500.jpg", 4L * 1024L * 1024L + 350L * 1024L, DateTime.Now.AddDays(-3), @"\Internal shared storage\DCIM\Camera", CategoryType.Photos),
                CreateFile("PXL_20261024_141022.jpg", 5L * 1024L * 1024L + 120L * 1024L, DateTime.Now.AddDays(-3), @"\Internal shared storage\DCIM\Camera", CategoryType.Photos),
                CreateFile("PXL_20261023_093012.raw", 28L * 1024L * 1024L, DateTime.Now.AddDays(-4), @"\Internal shared storage\DCIM\Camera", CategoryType.Photos),
                CreateFile("Screenshot_20261025_2145.png", 1L * 1024L * 1024L + 850L * 1024L, DateTime.Now.AddDays(-2), @"\Internal shared storage\Pictures\Screenshots", CategoryType.Photos),
                CreateFile("Screenshot_20261026_0830.png", 2L * 1024L * 1024L + 100L * 1024L, DateTime.Now.AddDays(-1), @"\Internal shared storage\Pictures\Screenshots", CategoryType.Photos),
                CreateFile("Family_Reunion_2026.heic", 3L * 1024L * 1024L + 400L * 1024L, DateTime.Now.AddDays(-10), @"\Internal shared storage\Pictures", CategoryType.Photos),
                CreateFile("Sunset_Coast_Panorama.dng", 45L * 1024L * 1024L, DateTime.Now.AddDays(-7), @"\Internal shared storage\DCIM\Camera", CategoryType.Photos),
                CreateFile("WhatsApp_IMG_20261022.jpg", 850L * 1024L, DateTime.Now.AddDays(-5), @"\Internal shared storage\Pictures\WhatsApp", CategoryType.Photos),
                CreateFile("Receipt_Scan_Oct.png", 620L * 1024L, DateTime.Now.AddDays(-6), @"\Internal shared storage\Pictures\Documents", CategoryType.Photos),
                CreateFile("Night_Sky_MilkyWay.jpg", 14L * 1024L * 1024L, DateTime.Now.AddDays(-12), @"\Internal shared storage\DCIM\Camera", CategoryType.Photos),

                // Videos
                CreateFile("VID_20261020_Vacation_4K.mp4", 1L * 1024L * 1024L * 1024L + 400L * 1024L * 1024L, DateTime.Now.AddDays(-7), @"\Internal shared storage\DCIM\Camera", CategoryType.Videos),
                CreateFile("Screenrecord_20261024_AppDemo.mp4", 142L * 1024L * 1024L, DateTime.Now.AddDays(-3), @"\Internal shared storage\Movies\Screenrecords", CategoryType.Videos),
                CreateFile("WhatsApp_Video_Party.mp4", 48L * 1024L * 1024L, DateTime.Now.AddDays(-8), @"\Internal shared storage\Movies\WhatsApp", CategoryType.Videos),
                CreateFile("Birthday_Drone_Footage.mov", 820L * 1024L * 1024L, DateTime.Now.AddDays(-15), @"\Internal shared storage\Movies", CategoryType.Videos),
                CreateFile("Interview_Presentation.mkv", 460L * 1024L * 1024L, DateTime.Now.AddDays(-11), @"\Internal shared storage\Movies", CategoryType.Videos),

                // Audio / Music
                CreateFile("Bohemian_Rhapsody.flac", 42L * 1024L * 1024L, DateTime.Now.AddDays(-30), @"\Internal shared storage\Music\Rock", CategoryType.Audio),
                CreateFile("Synthwave_Sunset_Mix.mp3", 18L * 1024L * 1024L, DateTime.Now.AddDays(-14), @"\Internal shared storage\Music\Electronic", CategoryType.Audio),
                CreateFile("Hardcore_History_Ep68.mp3", 125L * 1024L * 1024L, DateTime.Now.AddDays(-20), @"\Internal shared storage\Podcasts", CategoryType.Audio),
                CreateFile("Voice_Memo_Strategy_Meeting.m4a", 16L * 1024L * 1024L, DateTime.Now.AddDays(-2), @"\Internal shared storage\Recordings", CategoryType.Audio),
                CreateFile("Piano_Concerto_No2.flac", 58L * 1024L * 1024L, DateTime.Now.AddDays(-40), @"\Internal shared storage\Music\Classical", CategoryType.Audio),
                CreateFile("Custom_Ringtone_Marimba.wav", 1L * 1024L * 1024L + 200L * 1024L, DateTime.Now.AddDays(-50), @"\Internal shared storage\Ringtones", CategoryType.Audio),

                // Documents
                CreateFile("Annual_Financial_Summary_2026.pdf", 6L * 1024L * 1024L + 200L * 1024L, DateTime.Now.AddDays(-5), @"\Internal shared storage\Documents", CategoryType.Documents),
                CreateFile("Project_Pigsty_Architecture.docx", 2L * 1024L * 1024L + 400L * 1024L, DateTime.Now.AddDays(-1), @"\Internal shared storage\Documents", CategoryType.Documents),
                CreateFile("Q3_Metrics_Analytics.xlsx", 8L * 1024L * 1024L + 100L * 1024L, DateTime.Now.AddDays(-4), @"\Internal shared storage\Documents", CategoryType.Documents),
                CreateFile("Engineering_Handbook_v4.epub", 12L * 1024L * 1024L, DateTime.Now.AddDays(-18), @"\Internal shared storage\Documents\Books", CategoryType.Documents),
                CreateFile("Server_Configuration_Log.txt", 450L * 1024L, DateTime.Now.AddDays(-9), @"\Internal shared storage\Documents", CategoryType.Documents),
                CreateFile("Contract_Agreement_Signed.pdf", 3L * 1024L * 1024L + 150L * 1024L, DateTime.Now.AddDays(-14), @"\Internal shared storage\Documents\Legal", CategoryType.Documents),

                // APKs / Installers
                CreateFile("Nova_Launcher_Prime_v8.1.apk", 24L * 1024L * 1024L, DateTime.Now.AddDays(-6), @"\Internal shared storage\Download", CategoryType.Apks),
                CreateFile("VLC_Media_Player_3.5.4.apk", 42L * 1024L * 1024L, DateTime.Now.AddDays(-12), @"\Internal shared storage\Download", CategoryType.Apks),
                CreateFile("Termux_Terminal_0.118.apk", 96L * 1024L * 1024L, DateTime.Now.AddDays(-22), @"\Internal shared storage\Download", CategoryType.Apks),
                CreateFile("RetroArch_Emulation_Pack.xapk", 185L * 1024L * 1024L, DateTime.Now.AddDays(-15), @"\Internal shared storage\Download", CategoryType.Apks),

                // Downloads
                CreateFile("ArchLinux_ARM_2026.iso", 1L * 1024L * 1024L * 1024L + 800L * 1024L * 1024L, DateTime.Now.AddDays(-8), @"\Internal shared storage\Download", CategoryType.Downloads),
                CreateFile("Flight_Ticket_NYC_SFO.pdf", 480L * 1024L, DateTime.Now.AddDays(-3), @"\Internal shared storage\Download", CategoryType.Downloads),
                CreateFile("Dataset_Customer_Demographics.csv", 34L * 1024L * 1024L, DateTime.Now.AddDays(-10), @"\Internal shared storage\Download", CategoryType.Downloads),
                CreateFile("Camera_Firmware_Update_v2.bin", 68L * 1024L * 1024L, DateTime.Now.AddDays(-25), @"\Internal shared storage\Download", CategoryType.Downloads),

                // Archives
                CreateFile("SourceCode_Backup_October.zip", 240L * 1024L * 1024L, DateTime.Now.AddDays(-16), @"\Internal shared storage\Download", CategoryType.Archives),
                CreateFile("Family_Photos_Archive_2025.7z", 680L * 1024L * 1024L, DateTime.Now.AddDays(-45), @"\Internal shared storage\Download", CategoryType.Archives),
                CreateFile("Icons_Assets_Bundle.tar.gz", 54L * 1024L * 1024L, DateTime.Now.AddDays(-30), @"\Internal shared storage\Download", CategoryType.Archives)
            };
        }

        private AndroidFileItem CreateFile(string name, long size, DateTime date, string dir, CategoryType category)
        {
            return new AndroidFileItem
            {
                Name = name,
                SizeBytes = size,
                DateModified = date,
                DirectoryPath = dir,
                Category = category,
                IsFolder = false,
                IsSelected = false
            };
        }

        public List<AndroidDevice> DetectDevices()
        {
            return new List<AndroidDevice> { _demoDevice };
        }

        public Task<List<AndroidFileItem>> ScanFilesAsync(AndroidDevice device, Action<string, int> progressCallback)
        {
            return Task.Factory.StartNew(new Func<List<AndroidFileItem>>(() =>
            {
                if (progressCallback != null)
                {
                    progressCallback("Initializing demo device scanner...", 10);
                    System.Threading.Thread.Sleep(150);
                    progressCallback("Indexing DCIM & Pictures...", 40);
                    System.Threading.Thread.Sleep(150);
                    progressCallback("Indexing Music & Videos...", 70);
                    System.Threading.Thread.Sleep(150);
                    progressCallback("Analyzing storage breakdown...", 100);
                }

                return new List<AndroidFileItem>(_cachedDemoFiles);
            }));
        }

        public Task<int> ExportFilesAsync(List<AndroidFileItem> files, string destinationFolder, Action<int, int, string> progressCallback)
        {
            return Task.Factory.StartNew(new Func<int>(() =>
            {
                if (files == null || files.Count == 0) return 0;

                int exportedCount = 0;
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file == null) continue;

                    string safeDestPath = PathSanitizer.GetSafeExportPath(destinationFolder, file.Name);

                    if (progressCallback != null)
                    {
                        progressCallback(i + 1, files.Count, Path.GetFileName(safeDestPath));
                    }

                    string sampleInfo = string.Format("Pigsty Demo Exported File\r\nName: {0}\r\nOriginal Size: {1}\r\nDate: {2}\r\nPath: {3}",
                        file.Name, file.FormattedSize, file.FormattedDate, file.DirectoryPath);
                    File.WriteAllText(safeDestPath, sampleInfo);

                    exportedCount++;
                    System.Threading.Thread.Sleep(60);
                }

                return exportedCount;
            }));
        }

        public Task<int> ImportFilesAsync(AndroidDevice device, List<string> sourceFilePaths, string targetFolderName, Action<int, int, string> progressCallback)
        {
            return Task.Factory.StartNew(new Func<int>(() =>
            {
                if (sourceFilePaths == null || sourceFilePaths.Count == 0) return 0;

                int importedCount = 0;
                for (int i = 0; i < sourceFilePaths.Count; i++)
                {
                    string filePath = sourceFilePaths[i];
                    if (!File.Exists(filePath)) continue;

                    var fi = new FileInfo(filePath);
                    if (progressCallback != null)
                    {
                        progressCallback(i + 1, sourceFilePaths.Count, fi.Name);
                    }

                    string folder = string.Format(@"\Internal shared storage\{0}", string.IsNullOrEmpty(targetFolderName) ? "Download" : targetFolderName);
                    var category = FileFormatHelper.DetermineCategory(fi.Name, folder);

                    var newFile = CreateFile(fi.Name, fi.Length, fi.LastWriteTime, folder, category);
                    _cachedDemoFiles.Add(newFile);

                    device.FreeSpaceBytes -= fi.Length;
                    importedCount++;
                    System.Threading.Thread.Sleep(60);
                }

                return importedCount;
            }));
        }

        public Task<int> DeleteFilesAsync(List<AndroidFileItem> files, Action<int, int, string> progressCallback)
        {
            return Task.Factory.StartNew(new Func<int>(() =>
            {
                if (files == null || files.Count == 0) return 0;

                int deletedCount = 0;
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file == null) continue;

                    // STRICT SAFETY CHECK
                    if (!PathSanitizer.IsSafeDeletionTarget(file))
                    {
                        continue;
                    }

                    if (progressCallback != null)
                    {
                        progressCallback(i + 1, files.Count, file.Name);
                    }

                    _cachedDemoFiles.Remove(file);
                    _demoDevice.FreeSpaceBytes += file.SizeBytes;
                    deletedCount++;
                    System.Threading.Thread.Sleep(40);
                }

                return deletedCount;
            }));
        }
    }
}
