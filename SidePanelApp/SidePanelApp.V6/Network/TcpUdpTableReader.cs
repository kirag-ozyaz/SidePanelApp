using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

namespace SidePanelApp.V6.Network
{
    internal static class TcpUdpTableReader
    {
        public static List<ConnectionRow> ReadAll()
        {
            var rows = new List<ConnectionRow>();
            rows.AddRange(ReadTcp4());
            rows.AddRange(ReadTcp6());
            rows.AddRange(ReadUdp4());
            rows.AddRange(ReadUdp6());
            return rows;
        }

        private static IEnumerable<ConnectionRow> ReadTcp4()
        {
            int bufferSize = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, 2, TcpTableClass.OwnerPidAll, 0);
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                uint result = GetExtendedTcpTable(buffer, ref bufferSize, true, 2, TcpTableClass.OwnerPidAll, 0);
                if (result != 0)
                {
                    yield break;
                }

                int rowStructSize = Marshal.SizeOf(typeof(MibTcpRowOwnerPid));
                int count = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = buffer + 4;
                for (int i = 0; i < count; i++)
                {
                    MibTcpRowOwnerPid row = (MibTcpRowOwnerPid)Marshal.PtrToStructure(rowPtr, typeof(MibTcpRowOwnerPid));
                    rowPtr += rowStructSize;
                    yield return MapTcp4Row(row);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static IEnumerable<ConnectionRow> ReadTcp6()
        {
            int bufferSize = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, 23, TcpTableClass.OwnerPidAll, 0);
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                uint result = GetExtendedTcpTable(buffer, ref bufferSize, true, 23, TcpTableClass.OwnerPidAll, 0);
                if (result != 0)
                {
                    yield break;
                }

                int rowStructSize = Marshal.SizeOf(typeof(MibTcp6RowOwnerPid));
                int count = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = buffer + 4;
                for (int i = 0; i < count; i++)
                {
                    MibTcp6RowOwnerPid row = (MibTcp6RowOwnerPid)Marshal.PtrToStructure(rowPtr, typeof(MibTcp6RowOwnerPid));
                    rowPtr += rowStructSize;
                    yield return MapTcp6Row(row);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static IEnumerable<ConnectionRow> ReadUdp4()
        {
            int bufferSize = 0;
            GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, 2, UdpTableClass.OwnerPid, 0);
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                uint result = GetExtendedUdpTable(buffer, ref bufferSize, true, 2, UdpTableClass.OwnerPid, 0);
                if (result != 0)
                {
                    yield break;
                }

                int rowStructSize = Marshal.SizeOf(typeof(MibUdpRowOwnerPid));
                int count = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = buffer + 4;
                for (int i = 0; i < count; i++)
                {
                    MibUdpRowOwnerPid row = (MibUdpRowOwnerPid)Marshal.PtrToStructure(rowPtr, typeof(MibUdpRowOwnerPid));
                    rowPtr += rowStructSize;
                    yield return MapUdp4Row(row);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static IEnumerable<ConnectionRow> ReadUdp6()
        {
            int bufferSize = 0;
            GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, 23, UdpTableClass.OwnerPid, 0);
            IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
            try
            {
                uint result = GetExtendedUdpTable(buffer, ref bufferSize, true, 23, UdpTableClass.OwnerPid, 0);
                if (result != 0)
                {
                    yield break;
                }

                int rowStructSize = Marshal.SizeOf(typeof(MibUdp6RowOwnerPid));
                int count = Marshal.ReadInt32(buffer);
                IntPtr rowPtr = buffer + 4;
                for (int i = 0; i < count; i++)
                {
                    MibUdp6RowOwnerPid row = (MibUdp6RowOwnerPid)Marshal.PtrToStructure(rowPtr, typeof(MibUdp6RowOwnerPid));
                    rowPtr += rowStructSize;
                    yield return MapUdp6Row(row);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static ConnectionRow MapTcp4Row(MibTcpRowOwnerPid row)
        {
            int localPort = ToNetworkPort(row.LocalPort);
            int remotePort = ToNetworkPort(row.RemotePort);
            int stateCode = unchecked((int)row.State);
            string state = MapTcpState(stateCode);
            bool listening = stateCode == (int)TcpState.Listen;
            bool remoteWildcard = listening || row.RemoteAddr == 0;
            string localAddr = FormatDisplayAddressV4(row.LocalAddr);
            string remoteAddr = remoteWildcard ? "*" : FormatDisplayAddressV4(row.RemoteAddr);
            string localPortText = FormatPortDisplay(localPort);
            string remotePortText = remoteWildcard ? "*" : FormatPortDisplay(remotePort);
            string local = localAddr + ":" + localPort;
            string remote = remoteWildcard ? "*:*" : FormatEndpointV4(row.RemoteAddr, row.RemotePort);
            return new ConnectionRow
            {
                Protocol = "TCP",
                State = state,
                LocalAddress = localAddr,
                LocalPort = localPortText,
                RemoteAddress = remoteAddr,
                RemotePort = remotePortText,
                LocalEndpoint = local,
                RemoteEndpoint = remote,
                ProcessId = row.OwningPid,
                IsListening = listening,
                IsLoopback = IsLoopbackEndpoint(local) || IsLoopbackEndpoint(remote),
                IsUdp = false,
                IsTcp6 = false,
                TcpStateCode = row.State,
                LocalAddressV4 = row.LocalAddr,
                RemoteAddressV4 = row.RemoteAddr,
                LocalPortNetwork = row.LocalPort,
                RemotePortNetwork = row.RemotePort
            };
        }

        private static ConnectionRow MapTcp6Row(MibTcp6RowOwnerPid row)
        {
            int localPort = ToNetworkPort((int)row.LocalPort);
            int remotePort = ToNetworkPort((int)row.RemotePort);
            string state = MapTcpState((int)row.State);
            bool listening = row.State == (uint)TcpState.Listen;
            bool remoteWildcard = listening || IsUnspecifiedV6(row.RemoteAddr);
            string localAddr = FormatDisplayAddressV6(row.LocalAddr);
            string remoteAddr = remoteWildcard ? "*" : FormatDisplayAddressV6(row.RemoteAddr);
            string localPortText = FormatPortDisplay(localPort);
            string remotePortText = remoteWildcard ? "*" : FormatPortDisplay(remotePort);
            string local = FormatEndpointV6(row.LocalAddr, row.LocalPort);
            string remote = remoteWildcard ? "*:*" : FormatEndpointV6(row.RemoteAddr, row.RemotePort);
            return new ConnectionRow
            {
                Protocol = "TCPV6",
                State = state,
                LocalAddress = localAddr,
                LocalPort = localPortText,
                RemoteAddress = remoteAddr,
                RemotePort = remotePortText,
                LocalEndpoint = local,
                RemoteEndpoint = remote,
                ProcessId = (int)row.OwningPid,
                IsListening = listening,
                IsLoopback = IsLoopbackEndpoint(local) || IsLoopbackEndpoint(remote),
                IsUdp = false,
                IsTcp6 = true,
                TcpStateCode = row.State,
                LocalAddressV6 = (byte[])row.LocalAddr.Clone(),
                RemoteAddressV6 = (byte[])row.RemoteAddr.Clone(),
                LocalScopeIdV6 = row.LocalScopeId,
                RemoteScopeIdV6 = row.RemoteScopeId,
                LocalPortNetwork = (int)row.LocalPort,
                RemotePortNetwork = (int)row.RemotePort
            };
        }

        private static ConnectionRow MapUdp4Row(MibUdpRowOwnerPid row)
        {
            int localPort = ToNetworkPort(row.LocalPort);
            string localAddr = FormatDisplayAddressV4(row.LocalAddr);
            string local = FormatEndpointV4(row.LocalAddr, row.LocalPort);
            return new ConnectionRow
            {
                Protocol = "UDP",
                State = string.Empty,
                LocalAddress = localAddr,
                LocalPort = FormatPortDisplay(localPort),
                RemoteAddress = "*",
                RemotePort = "*",
                LocalEndpoint = local,
                RemoteEndpoint = "*:*",
                ProcessId = row.OwningPid,
                IsListening = true,
                IsLoopback = IsLoopbackEndpoint(local),
                IsUdp = true
            };
        }

        private static ConnectionRow MapUdp6Row(MibUdp6RowOwnerPid row)
        {
            int localPort = ToNetworkPort((int)row.LocalPort);
            string local = FormatEndpointV6(row.LocalAddr, row.LocalPort);
            return new ConnectionRow
            {
                Protocol = "UDPV6",
                State = string.Empty,
                LocalAddress = FormatDisplayAddressV6(row.LocalAddr),
                LocalPort = FormatPortDisplay(localPort),
                RemoteAddress = "*",
                RemotePort = "*",
                LocalEndpoint = local,
                RemoteEndpoint = "*:*",
                ProcessId = (int)row.OwningPid,
                IsListening = true,
                IsLoopback = IsLoopbackEndpoint(local),
                IsUdp = true,
                IsTcp6 = true
            };
        }

        /// <summary>Коды MIB_TCP_STATE (Windows) → подписи как в TCPView.</summary>
        private static string MapTcpState(int state)
        {
            switch ((TcpState)state)
            {
                case TcpState.Closed:
                    return "CLOSED";
                case TcpState.Listen:
                    return "LISTENING";
                case TcpState.SynSent:
                    return "SYN_SENT";
                case TcpState.SynReceived:
                    return "SYN_RECEIVED";
                case TcpState.Established:
                    return "ESTABLISHED";
                case TcpState.FinWait1:
                    return "FIN_WAIT_1";
                case TcpState.FinWait2:
                    return "FIN_WAIT_2";
                case TcpState.CloseWait:
                    return "CLOSE_WAIT";
                case TcpState.Closing:
                    return "CLOSING";
                case TcpState.LastAck:
                    return "LAST_ACK";
                case TcpState.TimeWait:
                    return "TIME_WAIT";
                case TcpState.DeleteTcb:
                    return "DELETE_TCB";
                default:
                    return "UNKNOWN";
            }
        }

        private static int ToNetworkPort(int port)
        {
            return ((port & 0xFF) << 8) | ((port >> 8) & 0xFF);
        }

        private static string FormatEndpointV4(uint addr, int port)
        {
            return new IPAddress(addr).ToString() + ":" + ToNetworkPort(port);
        }

        private static string FormatEndpointV6(byte[] addr, uint port)
        {
            return "[" + new IPAddress(addr).ToString() + "]:" + ToNetworkPort((int)port);
        }

        private static string FormatDisplayAddressV4(uint addr)
        {
            if (addr == 0)
            {
                return Environment.MachineName.ToLowerInvariant();
            }

            return new IPAddress(addr).ToString();
        }

        private static string FormatDisplayAddressV6(byte[] addr)
        {
            if (IsUnspecifiedV6(addr))
            {
                return Environment.MachineName.ToLowerInvariant();
            }

            return new IPAddress(addr).ToString();
        }

        private static bool IsUnspecifiedV6(byte[] addr)
        {
            if (addr == null || addr.Length != 16)
            {
                return true;
            }

            for (int i = 0; i < 16; i++)
            {
                if (addr[i] != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string FormatPortDisplay(int port)
        {
            return port.ToString();
        }

        private static bool IsLoopbackEndpoint(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                return false;
            }

            return endpoint.StartsWith("127.", StringComparison.Ordinal) ||
                   endpoint.StartsWith("[::1]", StringComparison.OrdinalIgnoreCase) ||
                   endpoint.StartsWith("[0:0:0:0:0:0:0:1]", StringComparison.OrdinalIgnoreCase);
        }

        private enum TcpTableClass
        {
            OwnerPidAll = 5
        }

        private enum UdpTableClass
        {
            OwnerPid = 1
        }

        private enum TcpState
        {
            Closed = 1,
            Listen = 2,
            SynSent = 3,
            SynReceived = 4,
            Established = 5,
            FinWait1 = 6,
            FinWait2 = 7,
            CloseWait = 8,
            Closing = 9,
            LastAck = 10,
            TimeWait = 11,
            DeleteTcb = 12
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpRowOwnerPid
        {
            public uint State;
            public uint LocalAddr;
            public int LocalPort;
            public uint RemoteAddr;
            public int RemotePort;
            public int OwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibUdpRowOwnerPid
        {
            public uint LocalAddr;
            public int LocalPort;
            public int OwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcp6RowOwnerPid
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] LocalAddr;
            public uint LocalScopeId;
            public uint LocalPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] RemoteAddr;
            public uint RemoteScopeId;
            public uint RemotePort;
            public uint State;
            public uint OwningPid;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibUdp6RowOwnerPid
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] LocalAddr;
            public uint LocalScopeId;
            public uint LocalPort;
            public uint OwningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            TcpTableClass tblClass,
            int reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(
            IntPtr pTcpTable,
            ref int dwOutBufLen,
            bool sort,
            int ipVersion,
            UdpTableClass tblClass,
            int reserved);
    }
}
