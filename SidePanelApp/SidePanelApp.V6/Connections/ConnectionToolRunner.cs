using System;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    internal static class ConnectionToolRunner
    {
        public static async Task<string> PingAsync(ConnectionRow row)
        {
            string host = ResolveRemoteHost(row);
            if (host == null)
            {
                return "Нет удалённого адреса для ping.";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Ping " + host);
            sb.AppendLine(new string('-', 40));
            try
            {
                using (var ping = new Ping())
                {
                    for (int i = 0; i < 4; i++)
                    {
                        PingReply reply = await ping.SendPingAsync(host, 3000);
                        sb.AppendLine(string.Format(
                            "Ответ от {0}: время={1}мс TTL={2} статус={3}",
                            reply.Address,
                            reply.RoundtripTime,
                            reply.Options != null ? reply.Options.Ttl.ToString() : "?",
                            reply.Status));
                        if (reply.Status != IPStatus.Success)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Ошибка: " + ex.Message);
            }

            return sb.ToString();
        }

        public static Task<string> TracerouteAsync(ConnectionRow row)
        {
            return TracerouteAsync(row, null);
        }

        public static async Task<string> TracerouteAsync(ConnectionRow row, Action<string> onLine)
        {
            var sb = new StringBuilder();
            Action<string> write = line =>
            {
                if (string.IsNullOrEmpty(line))
                {
                    return;
                }

                sb.AppendLine(line);
                if (onLine != null)
                {
                    onLine(line);
                }
            };

            string host = ResolveRemoteHost(row);
            if (host == null)
            {
                write("Нет удалённого адреса для traceroute.");
                return sb.ToString();
            }

            const int maxHops = 20;
            const int timeoutMs = 2000;
            const int probes = 3;
            write("Трассировка маршрута к " + host + ", прыжков не больше " + maxHops);

            bool dontFragment = IPAddress.TryParse(host, out IPAddress parsed)
                && parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            byte[] buffer = new byte[32];

            try
            {
                for (int ttl = 1; ttl <= maxHops; ttl++)
                {
                    var times = new string[probes];
                    IPAddress hopAddress = null;
                    bool reached = false;
                    IPStatus failure = IPStatus.Unknown;

                    for (int probe = 0; probe < probes; probe++)
                    {
                        PingReply reply = await SendProbeAsync(host, timeoutMs, buffer, ttl, dontFragment);
                        times[probe] = FormatProbe(reply);
                        if (reply == null || reply.Status == IPStatus.TimedOut)
                        {
                            continue;
                        }

                        if (reply.Address != null)
                        {
                            hopAddress = reply.Address;
                        }

                        if (reply.Status == IPStatus.Success)
                        {
                            reached = true;
                        }
                        else if (reply.Status != IPStatus.TtlExpired
                            && reply.Status != IPStatus.TtlReassemblyTimeExceeded)
                        {
                            failure = reply.Status;
                        }
                    }

                    string target;
                    if (hopAddress != null && failure != IPStatus.Unknown)
                    {
                        target = hopAddress + "  " + DescribeStatus(failure);
                    }
                    else if (hopAddress != null)
                    {
                        target = hopAddress.ToString();
                    }
                    else if (failure != IPStatus.Unknown)
                    {
                        target = DescribeStatus(failure);
                    }
                    else
                    {
                        target = "Превышен интервал ожидания для запроса.";
                    }

                    write(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0,3}   {1,8}   {2,8}   {3,8}   {4}",
                        ttl,
                        times[0],
                        times[1],
                        times[2],
                        target));

                    if (reached || failure != IPStatus.Unknown)
                    {
                        break;
                    }
                }

                write("Трассировка завершена.");
            }
            catch (Exception ex)
            {
                write("Ошибка: " + ex.Message);
            }

            return sb.ToString();
        }

        public static string BuildPacketSummary(ConnectionRow row)
        {
            var sb = new StringBuilder();
            sb.AppendLine("TCP статистика (EStats / таблица)");
            sb.AppendLine(new string('-', 40));
            if (row.IsUdp)
            {
                sb.AppendLine("UDP: счётчики пакетов в ядре недоступны через этот API.");
                return sb.ToString();
            }

            if (row.State == "CLOSED" || row.IsRecentlyClosed)
            {
                sb.AppendLine("Соединение уже закрыто — счётчики недоступны.");
                sb.AppendLine("Выберите строку ESTABLISHED с тем же процессом.");
                return sb.ToString();
            }

            TcpConnectionStatsEnricher.EnrichSingle(row);
            sb.AppendLine("Sent packets:  " + Format(row.SentPackets));
            sb.AppendLine("Sent bytes:    " + Format(row.SentBytes));
            sb.AppendLine("Rcvd packets:  " + Format(row.RcvdPackets));
            sb.AppendLine("Rcvd bytes:    " + Format(row.RcvdBytes));
            if (!row.SentPackets.HasValue && !row.SentBytes.HasValue
                && !row.RcvdPackets.HasValue && !row.RcvdBytes.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine("EStats не получены (права, тип сокета или ядро не включило сбор).");
                sb.AppendLine("Попробуйте активное соединение; при отказе — запуск от администратора.");
            }

            sb.AppendLine();
            sb.AppendLine("Полный захват пакетов — Wireshark / pktmon.");
            sb.AppendLine("Здесь — счётчики соединения, как в TCPView (Sent/Rcvd).");
            sb.AppendLine("В логе мониторинга — прирост OUT (синий) / IN (красный) при обмене.");
            return sb.ToString();
        }

        private static string Format(long? value)
        {
            return value.HasValue ? value.Value.ToString() : "(нет данных)";
        }

        private static string ResolveRemoteHost(ConnectionRow row)
        {
            if (row == null || row.IsUdp)
            {
                return null;
            }

            string addr = row.RemoteAddress;
            if (string.IsNullOrEmpty(addr) || addr == "*")
            {
                return null;
            }

            if (addr.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            {
                return "127.0.0.1";
            }

            if (IPAddress.TryParse(addr, out IPAddress ip))
            {
                return ip.ToString();
            }

            return addr;
        }

        private static async Task<PingReply> SendProbeAsync(string host, int timeoutMs, byte[] buffer, int ttl, bool dontFragment)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var options = new PingOptions(ttl, dontFragment);
                    return await ping.SendPingAsync(host, timeoutMs, buffer, options);
                }
            }
            catch (PingException)
            {
                return null;
            }
        }

        private static string FormatProbe(PingReply reply)
        {
            if (reply == null || reply.Status == IPStatus.TimedOut || reply.Address == null)
            {
                return "*";
            }

            if (reply.RoundtripTime < 1)
            {
                return "<1 мс";
            }

            return reply.RoundtripTime.ToString(CultureInfo.InvariantCulture) + " мс";
        }

        private static string DescribeStatus(IPStatus status)
        {
            switch (status)
            {
                case IPStatus.DestinationHostUnreachable:
                    return "узел недоступен";
                case IPStatus.DestinationNetworkUnreachable:
                    return "сеть недоступна";
                case IPStatus.DestinationPortUnreachable:
                    return "порт недоступен";
                case IPStatus.DestinationUnreachable:
                    return "адресат недоступен";
                default:
                    return status.ToString();
            }
        }
    }
}
