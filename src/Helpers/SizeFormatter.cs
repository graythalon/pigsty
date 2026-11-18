using System;
using System.Globalization;
using System.Text;

namespace Pigsty.Helpers
{
    public static class SizeFormatter
    {
        private static readonly string[] Units = new string[] { "B", "KB", "MB", "GB", "TB" };

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";

            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024.0 && unitIndex < Units.Length - 1)
            {
                size /= 1024.0;
                unitIndex++;
            }

            if (unitIndex == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} B", (long)size);
            }
            if (size >= 100)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0} {1}", size, Units[unitIndex]);
            }
            if (size >= 10)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:0.#} {1}", size, Units[unitIndex]);
            }
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", size, Units[unitIndex]);
        }

        public static long ParseSizeString(string sizeStr)
        {
            if (string.IsNullOrWhiteSpace(sizeStr)) return 0;

            // Strip invisible Unicode bidi marks (\u200E, \u200F, etc.) often injected by Windows Shell GetDetailsOf
            var sb = new StringBuilder(sizeStr.Length);
            foreach (char c in sizeStr)
            {
                if (c >= 32 && c <= 126)
                {
                    sb.Append(char.ToUpperInvariant(c));
                }
            }

            string clean = sb.ToString().Trim();
            if (string.IsNullOrEmpty(clean)) return 0;

            long multiplier = 1;
            if (clean.EndsWith("TB", StringComparison.Ordinal))
            {
                multiplier = 1024L * 1024L * 1024L * 1024L;
                clean = clean.Substring(0, clean.Length - 2).Trim();
            }
            else if (clean.EndsWith("GB", StringComparison.Ordinal))
            {
                multiplier = 1024L * 1024L * 1024L;
                clean = clean.Substring(0, clean.Length - 2).Trim();
            }
            else if (clean.EndsWith("MB", StringComparison.Ordinal))
            {
                multiplier = 1024L * 1024L;
                clean = clean.Substring(0, clean.Length - 2).Trim();
            }
            else if (clean.EndsWith("KB", StringComparison.Ordinal))
            {
                multiplier = 1024L;
                clean = clean.Substring(0, clean.Length - 2).Trim();
            }
            else if (clean.EndsWith("BYTES", StringComparison.Ordinal))
            {
                multiplier = 1;
                clean = clean.Substring(0, clean.Length - 5).Trim();
            }
            else if (clean.EndsWith("B", StringComparison.Ordinal))
            {
                multiplier = 1;
                clean = clean.Substring(0, clean.Length - 1).Trim();
            }

            // Normalize number separators
            if (clean.Contains(",") && clean.Contains("."))
            {
                clean = clean.Replace(",", "");
            }
            else if (clean.Contains(","))
            {
                clean = clean.Replace(",", ".");
            }

            double val;
            if (double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out val) && val >= 0)
            {
                return (long)(val * multiplier);
            }
            return 0;
        }
    }
}
