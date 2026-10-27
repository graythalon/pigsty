using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Pigsty.Helpers;
using Pigsty.Models;

namespace Pigsty.Services
{
    public class ShellMtpDeviceService : IMtpDeviceService
    {
        private static readonly HashSet<string> SkipFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".thumbnails", ".cache", "cache", ".trash", ".trashed", "Android/data", "Android/obb", ".nomedia", ".SystemConfig"
        };

        // Shell CopyHere flags:
        // 4 = FOF_SILENT
        // 16 = FOF_NOCONFIRMATION
        // 512 = FOF_NOCONFIRMMKDIR
        // 1024 = FOF_NOERRORUI
        private const int ShellCopyFlags = 4 | 16 | 512 | 1024;

        public List<AndroidDevice> DetectDevices()
        {
            var devices = new List<AndroidDevice>();

            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return devices;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic myComputer = shell.NameSpace(17); // ssfDRIVES (This PC)
                if (myComputer == null) return devices;

                dynamic items = myComputer.Items();
                if (items == null) return devices;

                for (int i = 0; i < items.Count; i++)
                {
                    try
                    {
                        dynamic item = items.Item(i);
                        if (item == null) continue;

                        string path = Convert.ToString(item.Path) ?? "";
                        string type = Convert.ToString(item.Type) ?? "";
                        string name = Convert.ToString(item.Name) ?? "";

                        // Check if it is a portable device (MTP / WPD)
                        bool isDriveLetter = path.Length >= 2 && path[1] == ':' && (path.Length == 2 || path.Length == 3);
                        bool isPortable = !isDriveLetter && (
                            type.IndexOf("Portable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            type.IndexOf("Device", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            type.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            type.IndexOf("Media", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            path.IndexOf("::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            path.StartsWith("::{") ||
                            path.StartsWith(@"\\?\usb")
                        );

                        if (isPortable)
                        {
                            var device = new AndroidDevice
                            {
                                Id = path,
                                Name = string.IsNullOrWhiteSpace(name) ? "Android Device" : name,
                                IsDemo = false
                            };

                            dynamic devFolder = item.GetFolder;
                            if (devFolder != null)
                            {
                                device.RootShellFolder = devFolder;
                                dynamic subItems = devFolder.Items();
                                if (subItems != null)
                                {
                                    for (int j = 0; j < subItems.Count; j++)
                                    {
                                        try
                                        {
                                            dynamic subItem = subItems.Item(j);
                                            string subName = Convert.ToString(subItem.Name);
                                            dynamic subFolder = subItem.GetFolder;

                                            string totalStr = Convert.ToString(devFolder.GetDetailsOf(subItem, 2));
                                            string freeStr = Convert.ToString(devFolder.GetDetailsOf(subItem, 3));

                                            long totalBytes = SizeFormatter.ParseSizeString(totalStr);
                                            long freeBytes = SizeFormatter.ParseSizeString(freeStr);

                                            var vol = new StorageVolumeInfo
                                            {
                                                Name = subName,
                                                TotalBytes = totalBytes,
                                                FreeBytes = freeBytes,
                                                ShellFolder = subFolder
                                            };

                                            device.StorageVolumes.Add(vol);
                                            device.TotalSpaceBytes += totalBytes;
                                            device.FreeSpaceBytes += freeBytes;

                                            if (string.IsNullOrEmpty(device.StorageName))
                                            {
                                                device.StorageName = subName;
                                            }
                                        }
                                        catch
                                        {
                                            // Individual sub-volume access error
                                        }
                                    }
                                }
                            }

                            if (device.TotalSpaceBytes <= 0)
                            {
                                device.TotalSpaceBytes = 64L * 1024L * 1024L * 1024L;
                                device.FreeSpaceBytes = 28L * 1024L * 1024L * 1024L;
                            }

                            devices.Add(device);
                        }
                    }
                    catch (COMException)
                    {
                        // Device disconnected mid-check
                    }
                    catch
                    {
                        // General fallback
                    }
                }
            }
            catch
            {
                // COM initialization error
            }

            return devices;
        }

