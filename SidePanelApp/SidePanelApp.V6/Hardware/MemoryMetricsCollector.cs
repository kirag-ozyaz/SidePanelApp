using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using Microsoft.VisualBasic.Devices;

namespace SidePanelApp.V6.Hardware
{
    public sealed class MemoryModuleSnapshot
    {
        public string Slot { get; set; }
        public ulong CapacityBytes { get; set; }
        public uint SpeedMhz { get; set; }
        public string TypeName { get; set; }
    }

    public sealed class MemorySnapshot
    {
        public double UsagePercent { get; set; }
        public ulong TotalBytes { get; set; }
        public ulong UsedBytes { get; set; }
        public ulong AvailableBytes { get; set; }
        public ulong CommittedBytes { get; set; }
        public ulong CommitLimitBytes { get; set; }
        public MemoryModuleSnapshot[] Modules { get; set; }

        public MemorySnapshot()
        {
            Modules = new MemoryModuleSnapshot[0];
        }
    }

    /// <summary>
    /// Метрики основной (физической) оперативной памяти.
    /// </summary>
    internal sealed class MemoryMetricsCollector : IDisposable
    {
        private readonly ComputerInfo _computerInfo;
        private readonly PerformanceCounter _committedCounter;
        private readonly PerformanceCounter _commitLimitCounter;
        private readonly List<MemoryModuleSnapshot> _modules = new List<MemoryModuleSnapshot>();
        private readonly object _sync = new object();
        private bool _commitAvailable;
        private bool _disposed;

        public MemoryMetricsCollector()
        {
            _computerInfo = new ComputerInfo();
            _commitAvailable = CategoryExists("Memory");

            if (_commitAvailable)
            {
                _committedCounter = new PerformanceCounter("Memory", "Committed Bytes");
                _commitLimitCounter = new PerformanceCounter("Memory", "Commit Limit");
                _committedCounter.NextValue();
                _commitLimitCounter.NextValue();
            }

            ReadMemoryModules();
        }

        public MemorySnapshot GetSnapshot()
        {
            lock (_sync)
            {
                if (_disposed)
                {
                    return new MemorySnapshot();
                }

                return GetSnapshotCore();
            }
        }

        private MemorySnapshot GetSnapshotCore()
        {
            ulong total = _computerInfo.TotalPhysicalMemory;
            ulong available = _computerInfo.AvailablePhysicalMemory;
            ulong used = total > available ? total - available : 0;

            var snapshot = new MemorySnapshot
            {
                TotalBytes = total,
                AvailableBytes = available,
                UsedBytes = used,
                UsagePercent = total == 0 ? 0 : Math.Max(0, Math.Min(100, used * 100d / total)),
                Modules = _modules.ToArray()
            };

            if (_commitAvailable)
            {
                try
                {
                    snapshot.CommittedBytes = (ulong)Math.Max(0, _committedCounter.NextValue());
                    snapshot.CommitLimitBytes = (ulong)Math.Max(0, _commitLimitCounter.NextValue());
                }
                catch (InvalidOperationException)
                {
                }
            }

            return snapshot;
        }

        private void ReadMemoryModules()
        {
            _modules.Clear();
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT BankLabel, Capacity, Speed, SMBIOSMemoryType FROM Win32_PhysicalMemory"))
                {
                    int index = 0;
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ulong capacity = ToULong(obj["Capacity"]);
                        if (capacity == 0)
                        {
                            continue;
                        }

                        string slot = obj["BankLabel"] as string;
                        if (string.IsNullOrWhiteSpace(slot))
                        {
                            slot = "Планка " + (index + 1);
                        }

                        _modules.Add(new MemoryModuleSnapshot
                        {
                            Slot = slot.Trim(),
                            CapacityBytes = capacity,
                            SpeedMhz = ToUInt(obj["Speed"]),
                            TypeName = MapMemoryType(ToUInt(obj["SMBIOSMemoryType"]))
                        });
                        index++;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private static string MapMemoryType(uint type)
        {
            switch (type)
            {
                case 24: return "DDR3";
                case 26: return "DDR4";
                case 34: return "DDR5";
                case 0: return "";
                default: return type > 0 ? "DDR" : "";
            }
        }

        private static ulong ToULong(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToUInt64(value);
            }
            catch (Exception)
            {
                return 0;
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

                if (_committedCounter != null)
                {
                    _committedCounter.Dispose();
                }

                if (_commitLimitCounter != null)
                {
                    _commitLimitCounter.Dispose();
                }
            }
        }
    }
}
