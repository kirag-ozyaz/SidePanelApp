using System;
using System.Threading;
using System.Threading.Tasks;
using SidePanelApp.V6.Properties;

namespace SidePanelApp.V6.Security
{
    /// <summary>
    /// Сбор снимка угроз в фоновом потоке; три канала опрашиваются параллельно.
    /// </summary>
    public sealed class ThreatBackgroundService : IDisposable
    {
        private readonly ThreatAttentionCollector _collector = new ThreatAttentionCollector();
        private readonly object _sync = new object();
        private ThreatSnapshot _latest = new ThreatSnapshot();
        private CancellationTokenSource _cts;
        private Task _worker;
        private bool _disposed;

        public void Start()
        {
            if (_worker != null)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _worker = Task.Run(() => RunLoop(_cts.Token));
        }

        public ThreatSnapshot GetLatestSnapshot()
        {
            lock (_sync)
            {
                return CloneSnapshot(_latest);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_cts != null)
            {
                _cts.Cancel();
            }

            try
            {
                _worker?.Wait(TimeSpan.FromSeconds(8));
            }
            catch (AggregateException)
            {
            }

            _cts?.Dispose();
            _collector.Dispose();
        }

        private async Task RunLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    ThreatSnapshot snapshot = await _collector.CollectSnapshotParallelAsync(token);
                    lock (_sync)
                    {
                        _latest = snapshot;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                }

                int delayMs = Settings.Default.ThreatPollIntervalMs;
                if (delayMs < 500)
                {
                    delayMs = 3000;
                }

                try
                {
                    await Task.Delay(delayMs, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static ThreatSnapshot CloneSnapshot(ThreatSnapshot source)
        {
            if (source == null)
            {
                return new ThreatSnapshot();
            }

            return new ThreatSnapshot
            {
                Remote = CloneChannel(source.Remote),
                Smb = CloneChannel(source.Smb),
                Probe = CloneChannel(source.Probe)
            };
        }

        private static ThreatChannelSnapshot CloneChannel(ThreatChannelSnapshot channel)
        {
            if (channel == null)
            {
                return new ThreatChannelSnapshot();
            }

            var copy = new ThreatChannelSnapshot
            {
                Level = channel.Level,
                Note = channel.Note
            };
            if (channel.Items != null)
            {
                copy.Items.AddRange(channel.Items);
            }

            return copy;
        }
    }
}
