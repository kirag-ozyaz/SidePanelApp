using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SidePanelApp.V4.Hardware
{
    internal sealed class NicSnapshot
    {
        public string Name { get; set; }
        public double ReceivedBytesPerSec { get; set; }
        public double SentBytesPerSec { get; set; }
        public double BandwidthBitsPerSec { get; set; }

        public double TotalBytesPerSec
        {
            get { return ReceivedBytesPerSec + SentBytesPerSec; }
        }

        public double UtilizationPercent
        {
            get
            {
                if (BandwidthBitsPerSec <= 0)
                {
                    return 0;
                }

                return Math.Max(0, Math.Min(100, TotalBytesPerSec * 8d / BandwidthBitsPerSec * 100d));
            }
        }
    }

    /// <summary>
    /// Сбор метрик сетевых адаптеров: каждый физический/активный интерфейс отдельно.
    /// </summary>
    internal sealed class NetworkMetricsCollector : IDisposable
    {
        private const double BusyBytesPerSec = 10d * 1024d * 1024d;

        private readonly Dictionary<string, AdapterCounters> _counters =
            new Dictionary<string, AdapterCounters>(StringComparer.OrdinalIgnoreCase);

        private bool _available;
        private int _tick;
        private bool _disposed;

        public NetworkMetricsCollector()
        {
            _available = CategoryExists("Network Interface");
            RebuildCounters();
        }

        public NicSnapshot[] GetAdapters()
        {
            if (_tick++ % 10 == 0)
            {
                RebuildCounters();
            }

            var result = new List<NicSnapshot>();

            foreach (KeyValuePair<string, AdapterCounters> pair in _counters)
            {
                AdapterCounters counters = pair.Value;
                double received;
                double sent;
                double bandwidth;
                try
                {
                    received = Math.Max(0, counters.Received.NextValue());
                    sent = Math.Max(0, counters.Sent.NextValue());
                    bandwidth = Math.Max(0, counters.Bandwidth.NextValue());
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                result.Add(new NicSnapshot
                {
                    Name = pair.Key,
                    ReceivedBytesPerSec = received,
                    SentBytesPerSec = sent,
                    BandwidthBitsPerSec = bandwidth
                });
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return result.ToArray();
        }

        public static double ToRingPercent(double totalBytesPerSec)
        {
            return Math.Max(0, Math.Min(100, totalBytesPerSec / BusyBytesPerSec * 100d));
        }

        private void RebuildCounters()
        {
            if (!_available)
            {
                return;
            }

            string[] names;
            try
            {
                names = new PerformanceCounterCategory("Network Interface").GetInstanceNames();
            }
            catch (Exception)
            {
                _available = false;
                return;
            }

            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string name in names)
            {
                if (IsIgnoredAdapter(name))
                {
                    continue;
                }

                live.Add(name);
                if (_counters.ContainsKey(name))
                {
                    continue;
                }

                try
                {
                    var adapter = new AdapterCounters
                    {
                        Received = new PerformanceCounter("Network Interface", "Bytes Received/sec", name, true),
                        Sent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name, true),
                        Bandwidth = new PerformanceCounter("Network Interface", "Current Bandwidth", name, true)
                    };
                    adapter.Received.NextValue();
                    adapter.Sent.NextValue();
                    adapter.Bandwidth.NextValue();
                    _counters[name] = adapter;
                }
                catch (Exception)
                {
                }
            }

            var removed = new List<string>();
            foreach (string key in _counters.Keys)
            {
                if (!live.Contains(key))
                {
                    removed.Add(key);
                }
            }

            foreach (string key in removed)
            {
                _counters[key].Dispose();
                _counters.Remove(key);
            }
        }

        private static bool IsIgnoredAdapter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return true;
            }

            string n = name.ToLowerInvariant();
            return n.Contains("loopback") ||
                   n.Contains("isatap") ||
                   n.Contains("teredo") ||
                   n.Contains("wan miniport") ||
                   n.Contains("kernel debug") ||
                   n.Contains("pseudo-interface") ||
                   n.Contains("bluetooth") ||
                   n.Contains("virtualbox");
        }

        private static bool CategoryExists(string name)
        {
            try
            {
                return PerformanceCounterCategory.Exists(name);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (AdapterCounters counters in _counters.Values)
            {
                counters.Dispose();
            }

            _counters.Clear();
            _disposed = true;
        }

        private sealed class AdapterCounters : IDisposable
        {
            public PerformanceCounter Received;
            public PerformanceCounter Sent;
            public PerformanceCounter Bandwidth;

            public void Dispose()
            {
                if (Received != null)
                {
                    Received.Dispose();
                }

                if (Sent != null)
                {
                    Sent.Dispose();
                }

                if (Bandwidth != null)
                {
                    Bandwidth.Dispose();
                }
            }
        }
    }
}
