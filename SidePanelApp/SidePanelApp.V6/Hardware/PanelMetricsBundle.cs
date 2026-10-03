using SidePanelApp.V6.Security;

namespace SidePanelApp.V6.Hardware
{
    internal sealed class PanelMetricsBundle
    {
        public CpuSnapshot Cpu { get; set; }
        public MemorySnapshot Memory { get; set; }
        public GpuSnapshot[] Gpus { get; set; }
        public NicSnapshot[] Nics { get; set; }
        public ThreatSnapshot Threat { get; set; }
    }
}
