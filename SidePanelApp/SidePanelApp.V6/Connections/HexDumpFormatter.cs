using System;
using System.Text;

namespace SidePanelApp.V6.Connections
{
    internal static class HexDumpFormatter
    {
        private const int BytesPerLine = 16;
        private const int DefaultMaxBytes = 8192;

        public static string Format(byte[] data, int maxBytes = DefaultMaxBytes)
        {
            if (data == null || data.Length == 0)
            {
                return "(пустой payload)\r\n";
            }

            int length = Math.Min(data.Length, Math.Max(1, maxBytes));
            var sb = new StringBuilder();
            for (int offset = 0; offset < length; offset += BytesPerLine)
            {
                sb.Append(offset.ToString("X8"));
                sb.Append("  ");
                int lineLen = Math.Min(BytesPerLine, length - offset);
                for (int i = 0; i < BytesPerLine; i++)
                {
                    if (i < lineLen)
                    {
                        sb.Append(data[offset + i].ToString("X2"));
                    }
                    else
                    {
                        sb.Append("  ");
                    }

                    if (i == 7)
                    {
                        sb.Append(' ');
                    }
                    else if (i < BytesPerLine - 1)
                    {
                        sb.Append(' ');
                    }
                }

                sb.Append("  ");
                sb.Append(BuildAsciiLine(data, offset, lineLen));
                sb.AppendLine();
            }

            if (data.Length > length)
            {
                sb.AppendLine("… обрезано, всего " + data.Length + " байт");
            }

            sb.AppendLine();
            sb.AppendLine("ASCII:");
            sb.Append(ToPrintableAscii(data, length));
            return sb.ToString();
        }

        public static string ToPrintableAscii(byte[] data, int length)
        {
            if (data == null || length <= 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                byte b = data[i];
                if (b == (byte)'\r')
                {
                    sb.Append("\\r");
                }
                else if (b == (byte)'\n')
                {
                    sb.Append("\\n");
                }
                else if (b >= 32 && b < 127)
                {
                    sb.Append((char)b);
                }
                else
                {
                    sb.Append('.');
                }
            }

            return sb.ToString();
        }

        private static string BuildAsciiLine(byte[] data, int offset, int lineLen)
        {
            var sb = new StringBuilder(lineLen);
            for (int i = 0; i < lineLen; i++)
            {
                byte b = data[offset + i];
                sb.Append(b >= 32 && b < 127 ? (char)b : '.');
            }

            return sb.ToString();
        }
    }
}
