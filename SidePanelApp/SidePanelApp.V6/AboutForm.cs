using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using SidePanelApp;

namespace SidePanelApp.V6
{
    /// <summary>
    /// Окно «О программе». Текст и номер берутся из сведений о сборке (AssemblyInfo).
    /// </summary>
    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string product = ReadAttribute(assembly, (AssemblyProductAttribute a) => a.Product, "SidePanelApp.V6");
            string version = assembly.GetName().Version != null
                ? assembly.GetName().Version.ToString()
                : "1.0.0.0";
            string fileVersion = ReadAttribute(assembly, (AssemblyFileVersionAttribute a) => a.Version, version);
            string copyright = ReadAttribute(assembly, (AssemblyCopyrightAttribute a) => a.Copyright, "");
            string description = ReadAttribute(
                assembly,
                (AssemblyDescriptionAttribute a) => a.Description,
                "Панель метрик у края экрана: процессор, память, видеокарта, сеть и доступ к компьютеру.");

            Text = "О программе";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = PanelColors.Background;
            ForeColor = PanelColors.TextPrimary;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(420, 220);

            Icon icon = AppIconFactory.GetApplicationIcon(48);
            Icon = icon;

            PictureBox picture = new PictureBox
            {
                Location = new Point(24, 22),
                Size = new Size(48, 48),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = PanelColors.Background,
                Image = icon.ToBitmap()
            };

            Label title = new Label
            {
                AutoSize = false,
                Location = new Point(88, 22),
                Size = new Size(308, 30),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = PanelColors.TextPrimary,
                BackColor = PanelColors.Background,
                Text = product
            };

            string versionText = "Версия " + version;
            if (!string.IsNullOrEmpty(fileVersion)
                && !string.Equals(fileVersion, version, StringComparison.Ordinal))
            {
                versionText += "   ·   файл " + fileVersion;
            }

            Label versionLabel = new Label
            {
                AutoSize = false,
                Location = new Point(90, 54),
                Size = new Size(306, 20),
                ForeColor = PanelColors.NetAccent,
                BackColor = PanelColors.Background,
                Text = versionText
            };

            Label body = new Label
            {
                AutoSize = false,
                Location = new Point(24, 96),
                Size = new Size(372, 56),
                ForeColor = PanelColors.TextMuted,
                BackColor = PanelColors.Background,
                Text = description
            };

            Label copy = new Label
            {
                AutoSize = false,
                Location = new Point(24, 178),
                Size = new Size(240, 22),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = PanelColors.TextMuted,
                BackColor = PanelColors.Background,
                Text = copyright
            };

            Button close = new Button
            {
                Text = "Закрыть",
                Size = new Size(100, 28),
                Location = new Point(296, 176),
                FlatStyle = FlatStyle.Flat,
                BackColor = PanelColors.SectionFill,
                ForeColor = PanelColors.TextPrimary,
                DialogResult = DialogResult.OK
            };
            close.FlatAppearance.BorderColor = PanelColors.CalloutBorder;

            AcceptButton = close;
            CancelButton = close;

            Controls.Add(picture);
            Controls.Add(title);
            Controls.Add(versionLabel);
            Controls.Add(body);
            Controls.Add(copy);
            Controls.Add(close);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Icon current = Icon;
                Icon = null;
                if (current != null)
                {
                    current.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public static void ShowAbout(IWin32Window owner)
        {
            using (AboutForm form = new AboutForm())
            {
                form.ShowDialog(owner);
            }
        }

        private static string ReadAttribute<T>(Assembly assembly, Func<T, string> read, string fallback) where T : Attribute
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(T), false);
            if (attributes.Length == 0)
            {
                return fallback;
            }

            string value = read((T)attributes[0]);
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
