using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace SidePanelApp.V6.Security
{
    internal sealed class SmbChannelProbe
    {
        public ThreatChannelSnapshot Probe(ISet<string> trustedAccounts)
        {
            var snapshot = new ThreatChannelSnapshot();
            var items = new List<ThreatItem>();

            CollectInbound445(items);
            CollectNetSessions(items, trustedAccounts, snapshot);

            snapshot.Items = items;
            if (snapshot.Level == ThreatLevel.Ok && items.Count > 0)
            {
                snapshot.Level = ThreatLevel.Watch;
            }

            if (items.Count == 0)
            {
                snapshot.Note = "SMB-сессий к этому ПК не обнаружено";
            }

            return snapshot;
        }

        private static void CollectInbound445(List<ThreatItem> items)
        {
            try
            {
                foreach (TcpConnectionInformation conn in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections())
                {
                    if (conn.State != TcpState.Established || conn.LocalEndPoint.Port != 445)
                    {
                        continue;
                    }

                    if (IPAddress.IsLoopback(conn.RemoteEndPoint.Address))
                    {
                        continue;
                    }

                    items.Add(new ThreatItem
                    {
                        TimeUtc = DateTime.UtcNow,
                        Who = "—",
                        From = conn.RemoteEndPoint.Address + ":" + conn.RemoteEndPoint.Port,
                        What = "Установлено соединение SMB (445)",
                        Tool = "SMB",
                        Detail = conn.LocalEndPoint.ToString()
                    });
                }
            }
            catch (NetworkInformationException)
            {
            }
        }

        private static void CollectNetSessions(List<ThreatItem> items, ISet<string> trustedAccounts, ThreatChannelSnapshot snapshot)
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                if (identity == null || !IsAdministrator(identity))
                {
                    if (string.IsNullOrEmpty(snapshot.Note))
                    {
                        snapshot.Note = "Имена SMB-сессий: нужны права администратора";
                    }

                    return;
                }
            }
            catch
            {
                return;
            }

            IntPtr buffer;
            int entriesRead;
            int totalEntries;
            int resumeHandle = 0;
            int result = NetSessionEnum(null, null, null, 10, out buffer, -1, out entriesRead, out totalEntries, ref resumeHandle);
            if (result != 0 || entriesRead == 0)
            {
                return;
            }

            try
            {
                int size = Marshal.SizeOf(typeof(SESSION_INFO_10));
                for (int i = 0; i < entriesRead; i++)
                {
                    IntPtr ptr = new IntPtr(buffer.ToInt64() + i * size);
                    SESSION_INFO_10 info = (SESSION_INFO_10)Marshal.PtrToStructure(ptr, typeof(SESSION_INFO_10));
                    string who = info.username ?? "—";
                    string from = info.cname ?? "—";
                    bool trusted = trustedAccounts != null &&
                                   (trustedAccounts.Contains(who) || who.IndexOf('\\') >= 0 &&
                                    trustedAccounts.Contains(who.Substring(who.IndexOf('\\') + 1)));

                    items.Add(new ThreatItem
                    {
                        TimeUtc = DateTime.UtcNow,
                        Who = who,
                        From = from,
                        What = "SMB-сессия к этому компьютеру",
                        Tool = "SMB",
                        Detail = "Время " + info.time + " с"
                    });

                    if (!trusted)
                    {
                        snapshot.Level = ThreatLevel.Alert;
                    }
                }
            }
            finally
            {
                NetApiBufferFree(buffer);
            }
        }

        private static bool IsAdministrator(WindowsIdentity identity)
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetSessionEnum(
            string servername,
            string UncClientName,
            string username,
            int level,
            out IntPtr bufptr,
            int prefmaxlen,
            out int entriesread,
            out int totalentries,
            ref int resume_handle);

        [DllImport("netapi32.dll")]
        private static extern int NetApiBufferFree(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SESSION_INFO_10
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string cname;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string username;
            public int time;
            public int idle_time;
        }
    }
}
