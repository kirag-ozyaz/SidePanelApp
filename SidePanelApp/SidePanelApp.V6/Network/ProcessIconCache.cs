using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SidePanelApp.V6.Network
{
    internal static class ProcessIconCache
    {
        private const int ProcessQueryLimitedInformation = 0x1000;

        private static readonly Dictionary<string, Image> IconsByPath =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<int, string> PathByPid = new Dictionary<int, string>();
        private static readonly object Sync = new object();

        public static string TryGetExecutablePath(int pid)
        {
            if (pid <= 0)
            {
                return null;
            }

            lock (Sync)
            {
                if (PathByPid.TryGetValue(pid, out string cached))
                {
                    return cached;
                }
            }

            string path = QueryImageName(pid);
            if (string.IsNullOrEmpty(path))
            {
                try
                {
                    using (Process process = Process.GetProcessById(pid))
                    {
                        path = process.MainModule?.FileName;
                    }
                }
                catch
                {
                }
            }

            lock (Sync)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    PathByPid[pid] = path;
                }
            }

            return path;
        }

        public static string GuessPathFromProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return null;
            }

            string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string name = processName.Trim();
            if (name.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Idle", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(system, "ntoskrnl.exe");
            }

            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(system, name);
            }

            return Path.Combine(system, name + ".exe");
        }

        public static Image GetIconForPid(int pid, int size = 16)
        {
            string path = TryGetExecutablePath(pid);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return GetIconForPath(path, size);
        }

        public static Image GetIconForPath(string path, int size = 16)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string fullPath = path;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
            }

            lock (Sync)
            {
                if (IconsByPath.TryGetValue(fullPath, out Image cached))
                {
                    return cached;
                }
            }

            Image icon = ExtractIcon(fullPath, size);
            if (icon == null && File.Exists(fullPath))
            {
                icon = ExtractIconViaShell(fullPath, size);
            }

            if (icon == null)
            {
                return null;
            }

            lock (Sync)
            {
                if (!IconsByPath.ContainsKey(fullPath))
                {
                    IconsByPath[fullPath] = icon;
                }
                else
                {
                    icon.Dispose();
                    icon = IconsByPath[fullPath];
                }
            }

            return icon;
        }

        private static string QueryImageName(int pid)
        {
            IntPtr handle = OpenProcess(ProcessQueryLimitedInformation, false, pid);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var builder = new StringBuilder(1024);
                int size = builder.Capacity;
                if (QueryFullProcessImageName(handle, 0, builder, ref size))
                {
                    return builder.ToString();
                }
            }
            finally
            {
                CloseHandle(handle);
            }

            return null;
        }

        private static Image ExtractIcon(string path, int size)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                using (Icon associated = Icon.ExtractAssociatedIcon(path))
                {
                    if (associated == null)
                    {
                        return null;
                    }

                    using (Bitmap source = associated.ToBitmap())
                    {
                        return new Bitmap(source, new Size(size, size));
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static Image ExtractIconViaShell(string path, int size)
        {
            try
            {
                var info = new SHFILEINFO();
                IntPtr result = SHGetFileInfo(
                    path,
                    0,
                    ref info,
                    (uint)Marshal.SizeOf(info),
                    SHGFI_ICON | SHGFI_SMALLICON);

                if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    using (Icon icon = Icon.FromHandle(info.hIcon))
                    using (Bitmap source = icon.ToBitmap())
                    {
                        return new Bitmap(source, new Size(size, size));
                    }
                }
                finally
                {
                    DestroyIcon(info.hIcon);
                }
            }
            catch
            {
                return null;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool QueryFullProcessImageName(
            IntPtr hProcess,
            int dwFlags,
            StringBuilder lpExeName,
            ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
    }
}
