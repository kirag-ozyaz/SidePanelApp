using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    internal static class ConnectionRowKey
    {
        public static string Get(ConnectionRow row)
        {
            if (row == null)
            {
                return null;
            }

            return row.ProcessId + "|" + row.Protocol + "|" + row.LocalEndpoint + "|" + row.RemoteEndpoint;
        }

        /// <summary>Ключ процесса (PID + имя), если нужна группировка по приложению.</summary>
        public static string GetProcessKey(ConnectionRow row)
        {
            if (row == null)
            {
                return null;
            }

            if (row.ProcessId <= 0)
            {
                return "0|" + (row.Application ?? "?");
            }

            return row.ProcessId + "|" + (row.Application ?? "?");
        }
    }
}
