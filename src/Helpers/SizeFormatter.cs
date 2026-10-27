using System;

namespace Pigsty.Helpers
{
    public static class SizeFormatter
    {
        private static readonly string[] Units = new string[] { "B", "KB", "MB", "GB", "TB" };

        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) return "0 B";
            if (bytes == 0) return "0 B";

            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024.0 && unitIndex < Units.Length - 1)
            {
                size /= 1024.0;
                unitIndex++;
            }

            if (unitIndex == 0)
            {
                return string.Format("{0} B", (long)size);
            }
            if (size >= 100)
            {
                return string.Format("{0:0} {1}", size, Units[unitIndex]);
            }
            if (size >= 10)
            {
                return string.Format("{0:0.#} {1}", size, Units[unitIndex]);
            }
            return string.Format("{0:0.##} {1}", size, Units[unitIndex]);
        }

        public static long ParseSizeString(string sizeStr)
        {
            if (string.IsNullOrWhiteSpace(sizeStr)) return 0;
            sizeStr = sizeStr.Trim().ToUpperInvariant();

            long multiplier = 1;
            if (sizeStr.EndsWith("TB"))
            {
                multiplier = 1024L * 1024L * 1024L * 1024L;
                sizeStr = sizeStr.Substring(0, sizeStr.Length - 2).Trim();
            }
            else if (sizeStr.EndsWith("GB"))
            {
                multiplier = 1024L * 1024L * 1024L;
                sizeStr = sizeStr.Substring(0, sizeStr.Length - 2).Trim();
            }
            else if (sizeStr.EndsWith("MB"))
            {
                multiplier = 1024L * 1024L;
                sizeStr = sizeStr.Substring(0, sizeStr.Length - 2).Trim();
            }
            else if (sizeStr.EndsWith("KB"))
            {
                multiplier = 1024L;
                sizeStr = sizeStr.Substring(0, sizeStr.Length - 2).Trim();
            }
            else if (sizeStr.EndsWith("B"))
            {
                multiplier = 1;
                sizeStr = sizeStr.Substring(0, sizeStr.Length - 1).Trim();
            }

            double val;
            if (double.TryParse(sizeStr, out val))
            {
                return (long)(val * multiplier);
            }
            return 0;
        }
    }
}
