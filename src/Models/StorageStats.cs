using System;
using System.Collections.Generic;
using Pigsty.Helpers;

namespace Pigsty.Models
{
    public class StorageStats
    {
        public int TotalFileCount { get; set; }
        public long TotalSizeBytes { get; set; }

        public int PhotosCount { get; set; }
        public long PhotosSizeBytes { get; set; }

        public int VideosCount { get; set; }
        public long VideosSizeBytes { get; set; }

        public int AudioCount { get; set; }
        public long AudioSizeBytes { get; set; }

        public int DocumentsCount { get; set; }
        public long DocumentsSizeBytes { get; set; }

        public int ApksCount { get; set; }
        public long ApksSizeBytes { get; set; }

        public int DownloadsCount { get; set; }
        public long DownloadsSizeBytes { get; set; }

        public int ArchivesCount { get; set; }
        public long ArchivesSizeBytes { get; set; }

        public int LargeFilesCount { get; set; }
        public long LargeFilesSizeBytes { get; set; }

        public int OtherCount { get; set; }
        public long OtherSizeBytes { get; set; }

        public string FormattedTotalSize
        {
            get { return SizeFormatter.FormatBytes(TotalSizeBytes); }
        }

        public static StorageStats Calculate(IEnumerable<AndroidFileItem> items)
        {
            var stats = new StorageStats();
            if (items == null) return stats;

            foreach (var item in items)
            {
                if (item.IsFolder) continue;

                stats.TotalFileCount++;
                stats.TotalSizeBytes += item.SizeBytes;

                if (item.IsLargeFile)
                {
                    stats.LargeFilesCount++;
                    stats.LargeFilesSizeBytes += item.SizeBytes;
                }

                switch (item.Category)
                {
                    case CategoryType.Photos:
                        stats.PhotosCount++;
                        stats.PhotosSizeBytes += item.SizeBytes;
                        break;
                    case CategoryType.Videos:
                        stats.VideosCount++;
                        stats.VideosSizeBytes += item.SizeBytes;
                        break;
                    case CategoryType.Audio:
                        stats.AudioCount++;
                        stats.AudioSizeBytes += item.SizeBytes;
                        break;
                    case CategoryType.Documents:
                        stats.DocumentsCount++;
                        stats.DocumentsSizeBytes += item.SizeBytes;
                        break;
                    case CategoryType.Apks:
                        stats.ApksCount++;
                        stats.ApksSizeBytes += item.SizeBytes;
                        break;
                    case CategoryType.Downloads:
                        stats.DownloadsCount++;
                        stats.DownloadsSizeBytes += item.SizeBytes;
                        break;
                    case CategoryType.Archives:
                        stats.ArchivesCount++;
                        stats.ArchivesSizeBytes += item.SizeBytes;
                        break;
                    default:
                        stats.OtherCount++;
                        stats.OtherSizeBytes += item.SizeBytes;
                        break;
                }
            }

            return stats;
        }
    }
}
