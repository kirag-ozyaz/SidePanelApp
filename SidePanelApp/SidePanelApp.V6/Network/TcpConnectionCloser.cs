using System;
using System.Runtime.InteropServices;

namespace SidePanelApp.V6.Network
{
    internal static class TcpConnectionCloser
    {
        private const uint DeleteTcb = 12;

        public static bool TryClose(ConnectionRow row, out string error, out int win32Error)
        {
            error = null;
            win32Error = 0;
            if (row == null || row.IsUdp)
            {
                error = "Только для TCP.";
                return false;
            }

            if (row.IsListening)
            {
                error = "Нельзя закрыть LISTENING.";
                return false;
            }

            try
            {
                if (row.IsTcp6)
                {
                    return TryCloseTcp6(row, out error, out win32Error);
                }

                return TryCloseTcp4(row, out error, out win32Error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryCloseTcp4(ConnectionRow row, out string error, out int win32Error)
        {
            error = null;
            win32Error = 0;
            var mib = new MibTcpRow
            {
                State = DeleteTcb,
                LocalAddr = row.LocalAddressV4,
                LocalPort = unchecked((uint)row.LocalPortNetwork),
                RemoteAddr = row.RemoteAddressV4,
                RemotePort = unchecked((uint)row.RemotePortNetwork)
            };

            int result = SetTcpEntry(ref mib);
            if (result != 0)
            {
                win32Error = result;
                error = FormatWin32Error("SetTcpEntry", result);
                return false;
            }

            return true;
        }

        private static bool TryCloseTcp6(ConnectionRow row, out string error, out int win32Error)
        {
            error = null;
            win32Error = 0;
            if (row.LocalAddressV6 == null || row.RemoteAddressV6 == null)
            {
                error = "Нет адреса IPv6.";
                return false;
            }

            var mib = new MibTcp6Row
            {
                State = DeleteTcb,
                LocalAddr = (byte[])row.LocalAddressV6.Clone(),
                LocalScopeId = row.LocalScopeIdV6,
                LocalPort = unchecked((uint)row.LocalPortNetwork),
                RemoteAddr = (byte[])row.RemoteAddressV6.Clone(),
                RemoteScopeId = row.RemoteScopeIdV6,
                RemotePort = unchecked((uint)row.RemotePortNetwork)
            };

            int result = SetTcp6Entry(ref mib);
            if (result != 0)
            {
                win32Error = result;
                error = FormatWin32Error("SetTcp6Entry", result);
                return false;
            }

            return true;
        }

        private static string FormatWin32Error(string api, int code)
        {
            if (code == 5)
            {
                return api + ": отказано в доступе (код 5). Нужны права администратора.";
            }

            return api + ": код ошибки " + code;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcpRow
        {
            public uint State;
            public uint LocalAddr;
            public uint LocalPort;
            public uint RemoteAddr;
            public uint RemotePort;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MibTcp6Row
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
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int SetTcpEntry(ref MibTcpRow tcpRow);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int SetTcp6Entry(ref MibTcp6Row tcpRow);
    }
}
