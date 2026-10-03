using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SidePanelApp.V6.Infrastructure
{
    /// <summary>
    /// Одна копия серии SidePanelApp (V1–V6) на машине: предложение закрыть уже запущенную.
    /// </summary>
    internal static class SeriesInstanceGuard
    {
        private static readonly string[] SeriesProcessNames =
        {
            "SidePanelApp",
            "SidePanelApp.V2",
            "SidePanelApp.V3",
            "SidePanelApp.V4",
            "SidePanelApp.V5",
            "SidePanelApp.V6"
        };

        public static bool TryResolveConflicts()
        {
            Process current = Process.GetCurrentProcess();
            List<Process> others = FindOtherSeriesProcesses(current.Id);
            if (others.Count == 0)
            {
                return true;
            }

            string message = BuildMessage(others);
            DialogResult result = MessageBox.Show(
                message,
                "SidePanelApp — уже запущено",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return false;
            }

            if (!TryCloseProcesses(others, out string error))
            {
                MessageBox.Show(
                    error,
                    "SidePanelApp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            others = FindOtherSeriesProcesses(current.Id);
            if (others.Count > 0)
            {
                MessageBox.Show(
                    "Не удалось полностью завершить другие копии. Закройте их вручную и запустите снова.",
                    "SidePanelApp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private static List<Process> FindOtherSeriesProcesses(int currentPid)
        {
            var result = new List<Process>();
            foreach (string name in SeriesProcessNames)
            {
                Process[] processes;
                try
                {
                    processes = Process.GetProcessesByName(name);
                }
                catch
                {
                    continue;
                }

                foreach (Process process in processes)
                {
                    try
                    {
                        if (process.Id != currentPid && !process.HasExited)
                        {
                            result.Add(process);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return result;
        }

        private static string BuildMessage(List<Process> others)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Уже запущена другая копия SidePanelApp:");
            sb.AppendLine();
            foreach (Process process in others)
            {
                try
                {
                    sb.AppendLine("  • " + process.ProcessName + " (PID " + process.Id + ")");
                }
                catch
                {
                    sb.AppendLine("  • (процесс недоступен)");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Одновременно должна работать только одна версия (V1–V6).");
            sb.AppendLine();
            sb.AppendLine("Закрыть найденные процессы и запустить эту копию?");
            return sb.ToString();
        }

        private static bool TryCloseProcesses(List<Process> processes, out string error)
        {
            error = null;
            foreach (Process process in processes.Distinct())
            {
                try
                {
                    if (process.HasExited)
                    {
                        continue;
                    }

                    process.CloseMainWindow();
                    if (!process.WaitForExit(3000))
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    error = "Не удалось завершить " + SafeProcessName(process) + ": " + ex.Message;
                    return false;
                }
                finally
                {
                    process.Dispose();
                }
            }

            return true;
        }

        private static string SafeProcessName(Process process)
        {
            try
            {
                return process.ProcessName;
            }
            catch
            {
                return "процесс";
            }
        }
    }
}
