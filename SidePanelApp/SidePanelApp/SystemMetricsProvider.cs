using System;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;

namespace SidePanelApp
{
    /// <summary>Сборщик метрик ОС: загрузка CPU и использование RAM.</summary>
    public sealed class SystemMetricsProvider : IDisposable
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly ComputerInfo _computerInfo;
        private bool _disposed;

        public SystemMetricsProvider()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _computerInfo = new ComputerInfo();
            _cpuCounter.NextValue();
        }

        public double GetCpuUsagePercent()
        {
            return Math.Max(0, Math.Min(100, _cpuCounter.NextValue()));
        }

        public double GetRamUsagePercent()
        {
            ulong total = _computerInfo.TotalPhysicalMemory;
            if (total == 0)
            {
                return 0;
            }

            ulong available = _computerInfo.AvailablePhysicalMemory;
            double used = total - available;
            return Math.Max(0, Math.Min(100, used * 100.0 / total));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cpuCounter.Dispose();
            _disposed = true;
        }
    }
}
