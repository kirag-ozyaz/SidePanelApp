using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SidePanelApp
{
    /// <summary>Единая иконка приложения (app.ico + одинаковый рисунок во всех размерах).</summary>
    public static class AppIconFactory
    {
        private static readonly Color Background = Color.FromArgb(26, 26, 46);
        private static readonly Color CpuAccent = Color.FromArgb(255, 107, 53);
        private static readonly Color RamAccent = Color.FromArgb(56, 239, 125);
        private static readonly Color RingTrack = Color.FromArgb(51, 51, 85);

        private static readonly object IconCacheLock = new object();
        private static readonly Dictionary<int, Icon> IconCache = new Dictionary<int, Icon>();

        /// <summary>Окна, трей, exe — одна и та же иконка из app.ico.</summary>
        public static Icon GetApplicationIcon(int size = 32)
        {
            lock (IconCacheLock)
            {
                Icon cached;
                if (IconCache.TryGetValue(size, out cached))
                {
                    return (Icon)cached.Clone();
                }

                Icon icon = LoadBundledIcon(size) ?? RenderToIcon(size);
                IconCache[size] = icon;
                return (Icon)icon.Clone();
            }
        }

        public static Icon CreateTrayIcon()
        {
            return GetApplicationIcon(16);
        }

        [Obsolete("Use GetApplicationIcon(size).")]
        public static Icon CreateIcon(int size)
        {
            return GetApplicationIcon(size);
        }

        public static void SaveIconFile(string path)
        {
            using (Bitmap icon16 = Render(16))
            using (Bitmap icon32 = Render(32))
            using (Bitmap icon48 = Render(48))
            {
                WriteIco(path, icon16, icon32, icon48);
            }
        }

        private static Icon LoadBundledIcon(int size)
        {
            using (Stream stream = OpenEmbeddedIcoStream())
            {
                if (stream != null)
                {
                    using (Icon multi = new Icon(stream))
                    {
                        return new Icon(multi, size, size);
                    }
                }
            }

            return null;
        }

        private static Stream OpenEmbeddedIcoStream()
        {
            Assembly assembly = typeof(AppIconFactory).Assembly;
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase))
                {
                    return assembly.GetManifestResourceStream(name);
                }
            }

            return null;
        }

        private static Icon RenderToIcon(int size)
        {
            using (Bitmap bitmap = Render(size))
            {
                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon source = Icon.FromHandle(handle))
                    {
                        return (Icon)source.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        private static Bitmap Render(int size)
        {
            var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                DrawSimplifiedTrayGlyph(g, size);
            }

            return bitmap;
        }

        private static void DrawSimplifiedTrayGlyph(Graphics g, int size)
        {
            float margin = size * 0.12f;
            float panelWidth = size - margin * 2f;
            float panelHeight = panelWidth;
            float panelLeft = (size - panelWidth) / 2f;
            float panelTop = (size - panelHeight) / 2f;
            float radius = Math.Max(2f, size * 0.18f);

            using (GraphicsPath panelPath = CreateRoundedRect(panelLeft, panelTop, panelWidth, panelHeight, radius))
            using (SolidBrush panelBrush = new SolidBrush(Background))
            {
                g.FillPath(panelBrush, panelPath);
            }

            float barWidth = panelWidth * 0.55f;
            float barHeight = Math.Max(2.5f, size * 0.16f);
            float barLeft = panelLeft + (panelWidth - barWidth) / 2f;

            using (SolidBrush cpuBrush = new SolidBrush(CpuAccent))
            {
                g.FillRectangle(cpuBrush, barLeft, panelTop + panelHeight * 0.28f, barWidth, barHeight);
            }

            using (SolidBrush ramBrush = new SolidBrush(RamAccent))
            {
                g.FillRectangle(ramBrush, barLeft, panelTop + panelHeight * 0.58f, barWidth, barHeight);
            }
        }

        private static GraphicsPath CreateRoundedRect(float x, float y, float width, float height, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void WriteIco(string path, params Bitmap[] images)
        {
            var pngData = new List<byte[]>();
            foreach (Bitmap image in images)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    pngData.Add(ms.ToArray());
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
            using (FileStream fs = File.Create(path))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write((short)0);
                writer.Write((short)1);
                writer.Write((short)pngData.Count);

                int offset = 6 + 16 * pngData.Count;
                for (int i = 0; i < images.Length; i++)
                {
                    writer.Write((byte)images[i].Width);
                    writer.Write((byte)images[i].Height);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((short)1);
                    writer.Write((short)32);
                    writer.Write(pngData[i].Length);
                    writer.Write(offset);
                    offset += pngData[i].Length;
                }

                foreach (byte[] data in pngData)
                {
                    writer.Write(data);
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
