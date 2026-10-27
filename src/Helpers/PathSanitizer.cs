using System;
using System.IO;
using System.Text.RegularExpressions;
using Pigsty.Models;

namespace Pigsty.Helpers
{
    public static class PathSanitizer
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private static readonly string[] ReservedDeviceNames = new string[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "unnamed_file";
            }

            // Replace invalid characters with underscore
            string clean = fileName;
            foreach (char c in InvalidFileNameChars)
            {
                clean = clean.Replace(c, '_');
            }

            // Strip path traversal attempts
            clean = clean.Replace("..", "_");
            clean = clean.Trim('.', ' ');

            if (string.IsNullOrEmpty(clean))
            {
                clean = "file_" + DateTime.Now.Ticks;
            }

            // Check reserved Windows names
            string baseName = Path.GetFileNameWithoutExtension(clean).ToUpperInvariant();
            foreach (string reserved in ReservedDeviceNames)
            {
                if (baseName == reserved)
                {
                    clean = "_" + clean;
                    break;
                }
            }

            return clean;
        }

        public static string GetSafeExportPath(string destinationDirectory, string originalFileName)
        {
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            string safeFileName = SanitizeFileName(originalFileName);
            string targetPath = Path.Combine(destinationDirectory, safeFileName);

            // If file already exists, avoid overwriting by appending (1), (2), etc.
            if (File.Exists(targetPath))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
                string ext = Path.GetExtension(safeFileName);

                int counter = 1;
                while (File.Exists(targetPath) && counter < 10000)
                {
                    string numberedName = string.Format("{0} ({1}){2}", nameWithoutExt, counter, ext);
                    targetPath = Path.Combine(destinationDirectory, numberedName);
                    counter++;
                }
            }

            return targetPath;
        }

        public static bool IsSafeDeletionTarget(AndroidFileItem item)
        {
            if (item == null) return false;

            // Never delete folders directly - only individual files
            if (item.IsFolder) return false;

            if (string.IsNullOrWhiteSpace(item.Name)) return false;

            string dir = (item.DirectoryPath ?? "").Trim();

            // Guard against root storage deletion
            if (dir == "" || dir == @"\" || dir == "/" ||
                string.Equals(dir, @"\Internal shared storage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dir, @"\SD card", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Guard against system Android folders
            if (dir.IndexOf(@"\Android\data", StringComparison.OrdinalIgnoreCase) >= 0 ||
                dir.IndexOf(@"\Android\obb", StringComparison.OrdinalIgnoreCase) >= 0 ||
                dir.IndexOf(@"\system", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return true;
        }
    }
}
