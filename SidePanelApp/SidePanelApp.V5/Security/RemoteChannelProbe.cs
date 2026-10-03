using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
namespace SidePanelApp.V5.Security
{
    internal sealed class RemoteChannelProbe
    {
        private static readonly int[] RemotePorts = { 3389, 5900, 5901, 5902, 7070 };

        public ThreatChannelSnapshot Probe(ISet<string> trustedAccounts)
        {
            var snapshot = new ThreatChannelSnapshot();
            var items = new List<ThreatItem>();

            CollectWtsSessions(items, trustedAccounts, snapshot);
            CollectInboundTcp(items);
            CollectRemoteTools(items);

            snapshot.Items = items;
            if (snapshot.Level == ThreatLevel.Ok && items.Count > 0)
            {
                snapshot.Level = ThreatLevel.Watch;
            }

            if (items.Count == 0)
            {
                snapshot.Note = "Активных удалённых сессий не видно";
            }

            return snapshot;
        }

        private static void CollectWtsSessions(List<ThreatItem> items, ISet<string> trustedAccounts, ThreatChannelSnapshot snapshot)
        {
            IntPtr server = WtsCurrentServerHandle;
            IntPtr sessionInfo;
            int count;
            if (!WTSEnumerateSessions(server, 0, 1, out sessionInfo, out count))
            {
                snapshot.Note = "WTS недоступен";
                return;
            }

            try
            {
                int dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                for (int i = 0; i < count; i++)
                {
                    IntPtr current = new IntPtr(sessionInfo.ToInt64() + i * dataSize);
                    WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure(current, typeof(WTS_SESSION_INFO));
                    if (si.State != WTS_CONNECTSTATE_CLASS.WTSActive &&
                        si.State != WTS_CONNECTSTATE_CLASS.WTSDisconnected)
                    {
                        continue;
                    }

                    if (!IsRemoteDesktopSession(si.SessionId, si.pWinStationName))
                    {
                        continue;
                    }

                    string user = QuerySessionString(si.SessionId, WTS_INFO_CLASS.WTSUserName);
                    if (string.IsNullOrWhiteSpace(user) || user.Equals("Services", StringComparison.OrdinalIgnoreCase) ||
                        user.Equals("DWM-1", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string domain = QuerySessionString(si.SessionId, WTS_INFO_CLASS.WTSDomainName);
                    string who = string.IsNullOrWhiteSpace(domain) ? user : domain + "\\" + user;
                    string from = QueryClientAddress(si.SessionId);
                    string state = si.State == WTS_CONNECTSTATE_CLASS.WTSActive ? "Active" : "Disconnected";
                    string tool = DescribeRemoteProtocol(si.SessionId, si.pWinStationName);

                    bool trusted = IsTrusted(who, user, trustedAccounts);
                    var item = new ThreatItem
                    {
                        TimeUtc = DateTime.UtcNow,
                        Who = who,
                        From = string.IsNullOrEmpty(from) ? "—" : from,
                        What = "Удалённая сессия " + state + " (id " + si.SessionId + ")",
                        Tool = tool,
                        Detail = si.pWinStationName
                    };
                    items.Add(item);

                    if (!trusted && si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                    {
                        snapshot.Level = ThreatLevel.Alert;
                    }
                    else if (!trusted && snapshot.Level < ThreatLevel.Watch)
                    {
                        snapshot.Level = ThreatLevel.Watch;
                    }
                }
            }
            finally
            {
                WTSFreeMemory(sessionInfo);
            }
        }

        private static void CollectInboundTcp(List<ThreatItem> items)
        {
            try
            {
                foreach (TcpConnectionInformation conn in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections())
                {
                    if (conn.State != TcpState.Established)
                    {
                        continue;
                    }

                    int localPort = conn.LocalEndPoint.Port;
                    if (!RemotePorts.Contains(localPort))
                    {
                        continue;
                    }

                    string remote = conn.RemoteEndPoint.Address.ToString();
                    if (IPAddress.IsLoopback(conn.RemoteEndPoint.Address))
                    {
                        continue;
                    }

                    string tool = PortToolName(localPort);
                    items.Add(new ThreatItem
                    {
                        TimeUtc = DateTime.UtcNow,
                        Who = "—",
                        From = remote + ":" + conn.RemoteEndPoint.Port,
                        What = "Входящее TCP на порт " + localPort,
                        Tool = tool,
                        Detail = conn.LocalEndPoint.ToString()
                    });
                }
            }
            catch (NetworkInformationException)
            {
            }
        }

        private static void CollectRemoteTools(List<ThreatItem> items)
        {
            string[] names = { "AnyDesk", "TeamViewer", "winvnc", "vncserver", "tv_w32" };
            try
            {
                foreach (Process process in Process.GetProcesses())
                {
                    string name;
                    try
                    {
                        name = process.ProcessName;
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (string marker in names)
                    {
                        if (name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            items.Add(new ThreatItem
                            {
                                TimeUtc = DateTime.UtcNow,
                                Who = "—",
                                From = "локально",
                                What = "Процесс удалённого доступа запущен",
                                Tool = marker,
                                Detail = "PID " + process.Id
                            });
                            break;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static string PortToolName(int port)
        {
            if (port == 3389)
            {
                return "RDP";
            }

            if (port >= 5900 && port <= 5910)
            {
                return "VNC";
            }

            if (port == 7070)
            {
                return "AnyDesk?";
            }

            return "TCP";
        }

        private static bool IsRemoteDesktopSession(int sessionId, string winStationName)
        {
            try
            {
                if (WTSIsSessionRemote(WtsCurrentServerHandle, sessionId))
                {
                    return true;
                }
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(winStationName) &&
                winStationName.StartsWith("Console", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(winStationName) &&
                (winStationName.StartsWith("RDP", StringComparison.OrdinalIgnoreCase) ||
                 winStationName.IndexOf("RDP-Tcp", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }

            return false;
        }

        private static string DescribeRemoteProtocol(int sessionId, string winStationName)
        {
            short protocol = QueryClientProtocolType(sessionId);
            if (protocol == 2)
            {
                return "RDP";
            }

            if (protocol == 1)
            {
                return "удалённая (legacy)";
            }

            if (!string.IsNullOrEmpty(winStationName) &&
                winStationName.IndexOf("RDP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "RDP";
            }

            return "удалённый доступ";
        }

        private static short QueryClientProtocolType(int sessionId)
        {
            IntPtr buffer;
            int bytes;
            if (!WTSQuerySessionInformation(WtsCurrentServerHandle, sessionId, WTS_INFO_CLASS.WTSClientProtocolType,
                    out buffer, out bytes) || buffer == IntPtr.Zero)
            {
                return -1;
            }

            try
            {
                return (short)Marshal.ReadInt16(buffer);
            }
            finally
            {
                WTSFreeMemory(buffer);
            }
        }

        private static bool IsTrusted(string who, string user, ISet<string> trustedAccounts)
        {
            if (trustedAccounts == null || trustedAccounts.Count == 0)
            {
                return false;
            }

            if (trustedAccounts.Contains(who) || trustedAccounts.Contains(user))
            {
                return true;
            }

            foreach (string trusted in trustedAccounts)
            {
                if (who.EndsWith("\\" + trusted, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string QuerySessionString(int sessionId, WTS_INFO_CLASS infoClass)
        {
            IntPtr buffer;
            int bytes;
            if (!WTSQuerySessionInformation(WtsCurrentServerHandle, sessionId, infoClass, out buffer, out bytes) ||
                buffer == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                return Marshal.PtrToStringAnsi(buffer) ?? string.Empty;
            }
            finally
            {
                WTSFreeMemory(buffer);
            }
        }

        private static string QueryClientAddress(int sessionId)
        {
            IntPtr buffer;
            int bytes;
            if (!WTSQuerySessionInformation(WtsCurrentServerHandle, sessionId, WTS_INFO_CLASS.WTSClientAddress,
                    out buffer, out bytes) || buffer == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                WTS_CLIENT_ADDRESS addr = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure(buffer, typeof(WTS_CLIENT_ADDRESS));
                if (addr.AddressFamily == 2 && addr.Address != null && addr.Address.Length >= 4)
                {
                    return addr.Address[2] + "." + addr.Address[3] + "." + addr.Address[4] + "." + addr.Address[5];
                }

                return string.Empty;
            }
            finally
            {
                WTSFreeMemory(buffer);
            }
        }

        private static readonly IntPtr WtsCurrentServerHandle = IntPtr.Zero;

        [DllImport("wtsapi32.dll")]
        private static extern bool WTSIsSessionRemote(IntPtr hServer, int sessionId);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSEnumerateSessions(
            IntPtr hServer,
            int Reserved,
            int Version,
            out IntPtr ppSessionInfo,
            out int pCount);

        [DllImport("wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("wtsapi32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            int sessionId,
            WTS_INFO_CLASS wtsInfoClass,
            out IntPtr ppBuffer,
            out int pBytesReturned);

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public int SessionId;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pWinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        private enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        private enum WTS_INFO_CLASS
        {
            WTSUserName = 5,
            WTSDomainName = 7,
            WTSClientAddress = 14,
            WTSClientProtocolType = 16
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_CLIENT_ADDRESS
        {
            public int AddressFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Address;
        }
    }
}
