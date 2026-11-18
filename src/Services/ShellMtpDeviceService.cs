using System;
using System.Collections.Concurrent;
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

        private readonly BlockingCollection<Action> _staQueue = new BlockingCollection<Action>();
        private readonly Thread _staThread;

        public ShellMtpDeviceService()
        {
            _staThread = new Thread(StaWorkerLoop)
            {
                Name = "Pigsty-MTP-STA-Worker",
                IsBackground = true
            };
            _staThread.SetApartmentState(ApartmentState.STA);
            _staThread.Start();
        }

        private void StaWorkerLoop()
        {
            foreach (var work in _staQueue.GetConsumingEnumerable())
            {
                try
                {
                    work();
                }
                catch
                {
                }
            }
        }

        private Task<T> RunOnStaAsync<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            _staQueue.Add(() =>
            {
                try
                {
                    T result = func();
                    tcs.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }

        public Task<List<AndroidDevice>> DetectDevicesAsync()
        {
            return RunOnStaAsync(() => DetectDevicesInternal());
        }

        private List<AndroidDevice> DetectDevicesInternal()
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

                int count = items.Count;
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        dynamic item = items.Item(i);
                        if (item == null) continue;

                        string path = Convert.ToString(item.Path) ?? "";
                        string type = Convert.ToString(item.Type) ?? "";
                        string name = Convert.ToString(item.Name) ?? "";

                        bool isDriveLetter = path.Length >= 2 && path[1] == ':' && (path.Length == 2 || path.Length == 3);
                        bool isPortable = !isDriveLetter && (
                            type.IndexOf("Portable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            type.IndexOf("Device", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            type.IndexOf("Phone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            type.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            type.IndexOf("Media", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            path.IndexOf("::{20D04FE0-3AEA-1069-A2D8-08002B30309D}", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            path.StartsWith("::{", StringComparison.Ordinal) ||
                            path.StartsWith(@"\\?\usb", StringComparison.OrdinalIgnoreCase)
                        );

                        if (isPortable)
                        {
                            var device = new AndroidDevice
                            {
                                Id = path,
                                Name = string.IsNullOrWhiteSpace(name) ? "Android" : name.Trim()
                            };

                            dynamic devFolder = item.GetFolder;
                            if (devFolder != null)
                            {
                                device.RootShellFolder = devFolder;
                                dynamic subItems = devFolder.Items();
                                if (subItems != null)
                                {
                                    int subCount = subItems.Count;
                                    for (int j = 0; j < subCount; j++)
                                    {
                                        try
                                        {
                                            dynamic subItem = subItems.Item(j);
                                            if (subItem == null) continue;

                                            string subName = Convert.ToString(subItem.Name) ?? "Storage";
                                            dynamic subFolder = subItem.GetFolder;
                                            if (subFolder == null) continue;

                                            long totalBytes = TryReadExtendedLong(subItem, "System.Capacity");
                                            long freeBytes = TryReadExtendedLong(subItem, "System.FreeSpace");

                                            if (totalBytes <= 0)
                                            {
                                                string totalStr = Convert.ToString(devFolder.GetDetailsOf(subItem, 2));
                                                totalBytes = SizeFormatter.ParseSizeString(totalStr);
                                            }
                                            if (freeBytes <= 0)
                                            {
                                                string freeStr = Convert.ToString(devFolder.GetDetailsOf(subItem, 3));
                                                freeBytes = SizeFormatter.ParseSizeString(freeStr);
                                            }

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
                                        }
                                    }
                                }
                            }

                            devices.Add(device);
                        }
                    }
                    catch (COMException)
                    {
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            return devices;
        }

        private static long TryReadExtendedLong(dynamic shellItem, string propName)
        {
            try
            {
                object val = shellItem.ExtendedProperty(propName);
                if (val != null)
                {
                    return Convert.ToInt64(val);
                }
            }
            catch
            {
            }
            return 0;
        }

        public Task<List<AndroidFileItem>> ScanFilesAsync(AndroidDevice device, Action<string, int> progressCallback, Func<bool> isCancelled = null)
        {
            return RunOnStaAsync(() =>
            {
                var result = new List<AndroidFileItem>();
                if (device == null || device.StorageVolumes == null || device.StorageVolumes.Count == 0)
                {
                    return result;
                }

                int totalVolumes = device.StorageVolumes.Count;
                for (int v = 0; v < totalVolumes; v++)
                {
                    if (isCancelled != null && isCancelled()) break;

                    var vol = device.StorageVolumes[v];
                    if (vol.ShellFolder == null) continue;

                    string volName = vol.Name ?? "Storage";
                    if (progressCallback != null)
                    {
                        progressCallback(string.Format("Scanning {0}...", volName), (v * 100) / totalVolumes);
                    }

                    try
                    {
                        ScanFolderRecursive(vol.ShellFolder, @"\" + volName, 0, 6, result, progressCallback, isCancelled);
                    }
                    catch (COMException)
                    {
                        break;
                    }
                }

                return result;
            });
        }

        private void ScanFolderRecursive(dynamic folder, string currentPath, int depth, int maxDepth, List<AndroidFileItem> result, Action<string, int> progressCallback, Func<bool> isCancelled)
        {
            if (folder == null || depth > maxDepth) return;
            if (isCancelled != null && isCancelled()) return;

            try
            {
                dynamic items = folder.Items();
                if (items == null) return;

                int count = items.Count;
                for (int i = 0; i < count; i++)
                {
                    if (isCancelled != null && isCancelled()) return;

                    try
                    {
                        dynamic item = items.Item(i);
                        if (item == null) continue;

                        string itemName = Convert.ToString(item.Name) ?? "";
                        if (string.IsNullOrEmpty(itemName)) continue;

                        if (itemName.StartsWith(".", StringComparison.Ordinal) && itemName != ".nomedia") continue;
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
                                    if (progressCallback != null && (result.Count % 60 == 0))
                                    {
                                        progressCallback(string.Format("Scanning {0}", itemName), 50);
                                    }
                                    ScanFolderRecursive(subFolder, subPath, depth + 1, maxDepth, result, progressCallback, isCancelled);
                                }
                            }
                        }
                        else
                        {
                            long size = TryReadExtendedLong(item, "System.Size");
                            if (size <= 0)
                            {
                                try
                                {
                                    size = Convert.ToInt64(item.Size);
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
                            }
                            if (size < 0) size = 0;

                            DateTime? modDate = null;
                            try
                            {
                                object extDate = item.ExtendedProperty("System.DateModified");
                                if (extDate is DateTime)
                                {
                                    modDate = ((DateTime)extDate).ToLocalTime();
                                }
                                else
                                {
                                    DateTime dt = (DateTime)item.ModifyDate;
                                    if (dt.Year > 1980) modDate = dt;
                                }
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
                    }
                    catch
                    {
                    }
                }
            }
            catch (COMException)
            {
                throw;
            }
            catch
            {
            }
        }

        public Task<int> ExportFilesAsync(List<AndroidFileItem> files, string destinationFolder, Action<int, int, string> progressCallback, Func<bool> isCancelled = null)
        {
            return RunOnStaAsync(() =>
            {
                if (files == null || files.Count == 0) return 0;
                if (!PathSanitizer.IsValidDestinationDirectory(destinationFolder)) return 0;

                string fullDest = Path.GetFullPath(destinationFolder);
                if (!Directory.Exists(fullDest))
                {
                    Directory.CreateDirectory(fullDest);
                }

                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null) return 0;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic dstFolder = shell.NameSpace(fullDest);
                if (dstFolder == null) return 0;

                int exportedCount = 0;
                for (int i = 0; i < files.Count; i++)
                {
                    if (isCancelled != null && isCancelled()) break;

                    var file = files[i];
                    if (file == null || file.ShellItem == null) continue;

                    string safeName = PathSanitizer.SanitizeFileName(file.Name);
                    if (progressCallback != null)
                    {
                        progressCallback(i + 1, files.Count, safeName);
                    }

                    try
                    {
                        dstFolder.CopyHere(file.ShellItem, ShellCopyFlags);
                        exportedCount++;
                        Thread.Sleep(50);
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
                    }
                }

                return exportedCount;
            });
        }

        public Task<int> ImportFilesAsync(AndroidDevice device, List<string> sourceFilePaths, string targetFolderName, Action<int, int, string> progressCallback, Func<bool> isCancelled = null)
        {
            return RunOnStaAsync(() =>
            {
                if (device == null || device.StorageVolumes == null || device.StorageVolumes.Count == 0 || sourceFilePaths == null)
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
                        if (items != null)
                        {
                            int count = items.Count;
                            for (int i = 0; i < count; i++)
                            {
                                dynamic sub = items.Item(i);
                                if (sub != null && string.Equals(Convert.ToString(sub.Name), targetFolderName, StringComparison.OrdinalIgnoreCase))
                                {
                                    targetFolder = sub.GetFolder;
                                    break;
                                }
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
                    if (isCancelled != null && isCancelled()) break;

                    string filePath = sourceFilePaths[i];
                    if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) continue;

                    string fileName = Path.GetFileName(filePath);
                    if (progressCallback != null)
                    {
                        progressCallback(i + 1, sourceFilePaths.Count, fileName);
                    }

                    try
                    {
                        targetFolder.CopyHere(filePath, ShellCopyFlags);
                        importedCount++;
                        Thread.Sleep(60);
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
                    }
                }

                return importedCount;
            });
        }

        public Task<int> DeleteFilesAsync(List<AndroidFileItem> files, Action<int, int, string> progressCallback, Func<bool> isCancelled = null)
        {
            return RunOnStaAsync(() =>
            {
                if (files == null || files.Count == 0) return 0;

                int deletedCount = 0;
                for (int i = 0; i < files.Count; i++)
                {
                    if (isCancelled != null && isCancelled()) break;

                    var file = files[i];
                    if (file == null || file.ShellItem == null) continue;

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
                        Thread.Sleep(35);
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
                    }
                }

                return deletedCount;
            });
        }
    }
}
