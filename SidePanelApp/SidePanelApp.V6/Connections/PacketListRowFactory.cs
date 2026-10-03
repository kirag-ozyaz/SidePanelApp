using System;
using System.Text;
using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    internal static class PacketListRowFactory
    {
        public static PacketListRow CreateFromTcpPayload(
            ConnectionRow connection,
            int number,
            bool outbound,
            byte[] payload,
            int sourcePort,
            int destinationPort,
            string flagsSummary)
        {
            string local = FormatEndpoint(connection.LocalAddress, connection.LocalPort);
            string remote = FormatEndpoint(connection.RemoteAddress, connection.RemotePort);
            string source = outbound ? local : remote;
            string destination = outbound ? remote : local;
            int length = payload != null ? payload.Length : 0;
            string info = string.Format(
                "{0} → {1} [{2}] Len={3}",
                sourcePort,
                destinationPort,
                flagsSummary ?? "TCP",
                length);

            var row = new PacketListRow
            {
                Number = number,
                Time = DateTime.Now,
                Source = source,
                Destination = destination,
                Protocol = "TCP",
                Length = length,
                Info = info,
                IsOutbound = outbound,
                Payload = payload,
                HexDump = HexDumpFormatter.Format(payload)
            };

            row.Details.Add(new PacketDetailNode
            {
                Text = "Frame: TCP payload (Npcap)",
                Children =
                {
                    new PacketDetailNode { Text = "Payload size: " + length + " bytes" },
                    new PacketDetailNode { Text = "ASCII (printable): " + HexDumpFormatter.ToPrintableAscii(payload, length) }
                }
            });

            row.Details.Add(new PacketDetailNode
            {
                Text = "Transmission Control Protocol",
                Children =
                {
                    new PacketDetailNode { Text = "Source Port: " + sourcePort },
                    new PacketDetailNode { Text = "Destination Port: " + destinationPort },
                    new PacketDetailNode { Text = "Flags: " + (flagsSummary ?? "—") },
                    new PacketDetailNode { Text = "Direction: " + (outbound ? "outbound" : "inbound") }
                }
            });

            return row;
        }

        public static PacketListRow CreateSystem(ConnectionRow connection, int number, string message)
        {
            string local = FormatEndpoint(connection.LocalAddress, connection.LocalPort);
            string remote = FormatEndpoint(connection.RemoteAddress, connection.RemotePort);
            return new PacketListRow
            {
                Number = number,
                Time = DateTime.Now,
                Source = local,
                Destination = remote,
                Protocol = "—",
                Length = 0,
                Info = message,
                IsSystem = true,
                HexDump = "Системное сообщение (не кадр Ethernet).\r\n"
            };
        }

        public static PacketListRow CreateTcpDelta(
            ConnectionRow connection,
            int number,
            bool outbound,
            long deltaSegments,
            long deltaBytes)
        {
            string local = FormatEndpoint(connection.LocalAddress, connection.LocalPort);
            string remote = FormatEndpoint(connection.RemoteAddress, connection.RemotePort);
            string source = outbound ? local : remote;
            string dest = outbound ? remote : local;
            string srcPort = outbound ? connection.LocalPort : connection.RemotePort;
            string dstPort = outbound ? connection.RemotePort : connection.LocalPort;
            int length = (int)Math.Min(int.MaxValue, Math.Max(0, deltaBytes));

            string flags = outbound ? "PSH, ACK" : "ACK";
            string info = string.Format(
                "{0} → {1} [{2}] Seq=… Ack=… Win=… Len={3}",
                srcPort,
                dstPort,
                flags,
                length);

            var row = new PacketListRow
            {
                Number = number,
                Time = DateTime.Now,
                Source = source,
                Destination = dest,
                Protocol = "TCP",
                Length = length,
                Info = info,
                IsOutbound = outbound,
                HexDump = BuildSyntheticHexDump(outbound, deltaSegments, deltaBytes)
            };

            row.Details.Add(new PacketDetailNode
            {
                Text = "Frame: EStats aggregate (не полный захват с адаптера)",
                Children =
                {
                    new PacketDetailNode { Text = "Comment: счётчики TCP EStats, как TCPView Sent/Rcvd" }
                }
            });

            var ipNode = new PacketDetailNode
            {
                Text = "Internet Protocol Version 4",
                Children =
                {
                    new PacketDetailNode { Text = "Source: " + source },
                    new PacketDetailNode { Text = "Destination: " + dest }
                }
            };
            row.Details.Add(ipNode);

            var tcpNode = new PacketDetailNode
            {
                Text = "Transmission Control Protocol",
                Children =
                {
                    new PacketDetailNode { Text = "Source Port: " + srcPort },
                    new PacketDetailNode { Text = "Destination Port: " + dstPort },
                    new PacketDetailNode { Text = "Direction: " + (outbound ? "outbound (→)" : "inbound (←)") },
                    new PacketDetailNode { Text = "Segments in interval: " + deltaSegments },
                    new PacketDetailNode { Text = "Payload bytes (approx): " + deltaBytes }
                }
            };
            row.Details.Add(tcpNode);

            return row;
        }

        private static string FormatEndpoint(string address, string port)
        {
            if (string.IsNullOrEmpty(address))
            {
                return "?";
            }

            if (string.IsNullOrEmpty(port) || port == "*")
            {
                return address;
            }

            return address + ":" + port;
        }

        private static string BuildSyntheticHexDump(bool outbound, long segments, long bytes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("0000  EStats segment summary (payload not captured)");
            sb.AppendLine("      Direction: " + (outbound ? "OUT" : "IN"));
            sb.AppendLine("      Segments:  " + segments);
            sb.AppendLine("      Bytes:     " + bytes);
            sb.AppendLine();
            sb.AppendLine("Полный дамп кадров — Wireshark + Npcap.");
            return sb.ToString();
        }
    }
}
