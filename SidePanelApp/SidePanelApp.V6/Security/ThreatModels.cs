using System;
using System.Collections.Generic;

namespace SidePanelApp.V6.Security
{
    public enum ThreatLevel
    {
        Ok = 0,
        Watch = 1,
        Alert = 2
    }

    public sealed class ThreatItem
    {
        public DateTime TimeUtc { get; set; }
        public string Who { get; set; }
        public string From { get; set; }
        public string What { get; set; }
        public string Tool { get; set; }
        public string Detail { get; set; }
    }

    public sealed class ThreatChannelSnapshot
    {
        public ThreatLevel Level { get; set; }
        public string Note { get; set; }
        public List<ThreatItem> Items { get; set; }

        public ThreatChannelSnapshot()
        {
            Items = new List<ThreatItem>();
        }
    }

    public sealed class ThreatSnapshot
    {
        public ThreatChannelSnapshot Remote { get; set; }
        public ThreatChannelSnapshot Smb { get; set; }
        public ThreatChannelSnapshot Probe { get; set; }

        public ThreatSnapshot()
        {
            Remote = new ThreatChannelSnapshot();
            Smb = new ThreatChannelSnapshot();
            Probe = new ThreatChannelSnapshot();
        }

        public ThreatLevel OverallLevel
        {
            get
            {
                ThreatLevel level = ThreatLevel.Ok;
                level = Max(level, Remote.Level);
                level = Max(level, Smb.Level);
                level = Max(level, Probe.Level);
                return level;
            }
        }

        private static ThreatLevel Max(ThreatLevel a, ThreatLevel b)
        {
            return (ThreatLevel)Math.Max((int)a, (int)b);
        }
    }
}
