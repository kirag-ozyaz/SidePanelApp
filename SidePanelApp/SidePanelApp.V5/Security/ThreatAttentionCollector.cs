using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using SidePanelApp.V5.Properties;

namespace SidePanelApp.V5.Security
{
    internal sealed class ThreatAttentionCollector : IDisposable
    {
        private readonly RemoteChannelProbe _remote = new RemoteChannelProbe();
        private readonly SmbChannelProbe _smb = new SmbChannelProbe();
        private readonly ProbeChannelProbe _probe = new ProbeChannelProbe();
        private HashSet<string> _trustedAccounts;

        public ThreatAttentionCollector()
        {
            ReloadTrustedAccounts();
        }

        public void ReloadTrustedAccounts()
        {
            _trustedAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string configured = Settings.Default.TrustedAccounts;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                foreach (string part in configured.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    _trustedAccounts.Add(part.Trim());
                }
            }

            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                if (id != null && !string.IsNullOrEmpty(id.Name))
                {
                    _trustedAccounts.Add(id.Name);
                    int slash = id.Name.IndexOf('\\');
                    if (slash >= 0 && slash < id.Name.Length - 1)
                    {
                        _trustedAccounts.Add(id.Name.Substring(slash + 1));
                    }
                }
            }
            catch
            {
            }
        }

        public ThreatSnapshot GetSnapshot()
        {
            int threshold = Settings.Default.FailedLogonThreshold;
            if (threshold < 1)
            {
                threshold = 5;
            }

            int window = Settings.Default.FailedLogonWindowMinutes;
            if (window < 1)
            {
                window = 5;
            }

            var snapshot = new ThreatSnapshot
            {
                Remote = _remote.Probe(_trustedAccounts),
                Smb = _smb.Probe(_trustedAccounts),
                Probe = _probe.Probe(threshold, window)
            };

            return snapshot;
        }

        public void Dispose()
        {
        }
    }
}
