using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using SidePanelApp.V6.Properties;

namespace SidePanelApp.V6.Security
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

        public async Task<ThreatSnapshot> CollectSnapshotParallelAsync(CancellationToken token)
        {
            ReloadTrustedAccounts();
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

            ISet<string> trusted = _trustedAccounts;
            Task<ThreatChannelSnapshot> remoteTask = Task.Run(
                () => _remote.Probe(trusted),
                token);
            Task<ThreatChannelSnapshot> smbTask = Task.Run(
                () => _smb.Probe(trusted),
                token);
            Task<ThreatChannelSnapshot> probeTask = Task.Run(
                () => _probe.Probe(threshold, window),
                token);

            await Task.WhenAll(remoteTask, smbTask, probeTask);

            return new ThreatSnapshot
            {
                Remote = await remoteTask,
                Smb = await smbTask,
                Probe = await probeTask
            };
        }

        public void Dispose()
        {
        }
    }
}