        public Task<List<AndroidFileItem>> ScanFilesAsync(AndroidDevice device, Action<string, int> progressCallback)
        {
            return Task.Factory.StartNew(new Func<List<AndroidFileItem>>(() =>
            {
                var result = new List<AndroidFileItem>();
                if (device == null || device.StorageVolumes == null || device.StorageVolumes.Count == 0)
                {
                    return result;
                }

                int totalVolumes = device.StorageVolumes.Count;
                for (int v = 0; v < totalVolumes; v++)
                {
                    var vol = device.StorageVolumes[v];
                    if (vol.ShellFolder == null) continue;

                    string volName = vol.Name ?? "Storage";
                    if (progressCallback != null)
                    {
                        progressCallback(string.Format("Scanning {0}...", volName), (v * 100) / totalVolumes);
                    }

                    try
                    {
                        ScanFolderRecursive(vol.ShellFolder, @"\" + volName, 0, 5, result, progressCallback);
                    }
                    catch (COMException)
                    {
                        if (progressCallback != null)
                        {
                            progressCallback("Device communication interrupted.", 100);
                        }
                        break;
                    }
                }

                if (progressCallback != null)
                {
                    progressCallback(string.Format("Scan complete. Found {0} items.", result.Count), 100);
                }

                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                catch { }

                return result;
            }));
        }

        private void ScanFolderRecursive(dynamic folder, string currentPath, int depth, int maxDepth, List<AndroidFileItem> result, Action<string, int> progressCallback)
        {
            if (folder == null || depth > maxDepth) return;

            try
            {
                dynamic items = folder.Items();
                if (items == null) return;

                int count = items.Count;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        dynamic item = items.Item(i);
                        if (item == null) continue;

                        string itemName = Convert.ToString(item.Name) ?? "";
                        if (string.IsNullOrEmpty(itemName)) continue;

                        // Skip hidden or cache folders
                        if (itemName.StartsWith(".") && itemName != ".nomedia") continue;
                        if (SkipFolderNames.Contains(itemName)) continue;

                        bool isFolder = false;
                        try
                        {
                            isFolder = item.IsFolder;
                        }
                        catch { }

                        if (isFolder)
                        {
                            if (currentPath.IndexOf(@"\Android\data", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                currentPath.IndexOf(@"\Android\obb", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                continue;
                            }

                            if (depth < maxDepth)
                            {
                                dynamic subFolder = item.GetFolder;
                                if (subFolder != null)
                                {
                                    string subPath = currentPath + @"\" + itemName;
                                    if (progressCallback != null && (result.Count % 50 == 0))
                                    {
                                        progressCallback(string.Format("Scanning {0}...", subPath), 50);
                                    }
                                    ScanFolderRecursive(subFolder, subPath, depth + 1, maxDepth, result, progressCallback);
                                }
                            }
                        }
                        else
                        {
                            long size = 0;
                            try
                            {
                                size = (long)item.Size;
                            }
                            catch
                            {
                                try
                                {
                                    string sizeStr = Convert.ToString(folder.GetDetailsOf(item, 1));
                                    size = SizeFormatter.ParseSizeString(sizeStr);
                                }
                                catch { }
                            }

                            DateTime? modDate = null;
                            try
                            {
                                modDate = (DateTime)item.ModifyDate;
                            }
                            catch { }

                            var category = FileFormatHelper.DetermineCategory(itemName, currentPath);

                            var fileItem = new AndroidFileItem
                            {
                                Name = itemName,
                                SizeBytes = size,
                                DateModified = modDate,
                                DirectoryPath = currentPath,
                                Category = category,
                                IsFolder = false,
                                IsSelected = false,
                                ShellItem = item,
                                ParentFolder = folder
                            };

                            result.Add(fileItem);
                        }
                    }
                    catch (COMException)
                    {
                        // Item level COM error - device unplugged or file locked
                    }
                    catch
                    {
                        // Skip corrupted metadata
                    }
                }
            }
            catch (COMException)
            {
                throw; // Rethrow to stop traversal on disconnected device
            }
            catch
            {
                // Folder enumeration error
            }
        }

        public Task<int> ExportFilesAsync(List<AndroidFileItem> files, string destinationFolder, Action<int, int, string> progressCallback)
        {
            return Task.Factory.StartNew(new Func<int>(() =>
            {
                if (files == null || files.Count == 0) return 0;

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic dstFolder = shell.NameSpace(destinationFolder);

                int exportedCount = 0;
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file == null || file.ShellItem == null) continue;

                    string safeName = PathSanitizer.SanitizeFileName(file.Name);
                    if (progressCallback != null)
                    {
                        progressCallback(i + 1, files.Count, safeName);
                    }

                    try
                    {
                        if (dstFolder != null)
                        {
                            dstFolder.CopyHere(file.ShellItem, ShellCopyFlags);
                            exportedCount++;
                            Thread.Sleep(60);
                        }
                    }
                    catch (COMException ex)
                    {
                        if (ex.ErrorCode == unchecked((int)0x80070015) || ex.ErrorCode == unchecked((int)0x800706BA))
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // Copy failure for this specific item
                    }
                }

                return exportedCount;
            }));
        }

        public Task<int> ImportFilesAsync(AndroidDevice device, List<string> sourceFilePaths, string targetFolderName, Action<int, int, string> progressCallback)
        {
            return Task.Factory.StartNew(new Func<int>(() =>
            {
                if (device == null || device.StorageVolumes == null || device.StorageVolumes.Count == 0)
                {
                    return 0;
                }

                dynamic primaryStorage = device.StorageVolumes[0].ShellFolder;
                if (primaryStorage == null) return 0;

                dynamic targetFolder = null;
                if (!string.IsNullOrEmpty(targetFolderName))
                {
                    try
                    {
                        dynamic items = primaryStorage.Items();
                        for (int i = 0; i < items.Count; i++)
                        {
                            dynamic sub = items.Item(i);
                            if (string.Equals(Convert.ToString(sub.Name), targetFolderName, StringComparison.OrdinalIgnoreCase))
                            {
                                targetFolder = sub.GetFolder;
                                break;
                            }
                        }
                    }
                    catch { }
                }

                if (targetFolder == null)
                {
                    targetFolder = primaryStorage;
                }

                int importedCount = 0;
                for (int i = 0; i < sourceFilePaths.Count; i++)
                {
                    string filePath = sourceFilePaths[i];
                    if (!File.Exists(filePath)) continue;

                    string fileName = Path.GetFileName(filePath);
                    if (progressCallback != null)
                    {
                        progressCallback(i + 1, sourceFilePaths.Count, fileName);
                    }

                    try
                    {
                        targetFolder.CopyHere(filePath, ShellCopyFlags);
                        importedCount++;
                        Thread.Sleep(80);
                    }
                    catch (COMException ex)
                    {
                        if (ex.ErrorCode == unchecked((int)0x80070015) || ex.ErrorCode == unchecked((int)0x800706BA))
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // Import failure
                    }
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
                    if (file == null || file.ShellItem == null) continue;

                    // STRICT SAFETY CHECK: Verify safe deletion target
                    if (!PathSanitizer.IsSafeDeletionTarget(file))
                    {
                        continue;
                    }

                    if (progressCallback != null)
                    {
                        progressCallback(i + 1, files.Count, file.Name);
                    }

                    try
                    {
                        file.ShellItem.InvokeVerb("delete");
                        deletedCount++;
                        Thread.Sleep(40);
                    }
                    catch (COMException ex)
                    {
                        if (ex.ErrorCode == unchecked((int)0x80070015) || ex.ErrorCode == unchecked((int)0x800706BA))
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // Delete error
                    }
                }

                return deletedCount;
            }));
        }
    }
}
