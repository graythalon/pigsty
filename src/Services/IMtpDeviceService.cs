using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pigsty.Models;

namespace Pigsty.Services
{
    public interface IMtpDeviceService
    {
        Task<List<AndroidDevice>> DetectDevicesAsync();
        Task<List<AndroidFileItem>> ScanFilesAsync(AndroidDevice device, Action<string, int> progressCallback, Func<bool> isCancelled = null);
        Task<int> ExportFilesAsync(List<AndroidFileItem> files, string destinationFolder, Action<int, int, string> progressCallback, Func<bool> isCancelled = null);
        Task<int> ImportFilesAsync(AndroidDevice device, List<string> sourceFilePaths, string targetFolderName, Action<int, int, string> progressCallback, Func<bool> isCancelled = null);
        Task<int> DeleteFilesAsync(List<AndroidFileItem> files, Action<int, int, string> progressCallback, Func<bool> isCancelled = null);
    }
}
