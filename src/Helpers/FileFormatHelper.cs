using System;
using System.Collections.Generic;
using System.IO;
using Pigsty.Models;

namespace Pigsty.Helpers
{
    public static class FileFormatHelper
    {
        private static readonly HashSet<string> PhotoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".heic", ".heif", ".gif", ".bmp", ".dng", ".cr2", ".nef", ".arw", ".svg", ".raw", ".ico", ".tiff", ".tif"
        };

        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".mov", ".3gp", ".webm", ".avi", ".flv", ".wmv", ".m4v", ".ts", ".mpg", ".mpeg", ".vob"
        };

        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".m4a", ".wav", ".flac", ".aac", ".ogg", ".opus", ".wma", ".mid", ".midi", ".amr", ".m4b", ".alac", ".aif", ".aiff"
        };

        private static readonly HashSet<string> DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".txt", ".epub", ".mobi", ".csv", ".rtf", ".odt", ".ods", ".odp", ".tsv", ".json", ".xml", ".html", ".htm", ".md", ".log"
        };

        private static readonly HashSet<string> ApkExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".apk", ".xapk", ".apks", ".aab", ".jar"
        };

        private static readonly HashSet<string> ArchiveExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".tgz", ".7zip"
        };

        public const long LargeFileThresholdBytes = 50L * 1024L * 1024L; // 50 MB

        public static CategoryType DetermineCategory(string fileName, string folderPath)
        {
            string ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext)) ext = "";

            if (ApkExtensions.Contains(ext))
            {
                return CategoryType.Apks;
            }

            if (PhotoExtensions.Contains(ext))
            {
                return CategoryType.Photos;
            }

            if (VideoExtensions.Contains(ext))
            {
                return CategoryType.Videos;
            }

            if (AudioExtensions.Contains(ext))
            {
                return CategoryType.Audio;
            }

            if (ArchiveExtensions.Contains(ext))
            {
                return CategoryType.Archives;
            }

            if (DocumentExtensions.Contains(ext))
            {
                return CategoryType.Documents;
            }

            // Check if inside Download folder
            if (!string.IsNullOrEmpty(folderPath) && 
                (folderPath.IndexOf("Download", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 folderPath.IndexOf("Downloads", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return CategoryType.Downloads;
            }

            return CategoryType.AllFiles;
        }

        public static string GetGlyphForExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return "📄";
            ext = ext.ToLowerInvariant();

            if (PhotoExtensions.Contains(ext)) return "📷";
            if (VideoExtensions.Contains(ext)) return "🎬";
            if (AudioExtensions.Contains(ext)) return "🎵";
            if (DocumentExtensions.Contains(ext))
            {
                if (ext == ".pdf") return "📕";
                if (ext == ".xlsx" || ext == ".xls" || ext == ".csv") return "📊";
                if (ext == ".docx" || ext == ".doc") return "📝";
                return "📄";
            }
            if (ApkExtensions.Contains(ext)) return "📦";
            if (ArchiveExtensions.Contains(ext)) return "🗃️";

            return "📄";
        }
    }
}
