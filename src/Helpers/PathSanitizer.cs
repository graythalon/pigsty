using System;
using System.IO;
using System.Text;
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

        private static readonly string[] ProtectedSystemPaths = new string[]
        {
            @"\Android\data",
            @"\Android\obb",
            @"\system",
            @"\vendor",
            @"\proc",
            @"\dev",
            @"\efs",
            @"\boot",
            @"\recovery",
            @"\firmware"
        };

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "unnamed_file";
            }

            var sb = new StringBuilder(fileName.Length);
            foreach (char c in fileName)
            {
                if (c < 32 || Array.IndexOf(InvalidFileNameChars, c) >= 0)
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(c);
                }
            }

            string clean = sb.ToString().Replace("..", "_").Trim('.', ' ');

            if (string.IsNullOrEmpty(clean))
            {
                clean = "file_" + DateTime.UtcNow.Ticks;
            }

            string baseName = Path.GetFileNameWithoutExtension(clean).ToUpperInvariant();
            foreach (string reserved in ReservedDeviceNames)
            {
                if (baseName == reserved)
                {
                    clean = "_" + clean;
                    break;
                }
            }

            if (clean.Length > 200)
            {
                string ext = Path.GetExtension(clean);
                if (ext.Length > 20) ext = ext.Substring(0, 20);
                clean = clean.Substring(0, 200 - ext.Length) + ext;
            }

            return clean;
        }

        public static bool IsValidDestinationDirectory(string destinationDirectory)
        {
            if (string.IsNullOrWhiteSpace(destinationDirectory)) return false;
            try
            {
                string full = Path.GetFullPath(destinationDirectory);
                string root = Path.GetPathRoot(full);
                return !string.IsNullOrEmpty(root) && full.Length >= root.Length;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSafeDeletionTarget(AndroidFileItem item)
        {
            if (item == null || item.IsFolder || item.ShellItem == null) return false;
            if (string.IsNullOrWhiteSpace(item.Name)) return false;

            string dir = (item.DirectoryPath ?? "").Replace('/', '\\').Trim();

            if (string.IsNullOrEmpty(dir) || dir == @"\" ||
                string.Equals(dir, @"\Internal shared storage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dir, @"\SD card", StringComparison.OrdinalIgnoreCase))
            {
                // Wait: files directly inside the root of Internal shared storage have DirectoryPath == "\Internal shared storage".
                // We DO want to allow deleting a user file (like a stray .apk or .log) in root storage,
                // as long as item.IsFolder == false and item.Name is not a system folder name!
                if (string.Equals(item.Name, "Android", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.Name, "system", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return !string.IsNullOrEmpty(dir) && dir != @"\";
            }

            foreach (string protectedPath in ProtectedSystemPaths)
            {
                if (dir.IndexOf(protectedPath, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
