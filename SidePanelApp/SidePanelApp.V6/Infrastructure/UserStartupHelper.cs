using System;
using System.IO;
using System.Windows.Forms;

namespace SidePanelApp.V6.Infrastructure
{
    /// <summary>
    /// Автозапуск через ярлык в папке «Автозагрузка» текущего пользователя (без HKLM и без повышенных прав).
    /// </summary>
    internal static class UserStartupHelper
    {
        private const string ShortcutFileName = "SidePanelApp V6.lnk";

        private static readonly string[] LegacyShortcutNames =
        {
            "SidePanelApp.lnk",
            "SidePanelApp V2.lnk",
            "SidePanelApp V3.lnk",
            "SidePanelApp V4.lnk",
            "SidePanelApp V5.lnk"
        };

        public static bool IsRegistered()
        {
            return File.Exists(GetShortcutPath());
        }

        public static void Apply(bool enable)
        {
            if (enable)
            {
                RemoveLegacyShortcuts();
                CreateOrUpdateShortcut();
            }
            else
            {
                RemoveShortcut();
            }
        }

        /// <summary>Синхронизировать ярлык с настройкой (восстановить, если пользователь удалил вручную).</summary>
        public static void SyncFromSettings(bool shouldRunAtStartup)
        {
            if (shouldRunAtStartup)
            {
                if (!IsRegistered() || !ShortcutPointsToCurrentExe())
                {
                    Apply(true);
                }
            }
            else if (IsRegistered())
            {
                RemoveShortcut();
            }
        }

        private static string GetStartupFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        }

        private static string GetShortcutPath()
        {
            return Path.Combine(GetStartupFolder(), ShortcutFileName);
        }

        private static void CreateOrUpdateShortcut()
        {
            string exePath = Application.ExecutablePath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                return;
            }

            string shortcutPath = GetShortcutPath();
            string workDir = Path.GetDirectoryName(exePath) ?? string.Empty;

            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                return;
            }

            dynamic shell = Activator.CreateInstance(shellType);
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = workDir;
            shortcut.WindowStyle = 1;
            shortcut.Description = "SidePanelApp V6 — панель метрик и мониторинг АРМ";
            shortcut.Save();
        }

        private static bool ShortcutPointsToCurrentExe()
        {
            try
            {
                string shortcutPath = GetShortcutPath();
                if (!File.Exists(shortcutPath))
                {
                    return false;
                }

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    return false;
                }

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                string target = (string)shortcut.TargetPath;
                return string.Equals(
                    Path.GetFullPath(target),
                    Path.GetFullPath(Application.ExecutablePath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void RemoveShortcut()
        {
            try
            {
                string path = GetShortcutPath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static void RemoveLegacyShortcuts()
        {
            string folder = GetStartupFolder();
            foreach (string name in LegacyShortcutNames)
            {
                try
                {
                    string path = Path.Combine(folder, name);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
