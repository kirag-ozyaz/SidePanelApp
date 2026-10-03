using System.Drawing;
using System.Windows.Forms;

namespace SidePanelApp.V6.Connections
{
    internal sealed class StatCardControl : UserControl
    {
        private string _caption = "";
        private string _value = "0";

        public StatCardControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Size = new Size(140, 52);
        }

        public string Caption
        {
            get { return _caption; }
            set { _caption = value ?? ""; Invalidate(); }
        }

        public string Value
        {
            get { return _value; }
            set { _value = value ?? ""; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush fill = new SolidBrush(PanelColors.Background))
            using (Pen border = new Pen(PanelColors.CalloutBorder))
            {
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                g.FillRectangle(fill, rect);
                g.DrawRectangle(border, rect);
            }

            using (Font captionFont = new Font("Segoe UI", 8F))
            using (Font valueFont = new Font("Segoe UI", 14F, FontStyle.Bold))
            using (SolidBrush muted = new SolidBrush(PanelColors.TextMuted))
            using (SolidBrush text = new SolidBrush(PanelColors.TextPrimary))
            {
                g.DrawString(_caption, captionFont, muted, 8, 6);
                g.DrawString(_value, valueFont, text, 8, 22);
            }
        }
    }
}
