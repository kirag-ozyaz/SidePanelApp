using System.Drawing;
using System.Windows.Forms;
using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    /// <summary>Фон всей строки по TCP state (как семантика TCPView на тёмной теме).</summary>
    internal static class TcpConnectionRowStyle
    {
        private static readonly Color GridBackground = PanelColors.Background;
        private static readonly Color SelectionBase = Color.FromArgb(45, 75, 95);

        private enum RowBand
        {
            Default,
            Healthy,
            Transitional,
            Terminal
        }

        public static void ApplyCellStyle(DataGridViewCellFormattingEventArgs e, DataGridView grid, ConnectionRow row)
        {
            if (row == null)
            {
                return;
            }

            bool selected = grid.Rows[e.RowIndex].Selected;
            if (row.IsRecentlyClosed)
            {
                ApplyVanishedStyle(e, selected);
                return;
            }

            RowBand band = GetBand(row);
            Color back = GetBackground(band);
            Color fore = PanelColors.TextPrimary;

            if (selected)
            {
                e.CellStyle.BackColor = selected && band != RowBand.Default
                    ? Blend(back, SelectionBase, 0.5f)
                    : SelectionBase;
                e.CellStyle.ForeColor = fore;
                return;
            }

            e.CellStyle.BackColor = back;
            e.CellStyle.ForeColor = fore;
        }

        private static void ApplyVanishedStyle(DataGridViewCellFormattingEventArgs e, bool selected)
        {
            Color back = Tint(GridBackground, PanelColors.ThreatAlert, 0.62f);
            if (selected)
            {
                back = Blend(back, SelectionBase, 0.45f);
            }

            e.CellStyle.BackColor = back;
            e.CellStyle.ForeColor = PanelColors.TextPrimary;
        }

        private static RowBand GetBand(ConnectionRow row)
        {
            if (row.IsUdp || string.IsNullOrEmpty(row.State))
            {
                return RowBand.Default;
            }

            switch (row.State)
            {
                case "ESTABLISHED":
                case "LISTENING":
                    return RowBand.Healthy;
                case "SYN_SENT":
                case "SYN_RECEIVED":
                case "FIN_WAIT_1":
                case "FIN_WAIT_2":
                case "CLOSE_WAIT":
                case "CLOSING":
                case "LAST_ACK":
                case "TIME_WAIT":
                    return RowBand.Transitional;
                case "DELETE_TCB":
                case "CLOSED":
                case "UNKNOWN":
                    return RowBand.Terminal;
                default:
                    return RowBand.Default;
            }
        }

        private static Color GetBackground(RowBand band)
        {
            switch (band)
            {
                case RowBand.Healthy:
                    return Tint(GridBackground, PanelColors.ThreatOk, 0.24f);
                case RowBand.Transitional:
                    return Tint(GridBackground, PanelColors.ThreatWatch, 0.30f);
                case RowBand.Terminal:
                    return Tint(GridBackground, PanelColors.ThreatAlert, 0.48f);
                default:
                    return GridBackground;
            }
        }

        private static Color Tint(Color baseColor, Color accent, float amount)
        {
            if (amount <= 0f)
            {
                return baseColor;
            }

            if (amount >= 1f)
            {
                return accent;
            }

            float inv = 1f - amount;
            return Color.FromArgb(
                255,
                (int)(baseColor.R * inv + accent.R * amount),
                (int)(baseColor.G * inv + accent.G * amount),
                (int)(baseColor.B * inv + accent.B * amount));
        }

        private static Color Blend(Color a, Color b, float t)
        {
            if (t <= 0f)
            {
                return a;
            }

            if (t >= 1f)
            {
                return b;
            }

            float inv = 1f - t;
            return Color.FromArgb(
                255,
                (int)(a.R * inv + b.R * t),
                (int)(a.G * inv + b.G * t),
                (int)(a.B * inv + b.B * t));
        }
    }
}
