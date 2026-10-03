using System;
using System.Collections.Generic;

namespace SidePanelApp.V6.Connections
{
    internal sealed class ConnectionPacketRowStore
    {
        private const int MaxRowsPerConnection = 2500;
        private readonly Dictionary<string, List<PacketListRow>> _rows =
            new Dictionary<string, List<PacketListRow>>(StringComparer.Ordinal);

        public IReadOnlyList<PacketListRow> Get(string connectionKey)
        {
            if (string.IsNullOrEmpty(connectionKey))
            {
                return Array.Empty<PacketListRow>();
            }

            List<PacketListRow> list;
            if (_rows.TryGetValue(connectionKey, out list))
            {
                return list;
            }

            return new List<PacketListRow>();
        }

        public int GetRowCount(string connectionKey)
        {
            return Get(connectionKey).Count;
        }

        public void Add(string connectionKey, PacketListRow row)
        {
            if (string.IsNullOrEmpty(connectionKey) || row == null)
            {
                return;
            }

            List<PacketListRow> list;
            if (!_rows.TryGetValue(connectionKey, out list))
            {
                list = new List<PacketListRow>();
                _rows[connectionKey] = list;
            }

            list.Add(row);
            if (list.Count > MaxRowsPerConnection)
            {
                list.RemoveRange(0, list.Count - MaxRowsPerConnection);
            }
        }

        public void PruneInactive(HashSet<string> activeConnectionKeys)
        {
            if (activeConnectionKeys == null)
            {
                return;
            }

            var remove = new List<string>();
            foreach (string key in _rows.Keys)
            {
                if (!activeConnectionKeys.Contains(key))
                {
                    remove.Add(key);
                }
            }

            foreach (string key in remove)
            {
                _rows.Remove(key);
            }
        }
    }
}
