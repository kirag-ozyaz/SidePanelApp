using System;

namespace SidePanelApp.V5.Hardware
{
    internal static class MetricFormat
    {
        public static string BytesPerSec(double bytesPerSec)
        {
            if (bytesPerSec < 0)
            {
                bytesPerSec = 0;
            }

            double kb = bytesPerSec / 1024d;
            if (kb < 1)
            {
                return "0 КБ/с";
            }

            if (kb < 1024)
            {
                return kb < 10 ? string.Format("{0:0.0} КБ/с", kb) : string.Format("{0:0} КБ/с", kb);
            }

            double mb = kb / 1024d;
            if (mb < 1024)
            {
                return mb < 10 ? string.Format("{0:0.0} МБ/с", mb) : string.Format("{0:0} МБ/с", mb);
            }

            return string.Format("{0:0.00} ГБ/с", mb / 1024d);
        }

        public static string CompactBytesPerSec(double bytesPerSec)
        {
            if (bytesPerSec < 0)
            {
                bytesPerSec = 0;
            }

            double kb = bytesPerSec / 1024d;
            if (kb < 1)
            {
                return "0K";
            }

            if (kb < 10)
            {
                return string.Format("{0:0.0}K", kb);
            }

            if (kb < 1024)
            {
                return string.Format("{0:0}K", kb);
            }

            double mb = kb / 1024d;
            if (mb < 10)
            {
                return string.Format("{0:0.0}M", mb);
            }

            if (mb < 1024)
            {
                return string.Format("{0:0}M", mb);
            }

            return string.Format("{0:0.0}G", mb / 1024d);
        }

        public static string Gigabytes(ulong bytes)
        {
            double gb = bytes / (1024d * 1024d * 1024d);
            if (gb < 0.01 && bytes > 0)
            {
                return string.Format("{0:0} МБ", bytes / (1024d * 1024d));
            }

            return gb >= 10 ? string.Format("{0:0.0} ГБ", gb) : string.Format("{0:0.00} ГБ", gb);
        }

        public static string Gigahertz(double mhz)
        {
            if (mhz <= 0)
            {
                return "—";
            }

            double ghz = mhz / 1000d;
            return ghz >= 10
                ? string.Format("{0:0.0} ГГц", ghz)
                : string.Format("{0:0.00} ГГц", ghz);
        }

        public static string FrequencyPair(double currentMhz, double maxMhz)
        {
            if (currentMhz <= 0 && maxMhz <= 0)
            {
                return "—";
            }

            if (maxMhz <= 0)
            {
                return Gigahertz(currentMhz);
            }

            return string.Format("{0} / {1}", Gigahertz(currentMhz), Gigahertz(maxMhz));
        }
    }
}
