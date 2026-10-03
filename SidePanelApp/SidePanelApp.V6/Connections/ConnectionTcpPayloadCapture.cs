using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    /// <summary>Захват TCP payload через Npcap — HEX и ASCII.</summary>
    internal sealed class ConnectionTcpPayloadCapture : IDisposable
    {
        private LibPcapLiveDevice _device;
        private ConnectionRow _row;
        private int _localPort;
        private int _remotePort;
        private string _localHost;
        private string _remoteHost;
        private Action<PacketListRow> _onPacket;
        private int _packetNumber;

        public bool IsRunning { get; private set; }

        public bool TryStart(ConnectionRow row, ref int initialPacketNumber, Action<PacketListRow> onPacket, Action<string> onStatus)
        {
            Stop();
            if (row == null || row.IsUdp || onPacket == null)
            {
                return false;
            }

            if (!TryParseEndpoint(row, out _localHost, out _localPort, out _remoteHost, out _remotePort))
            {
                onStatus?.Invoke("Не удалось разобрать адрес/порт для фильтра.");
                return false;
            }

            try
            {
                if (CaptureDeviceList.Instance.Count == 0)
                {
                    onStatus?.Invoke("Npcap не установлен. Установите Npcap (wireshark.org) и перезапустите приложение.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                onStatus?.Invoke("Npcap недоступен: " + ex.Message);
                return false;
            }

            _device = FindDevice(_localHost);
            if (_device == null)
            {
                onStatus?.Invoke("Не найден сетевой адаптер для " + _localHost + ".");
                return false;
            }

            _row = row;
            _onPacket = onPacket;
            _packetNumber = initialPacketNumber;

            try
            {
                _device.Open(DeviceModes.Promiscuous, 1000);
                _device.Filter = BuildFilter(row.IsTcp6, _localHost, _localPort, _remoteHost, _remotePort);
                _device.OnPacketArrival += Device_OnPacketArrival;
                _device.StartCapture();
                IsRunning = true;
                initialPacketNumber = _packetNumber;
            }
            catch (Exception ex)
            {
                onStatus?.Invoke("Ошибка захвата: " + ex.Message);
                SafeCloseDevice();
                return false;
            }

            try
            {
                onStatus?.Invoke("Захват Npcap: HEX + ASCII для payload TCP.");
            }
            catch
            {
            }

            return true;
        }

        public void Stop()
        {
            if (_device != null)
            {
                try
                {
                    _device.OnPacketArrival -= Device_OnPacketArrival;
                    if (_device.Started)
                    {
                        _device.StopCapture();
                    }
                }
                catch
                {
                }

                SafeCloseDevice();
            }

            IsRunning = false;
            _row = null;
            _onPacket = null;
            _device = null;
        }

        private void SafeCloseDevice()
        {
            try
            {
                _device?.Close();
            }
            catch
            {
            }
        }

        private void Device_OnPacketArrival(object sender, PacketCapture e)
        {
            if (_row == null || _onPacket == null)
            {
                return;
            }

            try
            {
                RawCapture raw = e.GetPacket();
                Packet packet = Packet.ParsePacket(raw.LinkLayerType, raw.Data);
                TcpPacket tcp = packet.Extract<TcpPacket>();
                if (tcp == null || tcp.PayloadData == null || tcp.PayloadData.Length == 0)
                {
                    return;
                }

                IPAddress srcIp;
                IPAddress dstIp;
                if (!TryGetIpEndpoints(packet, out srcIp, out dstIp))
                {
                    return;
                }

                if (!MatchesConnection(srcIp, tcp.SourcePort, dstIp, tcp.DestinationPort))
                {
                    return;
                }

                bool outbound = IsOutbound(srcIp, tcp.SourcePort);
                byte[] payload = new byte[tcp.PayloadData.Length];
                Buffer.BlockCopy(tcp.PayloadData, 0, payload, 0, payload.Length);
                _packetNumber++;
                PacketListRow listRow = PacketListRowFactory.CreateFromTcpPayload(
                    _row,
                    _packetNumber,
                    outbound,
                    payload,
                    tcp.SourcePort,
                    tcp.DestinationPort,
                    DescribeFlags(tcp));
                _onPacket(listRow);
            }
            catch
            {
            }
        }

        private bool MatchesConnection(IPAddress srcIp, int srcPort, IPAddress dstIp, int dstPort)
        {
            bool forward = PortEquals(srcPort, _localPort) && PortEquals(dstPort, _remotePort)
                && HostEquals(srcIp, _localHost) && HostEquals(dstIp, _remoteHost);
            bool reverse = PortEquals(srcPort, _remotePort) && PortEquals(dstPort, _localPort)
                && HostEquals(srcIp, _remoteHost) && HostEquals(dstIp, _localHost);
            return forward || reverse;
        }

        private bool IsOutbound(IPAddress srcIp, int srcPort)
        {
            return PortEquals(srcPort, _localPort) && HostEquals(srcIp, _localHost);
        }

        private static bool PortEquals(int a, int b)
        {
            return a == b;
        }

        private static bool HostEquals(IPAddress ip, string expectedHost)
        {
            if (ip == null || string.IsNullOrEmpty(expectedHost))
            {
                return false;
            }

            if (IPAddress.TryParse(expectedHost, out IPAddress expected))
            {
                return expected.Equals(ip);
            }

            return string.Equals(ip.ToString(), expectedHost, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetIpEndpoints(Packet packet, out IPAddress source, out IPAddress destination)
        {
            source = null;
            destination = null;
            IPv4Packet v4 = packet.Extract<IPv4Packet>();
            if (v4 != null)
            {
                source = v4.SourceAddress;
                destination = v4.DestinationAddress;
                return true;
            }

            IPv6Packet v6 = packet.Extract<IPv6Packet>();
            if (v6 != null)
            {
                source = v6.SourceAddress;
                destination = v6.DestinationAddress;
                return true;
            }

            return false;
        }

        private static bool TryParseEndpoint(
            ConnectionRow row,
            out string localHost,
            out int localPort,
            out string remoteHost,
            out int remotePort)
        {
            localHost = row.LocalAddress;
            remoteHost = row.RemoteAddress;
            localPort = 0;
            remotePort = 0;
            if (string.IsNullOrEmpty(localHost) || string.IsNullOrEmpty(remoteHost)
                || remoteHost == "*")
            {
                return false;
            }

            if (!int.TryParse(row.LocalPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out localPort))
            {
                return false;
            }

            if (!int.TryParse(row.RemotePort, NumberStyles.Integer, CultureInfo.InvariantCulture, out remotePort))
            {
                return false;
            }

            return localPort > 0 && remotePort > 0;
        }

        private static string BuildFilter(bool isTcp6, string localHost, int localPort, string remoteHost, int remotePort)
        {
            string proto = isTcp6 ? "tcp6" : "tcp";
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0} and ((src host {1} and src port {2} and dst host {3} and dst port {4}) or (src host {3} and src port {4} and dst host {1} and dst port {2})))",
                proto,
                localHost,
                localPort,
                remoteHost,
                remotePort);
        }

        private static LibPcapLiveDevice FindDevice(string localHost)
        {
            var devices = CaptureDeviceList.Instance.OfType<LibPcapLiveDevice>().ToList();
            if (devices.Count == 0)
            {
                return null;
            }

            if (IPAddress.TryParse(localHost, out IPAddress target))
            {
                foreach (LibPcapLiveDevice device in devices)
                {
                    if (device.Addresses == null)
                    {
                        continue;
                    }

                    foreach (PcapAddress address in device.Addresses)
                    {
                        if (address.Addr != null && target.Equals(address.Addr.ipAddress))
                        {
                            return device;
                        }
                    }
                }

                if (IPAddress.IsLoopback(target))
                {
                    LibPcapLiveDevice loopback = devices.FirstOrDefault(d =>
                        d.Description != null && d.Description.IndexOf("Loopback", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (loopback != null)
                    {
                        return loopback;
                    }
                }
            }

            return devices.FirstOrDefault(d => d.Addresses != null && d.Addresses.Count > 0) ?? devices[0];
        }

        private static string DescribeFlags(TcpPacket tcp)
        {
            if (tcp == null)
            {
                return "TCP";
            }

            var parts = new System.Collections.Generic.List<string>();
            ushort flags = tcp.Flags;
            if ((flags & 0x001) != 0)
            {
                parts.Add("FIN");
            }

            if ((flags & 0x002) != 0)
            {
                parts.Add("SYN");
            }

            if ((flags & 0x004) != 0)
            {
                parts.Add("RST");
            }

            if ((flags & 0x008) != 0)
            {
                parts.Add("PSH");
            }

            if ((flags & 0x010) != 0)
            {
                parts.Add("ACK");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "TCP";
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
