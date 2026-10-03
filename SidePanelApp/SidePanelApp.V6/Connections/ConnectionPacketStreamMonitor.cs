using System;
using System.Windows.Forms;
using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    /// <summary>Npcap payload (HEX/ASCII) или резерв EStats без дампа.</summary>
    internal sealed class ConnectionPacketStreamMonitor : IDisposable
    {
        private readonly Timer _estatsTimer = new Timer();
        private ConnectionTcpPayloadCapture _capture;
        private ConnectionRow _row;
        private string _connectionKey;
        private string _processKey;
        private Action<PacketListRow> _onPacket;
        private int _packetNumber;
        private bool _useEstatsFallback;
        private long? _lastSentPackets;
        private long? _lastSentBytes;
        private long? _lastRcvdPackets;
        private long? _lastRcvdBytes;

        public bool IsRunning { get; private set; }

        public string ConnectionKey
        {
            get { return _connectionKey; }
        }

        public string ProcessKey
        {
            get { return _processKey; }
        }

        public ConnectionPacketStreamMonitor()
        {
            _estatsTimer.Interval = 400;
            _estatsTimer.Tick += EstatsTimer_Tick;
        }

        public void Start(ConnectionRow row, Action<PacketListRow> onPacket, int initialPacketNumber)
        {
            Stop();
            if (row == null || row.IsUdp || onPacket == null)
            {
                return;
            }

            _row = row;
            _connectionKey = ConnectionRowKey.Get(row);
            _processKey = ConnectionRowKey.GetProcessKey(row);
            _onPacket = onPacket;
            _packetNumber = Math.Max(0, initialPacketNumber);

            _capture = new ConnectionTcpPayloadCapture();
            if (_capture.TryStart(row, ref _packetNumber, DeliverPacket, EmitSystem))
            {
                IsRunning = true;
                return;
            }

            _capture.Dispose();
            _capture = null;
            StartEstatsFallback();
        }

        private void StartEstatsFallback()
        {
            _useEstatsFallback = true;
            _lastSentPackets = null;
            _lastSentBytes = null;
            _lastRcvdPackets = null;
            _lastRcvdBytes = null;
            EmitSystem("Npcap недоступен — только счётчики (без HEX). Установите Npcap или запустите от администратора.");
            IsRunning = true;
            _estatsTimer.Start();
            EstatsTimer_Tick(null, EventArgs.Empty);
        }

        public void Stop()
        {
            _estatsTimer.Stop();
            _useEstatsFallback = false;
            if (_capture != null)
            {
                if (IsRunning)
                {
                    EmitSystem("— мониторинг остановлен —");
                }

                _capture.Stop();
                _capture.Dispose();
                _capture = null;
            }
            else if (IsRunning && _row != null)
            {
                EmitSystem("— мониторинг остановлен —");
            }

            IsRunning = false;
            _row = null;
            _connectionKey = null;
            _processKey = null;
            _onPacket = null;
        }

        private void EstatsTimer_Tick(object sender, EventArgs e)
        {
            if (!_useEstatsFallback || _row == null)
            {
                return;
            }

            TcpConnectionStatsEnricher.EnrichSingle(_row);
            EmitDelta(true, _lastSentPackets, _row.SentPackets, _lastSentBytes, _row.SentBytes);
            EmitDelta(false, _lastRcvdPackets, _row.RcvdPackets, _lastRcvdBytes, _row.RcvdBytes);

            _lastSentPackets = _row.SentPackets;
            _lastSentBytes = _row.SentBytes;
            _lastRcvdPackets = _row.RcvdPackets;
            _lastRcvdBytes = _row.RcvdBytes;
        }

        private void DeliverPacket(PacketListRow row)
        {
            _onPacket?.Invoke(row);
        }

        private void EmitSystem(string message)
        {
            _packetNumber++;
            PacketListRow row = PacketListRowFactory.CreateSystem(_row, _packetNumber, message);
            DeliverPacket(row);
        }

        private void EmitDelta(
            bool outbound,
            long? prevPackets,
            long? curPackets,
            long? prevBytes,
            long? curBytes)
        {
            if (!curPackets.HasValue && !curBytes.HasValue)
            {
                return;
            }

            long dPkts = 0;
            long dBytes = 0;
            if (prevPackets.HasValue && curPackets.HasValue)
            {
                dPkts = curPackets.Value - prevPackets.Value;
            }

            if (prevBytes.HasValue && curBytes.HasValue)
            {
                dBytes = curBytes.Value - prevBytes.Value;
            }

            if (!prevPackets.HasValue && !prevBytes.HasValue)
            {
                return;
            }

            if (dPkts <= 0 && dBytes <= 0)
            {
                return;
            }

            _packetNumber++;
            PacketListRow row = PacketListRowFactory.CreateTcpDelta(
                _row,
                _packetNumber,
                outbound,
                Math.Max(0, dPkts),
                Math.Max(0, dBytes));
            DeliverPacket(row);
        }

        public void Dispose()
        {
            Stop();
            _estatsTimer.Dispose();
        }
    }
}
