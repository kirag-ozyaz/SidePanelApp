using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;

namespace SidePanelApp.V5.Hardware
{
    internal sealed class GpuSnapshot
    {
        public string Name { get; set; }
        public int PhysIndex { get; set; }
        public double EnginePercent { get; set; }
        public ulong DedicatedUsed { get; set; }
        public ulong DedicatedTotal { get; set; }
        public ulong SharedUsed { get; set; }
        public ulong SharedTotal { get; set; }
        public ulong MemoryUsed { get; set; }
        public ulong MemoryTotal { get; set; }

        public double DedicatedPercent => UsagePercent(DedicatedUsed, DedicatedTotal);
        public double SharedPercent => UsagePercent(SharedUsed, SharedTotal);

        public double MemoryPercent => UsagePercent(MemoryUsed, MemoryTotal);

        private static double UsagePercent(ulong used, ulong total)
        {
            if (total == 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(100, used * 100d / total));
        }
    }

    /// <summary>
    /// Сбор метрик видеокарт: загрузка GPU и видеопамяти по каждому адаптеру.
    /// </summary>
    internal sealed class GpuMetricsCollector : IDisposable
    {
        private static readonly Regex EngineRegex = new Regex(
            @"phys_(\d+).*engtype_(\S+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MemoryRegex = new Regex(
            @"phys_(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly List<GpuIdentity> _identities = new List<GpuIdentity>();
        private readonly Dictionary<string, PerformanceCounter> _engineCounters =
            new Dictionary<string, PerformanceCounter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PerformanceCounter> _dedicatedCounters =
            new Dictionary<string, PerformanceCounter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PerformanceCounter> _sharedCounters =
            new Dictionary<string, PerformanceCounter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PerformanceCounter> _totalCommittedCounters =
            new Dictionary<string, PerformanceCounter>(StringComparer.OrdinalIgnoreCase);

        private readonly ulong _sharedMemoryBudgetBytes;
        private readonly object _sync = new object();
        private bool _engineAvailable;
        private bool _memoryAvailable;
        private int _tick;
        private bool _disposed;

        public GpuMetricsCollector()
        {
            _identities.AddRange(ReadGpuIdentities());
            _sharedMemoryBudgetBytes = ReadSharedMemoryBudgetBytes();
            _engineAvailable = CategoryExists("GPU Engine");
            _memoryAvailable = CategoryExists("GPU Adapter Memory");
            RebuildEngineCounters();
            RebuildMemoryCounters();
        }

        public GpuSnapshot[] GetGpus()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return new GpuSnapshot[0];
                }

