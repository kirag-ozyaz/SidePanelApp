using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SidePanelApp.V6.Network
{
    /// <summary>Счётчики TCP (GetPerTcpConnectionEStats — 11 параметров: Rw, Ros, Rod).</summary>
    internal static class TcpConnectionStatsEnricher
    {
        // TCP_ESTATS_TYPE::TcpConnectionEstatsData == 1 (не 3 — это Path).
        private const int TcpEstatsData = 1;
        private const int MaxStatsPerRefresh = 120;
        private static bool _apiUnavailable;

        public static void Enrich(IList<ConnectionRow> rows)
        {
            if (_apiUnavailable || rows == null || rows.Count == 0)
            {
                return;
            }

            if (!Environment.Is64BitProcess)
            {
                _apiUnavailable = true;
                return;
            }

            try
            {
                int done = 0;
                foreach (ConnectionRow row in rows)
                {
                    if (row.IsUdp || done >= MaxStatsPerRefresh)
                    {
                        continue;
                    }

                    if (TryReadStats(row, out TcpStats stats))
                    {
                        row.SentPackets = stats.SegmentsOut;
                        row.SentBytes = stats.BytesOut;
                        row.RcvdPackets = stats.SegmentsIn;
                        row.RcvdBytes = stats.BytesIn;
                        done++;
                    }
                }
            }
            catch
            {
                _apiUnavailable = true;
            }
        }

        public static void EnrichSingle(ConnectionRow row)
        {
            if (_apiUnavailable || row == null || row.IsUdp || !Environment.Is64BitProcess)
            {
                return;
            }

            if (TryReadStats(row, out TcpStats stats))
            {
                row.SentPackets = stats.SegmentsOut;
                row.SentBytes = stats.BytesOut;
                row.RcvdPackets = stats.SegmentsIn;
                row.RcvdBytes = stats.BytesIn;
            }
        }

        private static bool TryReadStats(ConnectionRow row, out TcpStats stats)
        {
            stats = default(TcpStats);
            try
            {
                if (row.IsTcp6)
                {
                    return TryReadTcp6(row, out stats);
                }

                return TryReadTcp4(row, out stats);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReadTcp4(ConnectionRow row, out TcpStats stats)
        {
            stats = default(TcpStats);
            var mib = new MibTcpRow
            {
                State = row.TcpStateCode,
                LocalAddr = row.LocalAddressV4,
                LocalPort = unchecked((uint)row.LocalPortNetwork),
                RemoteAddr = row.RemoteAddressV4,
                RemotePort = unchecked((uint)row.RemotePortNetwork)
            };

            TryEnableTcp4(ref mib);
            return ReadRodTcp4(ref mib, out stats);
        }

        private static bool TryReadTcp6(ConnectionRow row, out TcpStats stats)
        {
            stats = default(TcpStats);
            if (row.LocalAddressV6 == null || row.RemoteAddressV6 == null)
            {
                return false;
            }

            var mib = new MibTcp6Row
            {
                LocalAddr = (byte[])row.LocalAddressV6.Clone(),
                LocalScopeId = row.LocalScopeIdV6,
                LocalPort = unchecked((uint)row.LocalPortNetwork),
                RemoteAddr = (byte[])row.RemoteAddressV6.Clone(),
                RemoteScopeId = row.RemoteScopeIdV6,
                RemotePort = unchecked((uint)row.RemotePortNetwork),
                State = row.TcpStateCode
            };

            TryEnableTcp6(ref mib);
            return ReadRodTcp6(ref mib, out stats);
        }

        private static void TryEnableTcp4(ref MibTcpRow row)
        {
            var rw = new TcpEStatsDataRwV0 { EnableCollection = true };
            uint rwSize = (uint)Marshal.SizeOf(typeof(TcpEStatsDataRwV0));
            SetPerTcpConnectionEStats(ref row, TcpEstatsData, ref rw, 0, rwSize, 0);
        }

        private static void TryEnableTcp6(ref MibTcp6Row row)
        {
            var rw = new TcpEStatsDataRwV0 { EnableCollection = true };
            uint rwSize = (uint)Marshal.SizeOf(typeof(TcpEStatsDataRwV0));
            SetPerTcp6ConnectionEStats(ref row, TcpEstatsData, ref rw, 0, rwSize, 0);
        }

        private static bool ReadRodTcp4(ref MibTcpRow row, out TcpStats stats)
        {
            stats = default(TcpStats);
            int rodSize = Marshal.SizeOf(typeof(TcpEStatsDataRodV0));
            IntPtr rodPtr = Marshal.AllocHGlobal(rodSize);
            try
            {
                uint result = GetPerTcpConnectionEStats(
                    ref row,
                    TcpEstatsData,
                    IntPtr.Zero,
                    0,
                    0,
                    IntPtr.Zero,
                    0,
                    0,
                    rodPtr,
                    0,
                    (uint)rodSize);

                if (result != 0)
                {
                    return false;
                }

                TcpEStatsDataRodV0 rod = (TcpEStatsDataRodV0)Marshal.PtrToStructure(rodPtr, typeof(TcpEStatsDataRodV0));
                stats.BytesOut = (long)rod.DataBytesOut;
                stats.BytesIn = (long)rod.DataBytesIn;
                stats.SegmentsOut = rod.DataSegsOut;
                stats.SegmentsIn = rod.DataSegsIn;
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(rodPtr);
            }
        }

        private static bool ReadRodTcp6(ref MibTcp6Row row, out TcpStats stats)
        {
            stats = default(TcpStats);
            int rodSize = Marshal.SizeOf(typeof(TcpEStatsDataRodV0));
            IntPtr rodPtr = Marshal.AllocHGlobal(rodSize);
            try
            {
                uint result = GetPerTcp6ConnectionEStats(
                    ref row,
                    TcpEstatsData,
                    IntPtr.Zero,
                    0,
                    0,
                    IntPtr.Zero,
                    0,
                    0,
                    rodPtr,
                    0,
                    (uint)rodSize);

                if (result != 0)
                {
                    return false;
                }

                TcpEStatsDataRodV0 rod = (TcpEStatsDataRodV0)Marshal.PtrToStructure(rodPtr, typeof(TcpEStatsDataRodV0));
                stats.BytesOut = (long)rod.DataBytesOut;
                stats.BytesIn = (long)rod.DataBytesIn;
                stats.SegmentsOut = rod.DataSegsOut;
                stats.SegmentsIn = rod.DataSegsIn;
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(rodPtr);
            }
        }

        private struct TcpStats
        {
            public long BytesOut;
            public long BytesIn;
            public long SegmentsOut;
            public long SegmentsIn;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct TcpEStatsDataRwV0
        {
            [MarshalAs(UnmanagedType.U1)]
            public bool EnableCollection;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TcpEStatsDataRodV0
        {
            public ulong DataBytesOut;
            public ulong DataBytesIn;
            public uint DataSegsOut;
            public uint DataSegsIn;
            public uint CurrentCongState;
            public uint CongestionSignals;
            public uint PktsOut;
            public uint PktsIn;
            public uint PktsReord;
            public uint PktsRetrans;
            public uint PktsFastRetrans;
            public uint SacksRcvd;
            public uint SndUna;
            public uint SndNxt;
            public uint SndMax;
            public ulong ThruBytesAcked;
            public ulong RttVar;
            public ulong MaxRtt;
            public ulong SmoothedRtt;
            public ulong RttVarSample;
            public ulong RcvDupAcks;
            public ulong PktsRcvdDelivered;
            public ulong PktsRcvdOutOfOrder;
            public ulong PktsRcvdDropped;
            public ulong SamplesRtt;
            public ulong SumRtt;
            public ulong DataBytesOutInorder;
            public ulong DataBytesInInorder;
            public ulong DataBytesInReordered;
            public ulong DataBytesInDuplicate;
            public ulong DataBytesOutRetrans;
            public ulong DataBytesInRetrans;
            public ulong DataBytesRetransNonWin;
            public ulong DataBytesRetransWin;
            public ulong PktsRetransNonWin;
            public ulong PktsRetransWin;
        }

        [DllImport("iphlpapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        private static extern uint SetPerTcpConnectionEStats(
            ref MibTcpRow row,
            int estatsType,
            ref TcpEStatsDataRwV0 rw,
            uint rwVersion,
            uint rwSize,
            uint offset);

        [DllImport("iphlpapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        private static extern uint SetPerTcp6ConnectionEStats(
            ref MibTcp6Row row,
            int estatsType,
            ref TcpEStatsDataRwV0 rw,
            uint rwVersion,
            uint rwSize,
            uint offset);

        [DllImport("iphlpapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        private static extern uint GetPerTcpConnectionEStats(
            ref MibTcpRow row,
            int estatsType,
            IntPtr rw,
            uint rwVersion,
            uint rwSize,
            IntPtr ros,
            uint rosVersion,
            uint rosSize,
            IntPtr rod,
            uint rodVersion,
            uint rodSize);

        [DllImport("iphlpapi.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        private static extern uint GetPerTcp6ConnectionEStats(
            ref MibTcp6Row row,
            int estatsType,
            IntPtr rw,
            uint rwVersion,
            uint rwSize,
            IntPtr ros,
            uint rosVersion,
            uint rosSize,
            IntPtr rod,
            uint rodVersion,
            uint rodSize);
    }
}
