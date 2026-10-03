using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace SidePanelApp.V6.Network
{
    internal static class ConnectionsEnricher
    {
        private static readonly Dictionary<int, CachedProcessInfo> Cache =
            new Dictionary<int, CachedProcessInfo>();

        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan UserRetryTtl = TimeSpan.FromSeconds(8);

        /// <summary>Мгновенно: только кэш, иначе «(PID n)» — без вызовов WMI/OpenProcess.</summary>
        public static void ApplyCachedNamesOnly(IList<ConnectionRow> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            PruneCache(now);
            foreach (ConnectionRow row in rows)
            {
                int pid = row.ProcessId;
                if (pid <= 0)
                {
                    row.Application = "—";
                    row.User = "—";
                    continue;
                }

                if (Cache.TryGetValue(pid, out CachedProcessInfo cached) && !cached.IsExpired(now))
                {
                    row.Application = FormatApplicationName(cached.Name);
                    row.User = cached.UserResolved ? cached.User : "—";
                    row.ExecutablePath = cached.ExecutablePath;
                }
                else
                {
                    row.Application = "(PID " + pid + ")";
                    row.User = "—";
                }
            }
        }

        /// <summary>Догрузка имён процессов в фоне (лимит новых PID за проход).</summary>
        public static void EnrichProcessNames(IList<ConnectionRow> rows, int maxNewResolves, bool resolveUsers, int maxUserLookups = 0)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            PruneCache(now);
            var pending = new List<int>();
            foreach (ConnectionRow row in rows)
            {
                int pid = row.ProcessId;
                if (pid <= 0)
                {
                    continue;
                }

                if (!Cache.TryGetValue(pid, out CachedProcessInfo cached) || cached.IsExpired(now))
                {
                    if (!pending.Contains(pid))
                    {
                        pending.Add(pid);
                    }
                }
            }

            int resolvesLeft = maxNewResolves;
            foreach (int pid in pending)
            {
                if (resolvesLeft <= 0)
                {
                    break;
                }

                var cached = new CachedProcessInfo
                {
                    Name = ResolveProcessName(pid),
                    ExecutablePath = ProcessIconCache.TryGetExecutablePath(pid),
                    User = "—",
                    ExpiresUtc = now.Add(CacheTtl),
                    UserResolved = false
                };
                Cache[pid] = cached;
                resolvesLeft--;
            }

            int userLeft = maxUserLookups;
            foreach (ConnectionRow row in rows)
            {
                int pid = row.ProcessId;
                if (pid <= 0)
                {
                    continue;
                }

                if (!Cache.TryGetValue(pid, out CachedProcessInfo cached))
                {
                    continue;
                }

                if (resolveUsers && !cached.UserResolved && userLeft > 0)
                {
                    cached.User = ResolveProcessUser(pid);
                    cached.UserResolved = true;
                    userLeft--;
                }

                row.Application = FormatApplicationName(cached.Name);
                row.User = cached.UserResolved ? cached.User : "—";
                row.ExecutablePath = cached.ExecutablePath;
            }
        }

        /// <param name="maxNewUserLookups">WMI GetOwner за один проход — дорого, лимит на кадр обновления.</param>
        public static void Enrich(IList<ConnectionRow> rows, int maxNewUserLookups = 40)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            PruneCache(now);
            int userLookupsLeft = maxNewUserLookups;

            foreach (ConnectionRow row in rows)
            {
                int pid = row.ProcessId;
                if (pid <= 0)
                {
                    row.Application = "—";
                    row.User = "—";
                    continue;
                }

                if (!Cache.TryGetValue(pid, out CachedProcessInfo cached) || cached.IsExpired(now))
                {
                    cached = new CachedProcessInfo
                    {
                        Name = ResolveProcessName(pid),
                        ExecutablePath = ProcessIconCache.TryGetExecutablePath(pid),
                        User = "—",
                        ExpiresUtc = now.Add(CacheTtl),
                        UserResolved = false
                    };
                    Cache[pid] = cached;
                }

                if (!cached.UserResolved && userLookupsLeft > 0)
                {
                    cached.User = ResolveProcessUser(pid);
                    cached.UserResolved = true;
                    cached.ExpiresUtc = now.Add(CacheTtl);
                    userLookupsLeft--;
                }
                else if (!cached.UserResolved)
                {
                    cached.ExpiresUtc = now.Add(UserRetryTtl);
                }

                row.Application = FormatApplicationName(cached.Name);
                row.User = cached.User;
                row.ExecutablePath = cached.ExecutablePath;
            }
        }

        public static void EnrichSingle(ConnectionRow row)
        {
            if (row == null || row.ProcessId <= 0)
            {
                return;
            }

            Enrich(new List<ConnectionRow> { row }, 1);
        }

        private static void PruneCache(DateTime now)
        {
            if (Cache.Count < 500)
            {
                return;
            }

            var remove = new List<int>();
            foreach (KeyValuePair<int, CachedProcessInfo> pair in Cache)
            {
                if (pair.Value.IsExpired(now))
                {
                    remove.Add(pair.Key);
                }
            }

            foreach (int pid in remove)
            {
                Cache.Remove(pid);
            }
        }

        private static string FormatApplicationName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "—";
            }

            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }

            return name + ".exe";
        }

        private static string ResolveProcessName(int pid)
        {
            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    return process.ProcessName;
                }
            }
            catch
            {
                return "PID " + pid;
            }
        }

        private static string ResolveProcessUser(int pid)
        {
            try
            {
                string query = "SELECT * FROM Win32_Process WHERE ProcessId = " + pid;
                using (var searcher = new ManagementObjectSearcher(query))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        object[] owner = new object[2];
                        obj.InvokeMethod("GetOwner", owner);
                        string user = owner[0] as string;
                        string domain = owner[1] as string;
                        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(domain))
                        {
                            return domain + "\\" + user;
                        }

                        if (!string.IsNullOrEmpty(user))
                        {
                            return user;
                        }
                    }
                }
            }
            catch
            {
            }

            return "—";
        }

        private sealed class CachedProcessInfo
        {
            public string Name;
            public string ExecutablePath;
            public string User;
            public DateTime ExpiresUtc;
            public bool UserResolved;

            public bool IsExpired(DateTime now)
            {
                return now >= ExpiresUtc;
            }
        }
    }
}
