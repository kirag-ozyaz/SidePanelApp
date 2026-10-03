using System;
using System.Collections.Generic;

namespace SidePanelApp.V6.Connections
{
    internal sealed class PacketListRow
    {
        public int Number { get; set; }
        public DateTime Time { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public string Protocol { get; set; }
        public int Length { get; set; }
        public string Info { get; set; }
        public bool IsOutbound { get; set; }
        public bool IsSystem { get; set; }
        public List<PacketDetailNode> Details { get; set; } = new List<PacketDetailNode>();
        public string HexDump { get; set; }
        public byte[] Payload { get; set; }
    }

    internal sealed class PacketDetailNode
    {
        public string Text { get; set; }
        public List<PacketDetailNode> Children { get; set; } = new List<PacketDetailNode>();
    }
}
