using System;
using SharpPcap;
using SidePanelApp.V6.Infrastructure;

namespace SidePanelApp.V6.Connections
{
    internal static class NpcapAvailability
    {
        public static bool IsCaptureAvailable()
        {
            if (!NpcapInstallHelper.IsRegisteredInRegistry())
            {
                return false;
            }

            try
            {
                return CaptureDeviceList.Instance.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        public static string GetBannerText()
        {
            string installed = NpcapInstallHelper.FormatInstalledLabelForUi();

            if (IsCaptureAvailable())
            {
                if (!string.IsNullOrEmpty(installed))
                {
                    return installed + ". Захват доступен — нажмите «Старт» для HEX/ASCII.";
                }

                return "Npcap: захват доступен — можно нажать «Старт» для HEX/ASCII.";
            }

            if (!string.IsNullOrEmpty(installed))
            {
                return installed + ", но адаптеры захвата недоступны. Перезапустите приложение или запустите от администратора.";
            }

            return "Npcap не найден. Скачайте установщик (npcap-1.89.exe) или установите локально. Без Npcap — только счётчики TCP.";
        }
    }
}
