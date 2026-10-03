using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    internal static class ConnectionsGridHelper
    {
        public const int MaxVisibleRows = 2000;
        public const int MaxUniqueIcons = 48;

        internal sealed class RefreshPayload
        {
            public List<ConnectionRow> AllRows { get; set; }
            public List<ConnectionRow> DisplayRows { get; set; }
            public int FilteredTotal { get; set; }
            public List<Image> ExtraIcons { get; set; } = new List<Image>();
        }

        public static RefreshPayload Build(ConnectionsForm.ConnectionFilter filter)
        {
            List<ConnectionRow> all = TcpUdpTableReader.ReadAll();
            ConnectionsEnricher.ApplyCachedNamesOnly(all);

            List<ConnectionRow> filtered = Filter(all, filter)
                .OrderBy(r => r.Application)
                .ThenBy(r => r.LocalEndpoint)
                .ToList();

            var payload = new RefreshPayload
            {
                AllRows = all,
                FilteredTotal = filtered.Count
            };

            if (filtered.Count > MaxVisibleRows)
            {
                filtered = filtered.Take(MaxVisibleRows).ToList();
            }

            payload.DisplayRows = filtered;
            AssignIconIndices(filtered, payload.ExtraIcons);
            return payload;
        }

        public static List<ConnectionRow> FilterDisplayList(
            IList<ConnectionRow> allRows,
            ConnectionsForm.ConnectionFilter filter,
            out int filteredTotal)
        {
            List<ConnectionRow> filtered = Filter(allRows, filter)
                .OrderBy(r => r.Application)
                .ThenBy(r => r.LocalEndpoint)
                .ToList();

            filteredTotal = filtered.Count;
            if (filtered.Count > MaxVisibleRows)
            {
                filtered = filtered.Take(MaxVisibleRows).ToList();
            }

            return filtered;
        }

        public static void AssignIconIndices(IList<ConnectionRow> rows, List<Image> extraIcons)
        {
            extraIcons.Clear();
            var pathToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (ConnectionRow row in rows)
            {
                row.ApplicationIconIndex = 0;
                if (row.ProcessId <= 0)
                {
                    continue;
                }

                string path = row.ExecutablePath;
                if (string.IsNullOrEmpty(path))
                {
                    path = ProcessIconCache.TryGetExecutablePath(row.ProcessId);
                }

                if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(row.Application))
                {
                    path = ProcessIconCache.GuessPathFromProcessName(row.Application);
                }

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (!pathToIndex.TryGetValue(path, out int iconIndex))
                {
                    if (extraIcons.Count >= MaxUniqueIcons)
                    {
                        continue;
                    }

                    Image icon = ProcessIconCache.GetIconForPath(path);
                    if (icon == null)
                    {
                        continue;
                    }

                    extraIcons.Add(icon);
                    iconIndex = extraIcons.Count;
                    pathToIndex[path] = iconIndex;
                }

                row.ApplicationIconIndex = iconIndex;
            }
        }

        public static bool MatchesFilter(ConnectionRow row, ConnectionsForm.ConnectionFilter filter)
        {
            if (row == null)
            {
                return false;
            }

            switch (filter)
            {
                case ConnectionsForm.ConnectionFilter.Connected:
                    return row.State == "ESTABLISHED"
                        || (row.IsRecentlyClosed && row.VanishedWhileEstablished);
                case ConnectionsForm.ConnectionFilter.Listening:
                    return (row.IsListening && !row.IsUdp)
                        || (row.IsRecentlyClosed && row.VanishedWhileListening);
                case ConnectionsForm.ConnectionFilter.Udp:
                    return row.IsUdp;
                case ConnectionsForm.ConnectionFilter.Local:
                    return row.IsLoopback;
                default:
                    return true;
            }
        }

        public static List<ConnectionRow> MergeVanishedOnTop(
            IList<ConnectionRow> vanished,
            List<ConnectionRow> liveFiltered)
        {
            var merged = new List<ConnectionRow>();
            if (vanished != null)
            {
                foreach (ConnectionRow row in vanished)
                {
                    merged.Add(row);
                }
            }

            if (liveFiltered != null)
            {
                merged.AddRange(liveFiltered);
            }

            if (merged.Count <= MaxVisibleRows)
            {
                return merged;
            }

            int vanishedCount = vanished != null ? vanished.Count : 0;
            int liveKeep = Math.Max(0, MaxVisibleRows - vanishedCount);
            var trimmed = new List<ConnectionRow>();
            if (vanished != null)
            {
                trimmed.AddRange(vanished);
            }

            if (liveFiltered != null && liveKeep > 0)
            {
                trimmed.AddRange(liveFiltered.GetRange(0, Math.Min(liveKeep, liveFiltered.Count)));
            }

            return trimmed;
        }

        private static IEnumerable<ConnectionRow> Filter(IList<ConnectionRow> rows, ConnectionsForm.ConnectionFilter filter)
        {
            return rows.Where(r => MatchesFilter(r, filter));
        }
    }
}