                return GetGpusCore();
            }
        }

        private GpuSnapshot[] GetGpusCore()
        {
            if (_tick++ % 5 == 0)
            {
                RebuildEngineCounters();
                RebuildMemoryCounters();
            }

            Dictionary<int, double> engineByPhys = ReadEnginePercents();
            Dictionary<int, ulong> dedicatedByPhys = ReadMemoryUsageByPhys(_dedicatedCounters);
            Dictionary<int, ulong> sharedByPhys = ReadMemoryUsageByPhys(_sharedCounters);
            Dictionary<int, ulong> totalByPhys = ReadMemoryUsageByPhys(_totalCommittedCounters);

            int fromCounters = MaxKey(engineByPhys, dedicatedByPhys, sharedByPhys, totalByPhys) + 1;
            int deviceCount = _identities.Count > 0 ? _identities.Count : fromCounters;
            if (deviceCount <= 0)
            {
                return new GpuSnapshot[0];
            }

            var result = new List<GpuSnapshot>(deviceCount);
            for (int phys = 0; phys < deviceCount; phys++)
            {
                GpuIdentity identity = phys < _identities.Count ? _identities[phys] : null;
                string name = identity != null ? identity.Name : "Видеокарта " + (phys + 1);
                ulong dedicatedTotal = identity != null ? identity.MemoryTotal : 0;
                ulong dedicatedUsed = 0;
                dedicatedByPhys.TryGetValue(phys, out dedicatedUsed);
                if (dedicatedUsed > dedicatedTotal)
                {
                    dedicatedTotal = dedicatedUsed;
                }

                ulong sharedUsed = 0;
                sharedByPhys.TryGetValue(phys, out sharedUsed);
                ulong sharedTotal = _sharedMemoryBudgetBytes;
                if (sharedUsed > sharedTotal && sharedTotal > 0)
                {
                    sharedTotal = sharedUsed;
                }

                ulong totalUsed = 0;
                totalByPhys.TryGetValue(phys, out totalUsed);
                if (totalUsed == 0 && (dedicatedUsed > 0 || sharedUsed > 0))
                {
                    totalUsed = dedicatedUsed + sharedUsed;
                }

                ulong memoryTotal = 0;
                if (dedicatedTotal > 0 && sharedTotal > 0)
                {
                    memoryTotal = dedicatedTotal + sharedTotal;
                }
                else if (dedicatedTotal > 0)
                {
                    memoryTotal = dedicatedTotal;
                }
                else
                {
                    memoryTotal = sharedTotal;
                }

                if (totalUsed > memoryTotal && memoryTotal > 0)
                {
                    memoryTotal = totalUsed;
                }

                double engine = 0;
                engineByPhys.TryGetValue(phys, out engine);

                result.Add(new GpuSnapshot
                {
                    Name = name,
                    PhysIndex = phys,
                    EnginePercent = engine,
                    DedicatedUsed = dedicatedUsed,
                    DedicatedTotal = dedicatedTotal,
                    SharedUsed = sharedUsed,
                    SharedTotal = sharedTotal,
                    MemoryUsed = totalUsed,
                    MemoryTotal = memoryTotal
                });
            }

            return result.ToArray();
        }

        private Dictionary<int, double> ReadEnginePercents()
        {
            var byPhysAndType = new Dictionary<int, Dictionary<string, double>>();

            foreach (KeyValuePair<string, PerformanceCounter> pair in _engineCounters)
            {
                Match match = EngineRegex.Match(pair.Key);
                if (!match.Success)
                {
                    continue;
                }

                int phys = int.Parse(match.Groups[1].Value);
                string engType = match.Groups[2].Value;
                double value;
                try
                {
                    value = pair.Value.NextValue();
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                Dictionary<string, double> byType;
                if (!byPhysAndType.TryGetValue(phys, out byType))
                {
                    byType = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    byPhysAndType[phys] = byType;
                }

                double current;
                byType.TryGetValue(engType, out current);
                byType[engType] = current + value;
            }

            var result = new Dictionary<int, double>();
            foreach (KeyValuePair<int, Dictionary<string, double>> pair in byPhysAndType)
            {
                double max = 0;
                foreach (double value in pair.Value.Values)
                {
                    if (value > max)
                    {
                        max = value;
                    }
                }

                result[pair.Key] = Math.Max(0, Math.Min(100, max));
            }

            return result;
        }

        private Dictionary<int, ulong> ReadMemoryUsageByPhys(
            Dictionary<string, PerformanceCounter> counters)
        {
            var result = new Dictionary<int, ulong>();
            foreach (KeyValuePair<string, PerformanceCounter> pair in counters)
            {
                Match match = MemoryRegex.Match(pair.Key);
                if (!match.Success)
                {
                    continue;
                }

                int phys = int.Parse(match.Groups[1].Value);
                double value;
                try
                {
                    value = pair.Value.NextValue();
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                ulong usage = (ulong)Math.Max(0, value);
                ulong current;
                if (result.TryGetValue(phys, out current))
                {
                    usage = Math.Max(current, usage);
                }

                result[phys] = usage;
            }

            return result;
        }

        private void RebuildEngineCounters()
        {
            if (!_engineAvailable)
            {
                return;
            }

            string[] names;
            try
            {
                names = new PerformanceCounterCategory("GPU Engine").GetInstanceNames();
            }
            catch (Exception)
            {
                _engineAvailable = false;
                return;
            }

            SyncCounterMap(_engineCounters, "GPU Engine", "Utilization Percentage", names);
        }

        private void RebuildMemoryCounters()
        {
            if (!_memoryAvailable)
            {
                return;
            }

            string[] names;
            try
            {
                names = new PerformanceCounterCategory("GPU Adapter Memory").GetInstanceNames();
            }
            catch (Exception)
            {
                _memoryAvailable = false;
                return;
            }

            SyncCounterMap(_dedicatedCounters, "GPU Adapter Memory", "Dedicated Usage", names);
            SyncCounterMap(_sharedCounters, "GPU Adapter Memory", "Shared Usage", names);
            SyncCounterMap(_totalCommittedCounters, "GPU Adapter Memory", "Total Committed", names);
        }

        private static ulong ReadSharedMemoryBudgetBytes()
        {
            try
            {
                var info = new ComputerInfo();
                ulong total = info.TotalPhysicalMemory;
                return total > 0 ? total / 2 : 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static void SyncCounterMap(
            Dictionary<string, PerformanceCounter> map,
            string category,
            string counter,
            string[] names)
        {
            var live = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            var removed = new List<string>();
            foreach (string key in map.Keys)
            {
                if (!live.Contains(key))
                {
                    removed.Add(key);
                }
            }

            foreach (string key in removed)
            {
                map[key].Dispose();
                map.Remove(key);
            }

            foreach (string name in names)
            {
                if (map.ContainsKey(name))
                {
                    continue;
                }

                try
                {
                    var created = new PerformanceCounter(category, counter, name, true);
                    created.NextValue();
                    map[name] = created;
                }
                catch (Exception)
                {
                }
            }
        }

        private static List<GpuIdentity> ReadGpuIdentities()
        {
            var result = new List<GpuIdentity>();
            Dictionary<string, ulong> vramByName = ReadVramFromRegistry();

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Name, AdapterRAM FROM Win32_VideoController"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"] as string;
                        if (string.IsNullOrWhiteSpace(name) || IsIgnoredGpu(name))
                        {
                            continue;
                        }

                        ulong total = 0;
                        if (!vramByName.TryGetValue(name, out total))
                        {
                            object adapterRam = obj["AdapterRAM"];
                            if (adapterRam != null)
                            {
                                total = Convert.ToUInt64(adapterRam);
                            }
                        }

                        result.Add(new GpuIdentity { Name = name.Trim(), MemoryTotal = total });
                    }
                }
            }
            catch (Exception)
            {
                foreach (KeyValuePair<string, ulong> pair in vramByName)
                {
                    if (!IsIgnoredGpu(pair.Key))
                    {
                        result.Add(new GpuIdentity { Name = pair.Key, MemoryTotal = pair.Value });
                    }
                }
            }

            return result;
        }

        private static Dictionary<string, ulong> ReadVramFromRegistry()
        {
            var result = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
            const string root =
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(root))
                {
                    if (key == null)
                    {
                        return result;
                    }

                    foreach (string subName in key.GetSubKeyNames())
                    {
                        if (subName.Length != 4)
                        {
                            continue;
                        }

                        using (RegistryKey sub = key.OpenSubKey(subName))
                        {
                            if (sub == null)
                            {
                                continue;
                            }

                            string desc = sub.GetValue("DriverDesc") as string;
                            if (string.IsNullOrWhiteSpace(desc))
                            {
                                continue;
                            }

                            ulong bytes = ReadQword(sub.GetValue("HardwareInformation.qwMemorySize"));
                            if (bytes > 0)
                            {
                                result[desc.Trim()] = bytes;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        private static ulong ReadQword(object value)
        {
            if (value is long)
            {
                return (ulong)(long)value;
            }

            if (value is ulong)
            {
                return (ulong)value;
            }

            byte[] bytes = value as byte[];
            if (bytes != null && bytes.Length >= 8)
            {
                return BitConverter.ToUInt64(bytes, 0);
            }

            return 0;
        }

        private static bool IsIgnoredGpu(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("microsoft basic") ||
                   n.Contains("remote desktop") ||
                   n.Contains("virtualbox") ||
                   n.Contains("hyper-v video");
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

        private static int MaxKey(
            Dictionary<int, double> engines,
            Dictionary<int, ulong> dedicated,
            Dictionary<int, ulong> shared,
            Dictionary<int, ulong> total)
        {
            int max = -1;
            foreach (int key in engines.Keys)
            {
                if (key > max)
                {
                    max = key;
                }
            }

            foreach (int key in dedicated.Keys)
            {
                if (key > max)
                {
                    max = key;
                }
            }

            foreach (int key in shared.Keys)
            {
                if (key > max)
                {
                    max = key;
                }
            }

            foreach (int key in total.Keys)
            {
                if (key > max)
                {
                    max = key;
                }
            }

            return max;
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

                foreach (PerformanceCounter counter in _engineCounters.Values)
                {
                    counter.Dispose();
                }

                DisposeCounterMap(_dedicatedCounters);
                DisposeCounterMap(_sharedCounters);
                DisposeCounterMap(_totalCommittedCounters);

                _engineCounters.Clear();
            }
        }

        private static void DisposeCounterMap(Dictionary<string, PerformanceCounter> map)
        {
            foreach (PerformanceCounter counter in map.Values)
            {
                counter.Dispose();
            }

            map.Clear();
        }

        private sealed class GpuIdentity
        {
            public string Name;
            public ulong MemoryTotal;
        }
    }
}
