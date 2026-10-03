using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace SidePanelApp.V6.Infrastructure
{
    internal static class AdminElevationHelper
    {
        private const int ErrorAccessDenied = 5;

        public static bool IsRunningAsAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAccessDenied(int win32ErrorCode)
        {
            return win32ErrorCode == ErrorAccessDenied;
        }

        /// <summary>Запрос UAC и перезапуск exe с runas.</summary>
        public static void PromptRestartElevated(IWin32Window owner, string reason)
        {
            if (IsRunningAsAdministrator())
            {
                MessageBox.Show(
                    owner,
                    reason + Environment.NewLine + Environment.NewLine +
                    "Приложение уже запущено от администратора. Операция всё равно не удалась.",
                    "Connections",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            DialogResult answer = MessageBox.Show(
                owner,
                reason + Environment.NewLine + Environment.NewLine +
                "Перезапустить SidePanelApp с запросом прав администратора (UAC)?",
                "Требуются права администратора",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            try
            {
                string exe = Application.ExecutablePath;
                string workDir = Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory;
                var start = new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = workDir,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(start);
                Application.Exit();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show(
                    owner,
                    "Запуск от имени администратора отменён или заблокирован.",
                    "Connections",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    "Не удалось перезапустить с повышением прав: " + ex.Message,
                    "Connections",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
