using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SidePanelApp.V6.Security
{
    internal sealed class ProbeChannelProbe
    {
        private readonly List<ThreatItem> _recentFailures = new List<ThreatItem>();
        private readonly object _sync = new object();
        private bool _securityLogDenied;
        private DateTime _lastLogScanUtc = DateTime.MinValue;

        public ThreatChannelSnapshot Probe(int failedThreshold, int windowMinutes)
        {
            ScanSecurityLogIfDue();

            var snapshot = new ThreatChannelSnapshot();
            List<ThreatItem> inWindow;
            lock (_sync)
            {
                DateTime cutoff = DateTime.UtcNow.AddMinutes(-windowMinutes);
                inWindow = _recentFailures
                    .Where(f => f.TimeUtc >= cutoff)
                    .OrderByDescending(f => f.TimeUtc)
                    .Take(20)
                    .ToList();
            }

            snapshot.Items = inWindow;

            if (_securityLogDenied)
            {
                snapshot.Note = "Журнал Security недоступен (4625/4624)";
            }

            var byIp = inWindow
                .Where(f => !string.IsNullOrEmpty(f.From) && f.From != "—")
                .GroupBy(f => f.From)
                .Select(g => new { Ip = g.Key, Count = g.Count() })
                .ToList();

            foreach (var group in byIp)
            {
                if (group.Count >= failedThreshold)
                {
                    snapshot.Level = ThreatLevel.Alert;
                    break;
                }
            }

            if (snapshot.Level < ThreatLevel.Alert && inWindow.Count > 0)
            {
                snapshot.Level = ThreatLevel.Watch;
            }

            if (snapshot.Items.Count == 0 && string.IsNullOrEmpty(snapshot.Note))
            {
                snapshot.Note = "Недавних неудачных входов (4625) нет";
            }

            return snapshot;
        }

        private void ScanSecurityLogIfDue()
        {
            if ((DateTime.UtcNow - _lastLogScanUtc).TotalSeconds < 15)
            {
                return;
            }

            _lastLogScanUtc = DateTime.UtcNow;

            try
            {
                using (EventLog log = new EventLog("Security"))
                {
                    int start = Math.Max(0, log.Entries.Count - 80);
                    lock (_sync)
                    {
                        for (int i = log.Entries.Count - 1; i >= start; i--)
                        {
                            EventLogEntry entry = log.Entries[i];
                            if (entry.InstanceId != 4625 && entry.InstanceId != 4624)
                            {
                                continue;
                            }

                            if (entry.InstanceId == 4624 && !IsRemoteLogon(entry.Message))
                            {
                                continue;
                            }

                            ThreatItem item = ParseLogonEvent(entry);
                            if (item == null)
                            {
                                continue;
                            }

                            if (!_recentFailures.Any(f => f.TimeUtc == item.TimeUtc && f.Detail == item.Detail))
                            {
                                _recentFailures.Add(item);
                            }
                        }

                        if (_recentFailures.Count > 200)
                        {
                            _recentFailures.RemoveRange(0, _recentFailures.Count - 200);
                        }
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                _securityLogDenied = true;
            }
            catch (InvalidOperationException)
            {
                _securityLogDenied = true;
            }
            catch (UnauthorizedAccessException)
            {
                _securityLogDenied = true;
            }
        }

        private static bool IsRemoteLogon(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            return message.Contains("Тип входа:") && message.Contains("10") ||
                   message.Contains("Logon Type:") && message.Contains("10");
        }

        private static ThreatItem ParseLogonEvent(EventLogEntry entry)
        {
            string message = entry.Message ?? string.Empty;
            string who = ExtractLineValue(message, "Учетная запись:", "Account Name:");
            string from = ExtractLineValue(message, "Адрес сети:", "Source Network Address:");
            if (string.IsNullOrWhiteSpace(from) || from == "-")
            {
                from = ExtractLineValue(message, "Код безопасности:", "Ip Address:");
            }

            return new ThreatItem
            {
                TimeUtc = entry.TimeGenerated.ToUniversalTime(),
                Who = string.IsNullOrWhiteSpace(who) ? "—" : who.Trim(),
                From = string.IsNullOrWhiteSpace(from) ? "—" : from.Trim(),
                What = entry.InstanceId == 4625 ? "Неудачный вход" : "Удалённый вход (4624)",
                Tool = entry.InstanceId == 4625 ? "Попытка" : "RDP/сеть",
                Detail = "Event " + entry.InstanceId
            };
        }

        private static string ExtractLineValue(string message, string ruLabel, string enLabel)
        {
            foreach (string line in message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith(ruLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring(ruLabel.Length).Trim();
                }

                if (trimmed.StartsWith(enLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed.Substring(enLabel.Length).Trim();
                }
            }

            return string.Empty;
        }
    }
}
