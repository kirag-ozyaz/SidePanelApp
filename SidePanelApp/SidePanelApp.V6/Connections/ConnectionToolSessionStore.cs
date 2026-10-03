using System;
using System.Collections.Generic;
using System.Text;

namespace SidePanelApp.V6.Connections
{
    /// <summary>Текст инструментов по ключу соединения до его исчезновения (отдельные store для ping/trace и пакетов).</summary>
    internal sealed class ConnectionToolSessionStore
    {
        private readonly Dictionary<string, string> _outputs =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public string Get(string connectionKey)
        {
            if (string.IsNullOrEmpty(connectionKey))
            {
                return string.Empty;
            }

            string text;
            return _outputs.TryGetValue(connectionKey, out text) ? text : string.Empty;
        }

        public void Set(string connectionKey, string text)
        {
            if (string.IsNullOrEmpty(connectionKey))
            {
                return;
            }

            _outputs[connectionKey] = text ?? string.Empty;
        }

        public void Append(string connectionKey, string title, string body)
        {
            if (string.IsNullOrEmpty(connectionKey))
            {
                return;
            }

            var sb = new StringBuilder();
            string existing;
            if (_outputs.TryGetValue(connectionKey, out existing) && !string.IsNullOrEmpty(existing))
            {
                sb.AppendLine(existing);
                sb.AppendLine();
            }

            sb.AppendLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + title);
            sb.AppendLine(body);
            _outputs[connectionKey] = sb.ToString();
        }

        public void AppendPlainLine(string connectionKey, string line)
        {
            if (string.IsNullOrEmpty(connectionKey) || string.IsNullOrEmpty(line))
            {
                return;
            }

            string existing;
            if (_outputs.TryGetValue(connectionKey, out existing) && !string.IsNullOrEmpty(existing))
            {
                _outputs[connectionKey] = existing + line + Environment.NewLine;
            }
            else
            {
                _outputs[connectionKey] = line + Environment.NewLine;
            }
        }

        public void PruneInactive(HashSet<string> activeConnectionKeys)
        {
            if (activeConnectionKeys == null)
            {
                return;
            }

            var remove = new List<string>();
            foreach (string key in _outputs.Keys)
            {
                if (!activeConnectionKeys.Contains(key))
                {
                    remove.Add(key);
                }
            }

            foreach (string key in remove)
            {
                _outputs.Remove(key);
            }
        }
    }
}
