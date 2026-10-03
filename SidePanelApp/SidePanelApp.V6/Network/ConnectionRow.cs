namespace SidePanelApp.V6.Network
{
    internal sealed class ConnectionRow
    {
        public int ApplicationIconIndex { get; set; }
        public string Application { get; set; }
        public string ExecutablePath { get; set; }
        public string Protocol { get; set; }
        public string State { get; set; }
        public string LocalAddress { get; set; }
        public string LocalPort { get; set; }
        public string RemoteAddress { get; set; }
        public string RemotePort { get; set; }
        public string LocalEndpoint { get; set; }
        public string RemoteEndpoint { get; set; }
        public int ProcessId { get; set; }
        public string User { get; set; }
        public bool IsListening { get; set; }
        public bool IsLoopback { get; set; }
        public bool IsUdp { get; set; }

        /// <summary>Строка исчезла из таблицы ядра — показать один кадр красным (TCPView).</summary>
        public bool IsRecentlyClosed { get; set; }
        public bool VanishedWhileEstablished { get; set; }
        public bool VanishedWhileListening { get; set; }

        public long? SentPackets { get; set; }
        public long? SentBytes { get; set; }
        public long? RcvdPackets { get; set; }
        public long? RcvdBytes { get; set; }

        public string SentPacketsDisplay => FormatCounter(SentPackets);
        public string SentBytesDisplay => FormatCounter(SentBytes);
        public string RcvdPacketsDisplay => FormatCounter(RcvdPackets);
        public string RcvdBytesDisplay => FormatCounter(RcvdBytes);

        private static string FormatCounter(long? value)
        {
            return value.HasValue ? value.Value.ToString() : string.Empty;
        }

        internal bool IsTcp6 { get; set; }
        internal uint TcpStateCode { get; set; }
        internal uint LocalAddressV4 { get; set; }
        internal uint RemoteAddressV4 { get; set; }
        internal int LocalPortNetwork { get; set; }
        internal int RemotePortNetwork { get; set; }
        internal byte[] LocalAddressV6 { get; set; }
        internal byte[] RemoteAddressV6 { get; set; }
        internal uint LocalScopeIdV6 { get; set; }
        internal uint RemoteScopeIdV6 { get; set; }
    }
}
