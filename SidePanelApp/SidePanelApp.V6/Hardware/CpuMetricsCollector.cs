using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace SidePanelApp.V6.Hardware
{
    public sealed class CoreSnapshot
    {
        public int Index { get; set; }
        public double UsagePercent { get; set; }
        public double KernelPercent { get; set; }
        public double FrequencyMhz { get; set; }
    }

    public sealed class CpuSnapshot
    {
        public string Name { get; set; }
        public double TotalPercent { get; set; }
        public double KernelPercent { get; set; }
        public double CurrentFrequencyMhz { get; set; }
        public double MaxFrequencyMhz { get; set; }
        public CoreSnapshot[] Cores { get; set; }

        public CpuSnapshot()
        {
            Cores = new CoreSnapshot[0];
        }
    }

    /// <summary>
    /// Детальные метрики CPU: общая загрузка, по ядрам, время ядра (системное) и частота.
    /// </summary>
    internal sealed class CpuMetricsCollector : IDisposable
    {
        private readonly PerformanceCounter _totalCounter;
        private readonly PerformanceCounter _kernelCounter;
        private readonly List<CoreCounterSet> _cores = new List<CoreCounterSet>();
        private readonly Dictionary<int, PerformanceCounter> _frequencyCounters =
            new Dictionary<int, PerformanceCounter>();

        private readonly object _sync = new object();
        private string _processorName = "Процессор";
        private double _maxFrequencyMhz;
        private bool _frequencyAvailable;
        private bool _disposed;

        public CpuMetricsCollector()
        {
            _totalCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _kernelCounter = new PerformanceCounter("Processor", "% Privileged Time", "_Total");
            _totalCounter.NextValue();
            _kernelCounter.NextValue();

            ReadProcessorInfo();
            BuildCoreCounters();
            BuildFrequencyCounters();
        }

        public CpuSnapshot GetSnapshot()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return new CpuSnapshot();
                }

                return GetSnapshotCore();
            }
        }

        private CpuSnapshot GetSnapshotCore()
        {
            var snapshot = new CpuSnapshot
            {
                Name = _processorName,
                TotalPercent = ReadPercent(_totalCounter),
                KernelPercent = ReadPercent(_kernelCounter)
            };

            var cores = new List<CoreSnapshot>(_cores.Count);
            double freqSum = 0;
            int freqCount = 0;

            foreach (CoreCounterSet core in _cores)
            {
                double freq = ReadFrequency(core.Index);
                if (freq > 0)
                {
                    freqSum += freq;
                    freqCount++;
                }

                cores.Add(new CoreSnapshot
                {
                    Index = core.Index,
                    UsagePercent = ReadPercent(core.Usage),
                    KernelPercent = ReadPercent(core.Kernel),
                    FrequencyMhz = freq
                });
            }

            snapshot.Cores = cores.ToArray();
            snapshot.MaxFrequencyMhz = _maxFrequencyMhz;

            if (freqCount > 0)
            {
                snapshot.CurrentFrequencyMhz = freqSum / freqCount;
            }
            else if (_maxFrequencyMhz > 0)
            {
                snapshot.CurrentFrequencyMhz = _maxFrequencyMhz;
            }

            return snapshot;
        }

        private static double ReadPercent(PerformanceCounter counter)
        {
            try
            {
                return Math.Max(0, Math.Min(100, counter.NextValue()));
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        private double ReadFrequency(int coreIndex)
        {
            PerformanceCounter counter;
            if (_frequencyCounters.TryGetValue(coreIndex, out counter))
            {
                try
                {
                    return Math.Max(0, counter.NextValue());
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }

            return 0;
        }

        private void BuildCoreCounters()
        {
            _cores.Clear();
            string[] instances;
            try
            {
                instances = new PerformanceCounterCategory("Processor").GetInstanceNames();
            }
            catch (Exception)
            {
                return;
            }

            var coreIds = instances
                .Where(name => name != "_Total")
                .Select(name =>
                {
                    int index;
                    return int.TryParse(name, out index) ? index : -1;
                })
                .Where(index => index >= 0)
                .OrderBy(index => index)
                .ToArray();

            foreach (int index in coreIds)
            {
                string instance = index.ToString();
                try
                {
                    var usage = new PerformanceCounter("Processor", "% Processor Time", instance, true);
                    var kernel = new PerformanceCounter("Processor", "% Privileged Time", instance, true);
                    usage.NextValue();
                    kernel.NextValue();
                    _cores.Add(new CoreCounterSet
                    {
                        Index = index,
                        Usage = usage,
                        Kernel = kernel
                    });
                }
                catch (Exception)
                {
                }
            }
        }

        private void BuildFrequencyCounters()
        {
            foreach (PerformanceCounter counter in _frequencyCounters.Values)
            {
                counter.Dispose();
            }

            _frequencyCounters.Clear();
            _frequencyAvailable = CategoryExists("Processor Information");
            if (!_frequencyAvailable)
            {
                return;
            }

            string[] instances;
            try
            {
                instances = new PerformanceCounterCategory("Processor Information").GetInstanceNames();
            }
            catch (Exception)
            {
                _frequencyAvailable = false;
                return;
            }

            foreach (string instance in instances)
            {
                if (instance == "_Total" || instance.EndsWith(",_Total", StringComparison.Ordinal))
                {
                    continue;
                }

                int coreIndex = ParseCoreIndex(instance);
                if (coreIndex < 0 || _frequencyCounters.ContainsKey(coreIndex))
                {
                    continue;
                }

                try
                {
                    var counter = new PerformanceCounter("Processor Information", "Processor Frequency", instance, true);
                    counter.NextValue();
                    _frequencyCounters[coreIndex] = counter;
                }
                catch (Exception)
                {
                }
            }
        }

        private static int ParseCoreIndex(string instance)
        {
            int comma = instance.IndexOf(',');
            if (comma < 0)
            {
                return -1;
            }

            string corePart = instance.Substring(comma + 1);
            int index;
            return int.TryParse(corePart, out index) ? index : -1;
        }

        private void ReadProcessorInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Name, MaxClockSpeed, CurrentClockSpeed FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"] as string;
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            _processorName = name.Trim();
                        }

                        uint maxClock = ToUInt(obj["MaxClockSpeed"]);
                        uint currentClock = ToUInt(obj["CurrentClockSpeed"]);
                        if (maxClock > 0)
                        {
                            _maxFrequencyMhz = maxClock;
                        }
                        else if (currentClock > 0)
                        {
                            _maxFrequencyMhz = currentClock;
                        }

                        break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private static uint ToUInt(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToUInt32(value);
            }
            catch (Exception)
            {
                return 0;
            }
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
            lock (_sync)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _totalCounter.Dispose();
                _kernelCounter.Dispose();

                foreach (CoreCounterSet core in _cores)
                {
                    core.Dispose();
                }

                _cores.Clear();

                foreach (PerformanceCounter counter in _frequencyCounters.Values)
                {
                    counter.Dispose();
                }

                _frequencyCounters.Clear();
            }
        }

        private sealed class CoreCounterSet : IDisposable
        {
            public int Index;
            public PerformanceCounter Usage;
            public PerformanceCounter Kernel;

            public void Dispose()
            {
                if (Usage != null)
                {
                    Usage.Dispose();
                }

                if (Kernel != null)
                {
                    Kernel.Dispose();
                }
            }
        }
    }
}
