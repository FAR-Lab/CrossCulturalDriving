using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UltimateReplay.Statistics
{
    public static class ReplayStatisticsUtil
    {
        // Private
        private const long kiloByteUnit = 1024;
        private const long megaByteUnit = kiloByteUnit * 1024;

        // Methods
        public static decimal GetMemorySizeSmallestUnit(int amount)
        {
            // Get amount as decimal
            decimal value = amount;

            // Check for mega bytes
            if (value > megaByteUnit)
            {
                // Megabytes
                value = decimal.Round(value / megaByteUnit, 2);
            }
            else if (value > kiloByteUnit)
            {
                // Kilobytes
                value = decimal.Round(value / kiloByteUnit, 2);
            }
            else
            {
                // Bytes should not display a decimal place
                value = decimal.Round(value, 0);
            }

            return value;
        }

        public static string GetMemoryUnitString(int amount)
        {
            // Megabytes
            if (amount > megaByteUnit)
                return "MB";

            // Kilobytes
            if (amount > kiloByteUnit)
                return "KB";

            // Check for singular
            if (amount == 1)
                return "Byte";

            // Bytes
            return "Bytes";
        }

        public static string GetByteSizeString(int bytes)
        {
            const int kb = 1024;
            const int mb = kb * 1024;
            const int gb = mb * 1024;

            double result = bytes;
            string suffix = "B";

            if (bytes > gb)
            {
                result = Math.Round((double)bytes / gb, 2);
                suffix = "GB";
            }
            else if (bytes > mb)
            {
                result = Math.Round((double)bytes / mb, 2);
                suffix = "MB";
            }
            else if (bytes > kb)
            {
                result = Math.Round((double)bytes / kb, 2);
                suffix = "KB";
            }

            return string.Format("{0}{1}", result, suffix);
        }
    }
}
