using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pigsty.Models;

namespace Pigsty.Services
{
    public interface IMtpDeviceService
    {
        List<AndroidDevice> DetectDevices();
        Task<List<AndroidFileItem>> ScanFilesAsync(AndroidDevice device, Action<string, int> progressCallback);
        Task<int> ExportFilesAsync(List<AndroidFileItem> files, string destinationFolder, Action<int, int, string> progressCallback);
        Task<int> ImportFilesAsync(AndroidDevice device, List<string> sourceFilePaths, string targetFolderName, Action<int, int, string> progressCallback);
        Task<int> DeleteFilesAsync(List<AndroidFileItem> files, Action<int, int, string> progressCallback);
    }
}
