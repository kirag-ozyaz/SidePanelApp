using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SidePanelApp.V6.Infrastructure
{
    /// <summary>Npcap — отдельный драйвер; приложение поставляет только SharpPcap. См. Installer\README.txt.</summary>
    internal static class NpcapInstallHelper
    {
        /// <summary>Прямая ссылка на установщик (обновляйте при новой версии Npcap).</summary>
        public const string DownloadInstallerUrl = "https://npcap.com/dist/npcap-1.89.exe";

        public const string DownloadPageUrl = "https://npcap.com/#download";

        /// <summary>Относительно каталога exe (копируется из tools\npcap-1.89.exe при сборке).</summary>
        public const string BundledInstallerRelativePath = @"Installer\npcap-1.89.exe";

        public static string GetBundledInstallerPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, BundledInstallerRelativePath);
        }

        public static bool HasBundledInstaller()
        {
            return File.Exists(GetBundledInstallerPath());
        }

        /// <summary>Запись в реестре — быстрая проверка без загрузки SharpPcap.</summary>
        public static bool IsRegisteredInRegistry()
        {
            return !string.IsNullOrEmpty(GetInstalledVersionOrNull()) || OpenNpcapRegistryKey() != null;
        }

        /// <summary>Версия из реестра (Npcap / Programs and Features), если есть.</summary>
        public static string GetInstalledVersionOrNull()
        {
            string version = ReadVersionFromNpcapKey(@"SOFTWARE\Npcap");
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version.Trim();
            }

            version = ReadVersionFromNpcapKey(@"SOFTWARE\WOW6432Node\Npcap");
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version.Trim();
            }

            return ReadVersionFromUninstallEntry();
        }

        private static RegistryKey OpenNpcapRegistryKey()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Npcap");
                if (key != null)
                {
                    return key;
                }

                return Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Npcap");
            }
            catch
            {
                return null;
            }
        }

        private static string ReadVersionFromNpcapKey(string registryPath)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key == null)
                    {
                        return null;
                    }

                    string version = key.GetValue("Version") as string;
                    if (string.IsNullOrWhiteSpace(version))
                    {
                        version = key.GetValue("VersionString") as string;
                    }

                    return version;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string ReadVersionFromUninstallEntry()
        {
            string[] roots =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string root in roots)
            {
                try
                {
                    using (RegistryKey uninstall = Registry.LocalMachine.OpenSubKey(root))
                    {
                        if (uninstall == null)
                        {
                            continue;
                        }

                        foreach (string subKeyName in uninstall.GetSubKeyNames())
                        {
                            using (RegistryKey app = uninstall.OpenSubKey(subKeyName))
                            {
                                if (app == null)
                                {
                                    continue;
                                }

                                string name = app.GetValue("DisplayName") as string;
                                if (string.IsNullOrEmpty(name)
                                    || name.IndexOf("Npcap", StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    continue;
                                }

                                string version = app.GetValue("DisplayVersion") as string;
                                if (!string.IsNullOrWhiteSpace(version))
                                {
                                    return version.Trim();
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static string FormatInstalledLabel()
        {
            string version = GetInstalledVersionOrNull();
            if (!string.IsNullOrEmpty(version))
            {
                return "Npcap " + version + " установлен";
            }

            if (OpenNpcapRegistryKey() != null)
            {
                return "Npcap установлен";
            }

            return null;
        }

        internal static string FormatInstalledLabelForUi()
        {
            return FormatInstalledLabel();
        }

        public static void OpenDownloadPage()
        {
            try
            {
                Process.Start(new ProcessStartInfo(DownloadInstallerUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось открыть браузер: " + ex.Message + Environment.NewLine + DownloadInstallerUrl,
                    "Npcap",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>Запуск npcap-1.89.exe из папки Installer рядом с exe.</summary>
        public static bool TryLaunchBundledInstaller(IWin32Window owner)
        {
            string path = GetBundledInstallerPath();
            if (!File.Exists(path))
            {
                MessageBox.Show(
                    owner,
                    "Файл не найден:" + Environment.NewLine + path + Environment.NewLine + Environment.NewLine +
                    "Скачайте установщик (" + DownloadInstallerUrl + ") и положите его по этому пути (см. Installer\\README.txt).",
                    "Npcap",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            DialogResult confirm = MessageBox.Show(
                owner,
                "Запустить локальный установщик Npcap?" + Environment.NewLine + path,
                "Npcap",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(path)
                });
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Не удалось запустить: " + ex.Message, "Npcap", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
    }
}
