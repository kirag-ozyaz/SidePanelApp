using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SidePanelApp.V6;

namespace SidePanelApp.V6.Connections
{
    /// <summary>Список «пакетов» сверху, дерево и hex снизу (раскладка как Wireshark).</summary>
    internal sealed class ConnectionPacketWiresharkView : UserControl
    {
        private readonly DataGridView _grid;
        private readonly TreeView _tree;
        private readonly TextBox _hex;
        private readonly Label _status;
        private readonly BindingList<PacketListRow> _binding = new BindingList<PacketListRow>();
        private readonly SplitContainer _splitMain;
        private readonly SplitContainer _splitBottom;
        private bool _suppressSelectionRestore;

        public ConnectionPacketWiresharkView()
        {
            Dock = DockStyle.Fill;
            BackColor = PanelColors.SectionFill;

            _splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 120,
                BackColor = PanelColors.SectionFill
            };

            _splitBottom = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 140,
                BackColor = PanelColors.SectionFill
            };

            _grid = CreateGrid();
            _tree = CreateTree();
            _hex = CreateHexBox();
            _status = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Padding = new Padding(4, 2, 4, 0),
                Font = new Font("Segoe UI", 8F),
                ForeColor = PanelColors.TextMuted,
                Text = "Packets: 0"
            };

            _splitMain.Panel1.Controls.Add(_grid);
            _splitBottom.Panel1.Controls.Add(_tree);
            _splitBottom.Panel2.Controls.Add(_hex);
            _splitMain.Panel2.Controls.Add(_splitBottom);

            Controls.Add(_splitMain);
            Controls.Add(_status);

            _grid.SelectionChanged += Grid_SelectionChanged;
            _grid.CellFormatting += Grid_CellFormatting;
            ApplyTheme();
        }

        public void Clear()
        {
            _binding.Clear();
            _tree.Nodes.Clear();
            _hex.Clear();
            UpdateStatus();
        }

        public void SetRows(IReadOnlyList<PacketListRow> rows)
        {
            _binding.Clear();
            if (rows != null)
            {
                foreach (PacketListRow row in rows)
                {
                    _binding.Add(row);
                }
            }

            UpdateStatus();
            if (_binding.Count > 0)
            {
                SelectRowByIndex(_binding.Count - 1, scrollToSelection: true);
            }
            else
            {
                _tree.Nodes.Clear();
                _hex.Clear();
            }
        }

        public void AddRow(PacketListRow row)
        {
            if (row == null)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AddRow(row)));
                return;
            }

            int? selectedNumber = GetSelectedPacketNumber();
            int scrollIndex = _grid.FirstDisplayedScrollingRowIndex;

            _binding.Add(row);
            UpdateStatus();

            if (selectedNumber.HasValue)
            {
                RestoreSelectionByNumber(selectedNumber.Value, scrollIndex);
            }
            else if (scrollIndex >= 0 && _grid.Rows.Count > 0)
            {
                int idx = Math.Min(scrollIndex, _grid.Rows.Count - 1);
                if (_grid.FirstDisplayedScrollingRowIndex != idx)
                {
                    _grid.FirstDisplayedScrollingRowIndex = idx;
                }
            }
        }

        private int? GetSelectedPacketNumber()
        {
            PacketListRow selected = _grid.CurrentRow?.DataBoundItem as PacketListRow;
            if (selected == null)
            {
                return null;
            }

            return selected.Number;
        }

        private void RestoreSelectionByNumber(int packetNumber, int scrollIndex)
        {
            _suppressSelectionRestore = true;
            try
            {
                bool found = false;
                foreach (DataGridViewRow gridRow in _grid.Rows)
                {
                    PacketListRow item = gridRow.DataBoundItem as PacketListRow;
                    if (item != null && item.Number == packetNumber)
                    {
                        _grid.ClearSelection();
                        gridRow.Selected = true;
                        _grid.CurrentCell = gridRow.Cells[0];
                        found = true;
                        break;
                    }
                }

                if (!found && _grid.Rows.Count > 0)
                {
                    int idx = Math.Max(0, _grid.Rows.Count - 2);
                    SelectRowByIndex(idx, scrollToSelection: false);
                }

                if (scrollIndex >= 0 && _grid.Rows.Count > 0)
                {
                    int idx = Math.Min(scrollIndex, _grid.Rows.Count - 1);
                    if (_grid.FirstDisplayedScrollingRowIndex != idx)
                    {
                        _grid.FirstDisplayedScrollingRowIndex = idx;
                    }
                }
            }
            finally
            {
                _suppressSelectionRestore = false;
            }
        }

        private void SelectRowByIndex(int index, bool scrollToSelection)
        {
            if (index < 0 || index >= _grid.Rows.Count)
            {
                return;
            }

            _grid.ClearSelection();
            _grid.Rows[index].Selected = true;
            _grid.CurrentCell = _grid.Rows[index].Cells[0];
            if (scrollToSelection)
            {
                int keep = Math.Max(0, index - 2);
                _grid.FirstDisplayedScrollingRowIndex = keep;
            }
        }

        public void ApplyTheme()
        {
            Color rowBack = PanelColors.Background;
            Color rowSelect = Color.FromArgb(45, 75, 95);

            BackColor = PanelColors.SectionFill;
            _splitMain.BackColor = PanelColors.SectionFill;
            _splitBottom.BackColor = PanelColors.SectionFill;
            _splitMain.Panel1.BackColor = PanelColors.SectionFill;
            _splitMain.Panel2.BackColor = PanelColors.SectionFill;
            _splitBottom.Panel1.BackColor = PanelColors.SectionFill;
            _splitBottom.Panel2.BackColor = PanelColors.SectionFill;
            _status.ForeColor = PanelColors.TextMuted;
            _status.BackColor = PanelColors.SectionFill;

            _grid.EnableHeadersVisualStyles = false;
            _grid.BackgroundColor = rowBack;
            _grid.GridColor = PanelColors.CalloutBorder;
            _grid.BorderStyle = BorderStyle.None;
            _grid.DefaultCellStyle.BackColor = rowBack;
            _grid.DefaultCellStyle.ForeColor = PanelColors.TextPrimary;
            _grid.DefaultCellStyle.SelectionBackColor = rowSelect;
            _grid.DefaultCellStyle.SelectionForeColor = PanelColors.TextPrimary;
            _grid.ColumnHeadersDefaultCellStyle.BackColor = PanelColors.SectionFill;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = PanelColors.TextPrimary;
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PanelColors.SectionFill;
            _grid.RowHeadersDefaultCellStyle.BackColor = PanelColors.SectionFill;

            foreach (DataGridViewColumn column in _grid.Columns)
            {
                column.DefaultCellStyle.BackColor = rowBack;
                column.DefaultCellStyle.ForeColor = PanelColors.TextPrimary;
                column.DefaultCellStyle.SelectionBackColor = rowSelect;
                column.DefaultCellStyle.SelectionForeColor = PanelColors.TextPrimary;
            }

            _tree.BackColor = rowBack;
            _tree.ForeColor = PanelColors.TextPrimary;
            _tree.LineColor = PanelColors.CalloutBorder;
            _hex.BackColor = rowBack;
            _hex.ForeColor = PanelColors.TextPrimary;
            _hex.BorderStyle = BorderStyle.None;
        }

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                BorderStyle = BorderStyle.None,
                DataSource = _binding,
                ScrollBars = ScrollBars.Both
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Number",
                HeaderText = "No.",
                Width = 42,
                MinimumWidth = 36
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Time",
                HeaderText = "Time",
                Width = 72,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "HH:mm:ss.fff" }
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Source",
                HeaderText = "Source",
                Width = 88
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Destination",
                HeaderText = "Destination",
                Width = 88
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Protocol",
                HeaderText = "Protocol",
                Width = 52
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Length",
                HeaderText = "Length",
                Width = 52
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "Info",
                HeaderText = "Info",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 80
            });

            return grid;
        }

        private static TreeView CreateTree()
        {
            return new TreeView
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 8.25F),
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true
            };
        }

        private static TextBox CreateHexBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 8.25F),
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                HideSelection = false
            };
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressSelectionRestore)
            {
                return;
            }

            PacketListRow row = _grid.CurrentRow?.DataBoundItem as PacketListRow;
            if (row == null)
            {
                _tree.Nodes.Clear();
                _hex.Clear();
                return;
            }

            _tree.BeginUpdate();
            _tree.Nodes.Clear();
            foreach (PacketDetailNode node in row.Details)
            {
                _tree.Nodes.Add(CreateTreeNode(node));
            }

            _tree.ExpandAll();
            _tree.EndUpdate();
            _hex.Text = row.HexDump ?? string.Empty;
        }

        private static TreeNode CreateTreeNode(PacketDetailNode node)
        {
            var treeNode = new TreeNode(node.Text);
            if (node.Children != null)
            {
                foreach (PacketDetailNode child in node.Children)
                {
                    treeNode.Nodes.Add(CreateTreeNode(child));
                }
            }

            return treeNode;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            PacketListRow row = _grid.Rows[e.RowIndex].DataBoundItem as PacketListRow;
            if (row == null)
            {
                return;
            }

            e.CellStyle.BackColor = PanelColors.Background;
            bool selected = _grid.CurrentRow != null && e.RowIndex == _grid.CurrentRow.Index;
            if (selected)
            {
                e.CellStyle.ForeColor = PanelColors.TextPrimary;
                return;
            }

            if (row.IsSystem)
            {
                e.CellStyle.ForeColor = PanelColors.TextMuted;
                return;
            }

            e.CellStyle.ForeColor = row.IsOutbound ? PanelColors.NetAccent : PanelColors.ThreatAlert;
        }

        private void UpdateStatus()
        {
            _status.Text = string.Format("Packets: {0} | Displayed: {1}", _binding.Count, _binding.Count);
        }
    }
}
