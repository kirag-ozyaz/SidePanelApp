using System.Collections.Generic;
using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    /// <summary>Сравнение снимков таблицы соединений — «призраки» на один refresh.</summary>
    internal sealed class ConnectionRowVanishTracker
    {
        private readonly Dictionary<string, ConnectionRow> _snapshot =
            new Dictionary<string, ConnectionRow>();

        public List<ConnectionRow> BuildGhosts(IList<ConnectionRow> currentAll)
        {
            var currentKeys = new HashSet<string>();
            if (currentAll != null)
            {
                foreach (ConnectionRow row in currentAll)
                {
                    string key = ConnectionRowKey.Get(row);
                    if (key != null)
                    {
                        currentKeys.Add(key);
                    }
                }
            }

            var ghosts = new List<ConnectionRow>();
            foreach (KeyValuePair<string, ConnectionRow> pair in _snapshot)
            {
                if (currentKeys.Contains(pair.Key))
                {
                    continue;
                }

                ConnectionRow previous = pair.Value;
                if (previous.IsUdp)
                {
                    continue;
                }

                ghosts.Add(CloneVanished(previous));
            }

            ReplaceSnapshot(currentAll);
            return ghosts;
        }

        private void ReplaceSnapshot(IList<ConnectionRow> currentAll)
        {
            _snapshot.Clear();
            if (currentAll == null)
            {
                return;
            }

            foreach (ConnectionRow row in currentAll)
            {
                string key = ConnectionRowKey.Get(row);
                if (key == null)
                {
                    continue;
                }

                _snapshot[key] = CloneForSnapshot(row);
            }
        }

        private static ConnectionRow CloneForSnapshot(ConnectionRow src)
        {
            return new ConnectionRow
            {
                ApplicationIconIndex = src.ApplicationIconIndex,
                Application = src.Application,
                ExecutablePath = src.ExecutablePath,
                Protocol = src.Protocol,
                State = src.State,
                LocalAddress = src.LocalAddress,
                LocalPort = src.LocalPort,
                RemoteAddress = src.RemoteAddress,
                RemotePort = src.RemotePort,
                LocalEndpoint = src.LocalEndpoint,
                RemoteEndpoint = src.RemoteEndpoint,
                ProcessId = src.ProcessId,
                User = src.User,
                IsListening = src.IsListening,
                IsLoopback = src.IsLoopback,
                IsUdp = src.IsUdp,
                SentPackets = src.SentPackets,
                SentBytes = src.SentBytes,
                RcvdPackets = src.RcvdPackets,
                RcvdBytes = src.RcvdBytes
            };
        }

        private static ConnectionRow CloneVanished(ConnectionRow src)
        {
            ConnectionRow row = CloneForSnapshot(src);
            row.VanishedWhileEstablished = src.State == "ESTABLISHED";
            row.VanishedWhileListening = src.IsListening && !src.IsUdp;
            row.IsRecentlyClosed = true;
            row.State = "CLOSED";
            row.SentPackets = null;
            row.SentBytes = null;
            row.RcvdPackets = null;
            row.RcvdBytes = null;
            return row;
        }
    }
}
