using System;
using System.Drawing;
using System.Windows.Forms;

namespace SidePanelApp.V6.Connections
{
    /// <summary>Секция: заголовок + ▼▼/▲▲ + произвольное содержимое.</summary>
    internal sealed class CollapsibleDetailSection : Panel
    {
        private const int HeaderHeight = 26;
        private readonly Panel _bodyHost;
        private readonly Button _toggle;
        private readonly Label _titleLabel;
        private bool _expanded = true;
        private int _expandedBodyHeight = 110;

        public CollapsibleDetailSection(string title, Control bodyContent)
        {
            BackColor = PanelColors.SectionFill;
            Padding = new Padding(0, 2, 0, 4);

            _titleLabel = new Label
            {
                Text = title,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = PanelColors.TextMuted,
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold)
            };

            _toggle = new Button
            {
                Text = "▼▼",
                Dock = DockStyle.Right,
                Width = 36,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                Cursor = Cursors.Hand,
                ForeColor = PanelColors.TextMuted,
                BackColor = PanelColors.SectionFill
            };
            _toggle.FlatAppearance.BorderSize = 0;
            _toggle.Click += (s, e) => ToggleExpanded();

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = HeaderHeight
            };
            header.Controls.Add(_titleLabel);
            header.Controls.Add(_toggle);
            header.Cursor = Cursors.Hand;
            header.Click += (s, e) => ToggleExpanded();

            _bodyHost = new Panel { Dock = DockStyle.Fill };
            bodyContent.Dock = DockStyle.Fill;
            _bodyHost.Controls.Add(bodyContent);

            Controls.Add(_bodyHost);
            Controls.Add(header);

            ApplyExpandedLayout();
        }

        public void SetTitle(string title)
        {
            _titleLabel.Text = title ?? string.Empty;
        }

        public bool StretchVertically { get; set; }

        public int ExpandedBodyHeight
        {
            get { return _expandedBodyHeight; }
            set
            {
                _expandedBodyHeight = Math.Max(48, value);
                if (_expanded && !StretchVertically)
                {
                    UpdateFixedHeight();
                }
            }
        }

        public bool Expanded
        {
            get { return _expanded; }
            set
            {
                if (_expanded == value)
                {
                    return;
                }

                _expanded = value;
                ApplyExpandedLayout();
            }
        }

        public void ApplyTheme()
        {
            BackColor = PanelColors.SectionFill;
            _titleLabel.ForeColor = PanelColors.TextMuted;
            _toggle.ForeColor = PanelColors.TextMuted;
            _toggle.BackColor = PanelColors.SectionFill;
            _bodyHost.BackColor = PanelColors.SectionFill;
        }

        private void ToggleExpanded()
        {
            Expanded = !_expanded;
        }

        private void ApplyExpandedLayout()
        {
            _bodyHost.Visible = _expanded;
            _toggle.Text = _expanded ? "▼▼" : "▲▲";
            if (StretchVertically)
            {
                return;
            }

            UpdateFixedHeight();
        }

        private void UpdateFixedHeight()
        {
            Height = _expanded
                ? HeaderHeight + _expandedBodyHeight + Padding.Vertical
                : HeaderHeight + Padding.Vertical;
        }
    }
}
