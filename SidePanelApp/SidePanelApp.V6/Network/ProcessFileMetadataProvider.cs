using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SidePanelApp.V6.Network
{
    internal sealed class ProcessFileMetadata
    {
        public string ExecutablePath { get; set; }
        public string FileDescription { get; set; }
        public string CompanyName { get; set; }
        public string FileVersion { get; set; }
    }

    internal static class ProcessFileMetadataProvider
    {
        private static readonly Dictionary<string, ProcessFileMetadata> CacheByPath =
            new Dictionary<string, ProcessFileMetadata>(StringComparer.OrdinalIgnoreCase);

        private static readonly object Sync = new object();

        public static ProcessFileMetadata Get(int processId, string knownPath)
        {
            string path = knownPath;
            if (string.IsNullOrEmpty(path) && processId > 0)
            {
                path = ProcessIconCache.TryGetExecutablePath(processId);
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return Empty(path);
            }

            lock (Sync)
            {
                if (CacheByPath.TryGetValue(path, out ProcessFileMetadata cached))
                {
                    return cached;
                }
            }

            ProcessFileMetadata meta = ReadFromDisk(path);
            lock (Sync)
            {
                CacheByPath[path] = meta;
            }

            return meta;
        }

        private static ProcessFileMetadata ReadFromDisk(string path)
        {
            var meta = new ProcessFileMetadata { ExecutablePath = path };
            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(path);
                meta.FileDescription = TrimOrNull(info.FileDescription);
                meta.CompanyName = TrimOrNull(info.CompanyName);
                meta.FileVersion = TrimOrNull(info.FileVersion);
                if (string.IsNullOrEmpty(meta.FileVersion))
                {
                    meta.FileVersion = TrimOrNull(info.ProductVersion);
                }
            }
            catch
            {
            }

            return meta;
        }

        private static ProcessFileMetadata Empty(string path)
        {
            return new ProcessFileMetadata
            {
                ExecutablePath = path ?? string.Empty,
                FileDescription = string.Empty,
                CompanyName = string.Empty,
                FileVersion = string.Empty
            };
        }

        private static string TrimOrNull(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim();
        }
    }
}
