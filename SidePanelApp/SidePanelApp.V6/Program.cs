using System;
using System.Windows.Forms;
using SidePanelApp.V6.Infrastructure;

namespace SidePanelApp.V6
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!SeriesInstanceGuard.TryResolveConflicts())
            {
                return;
            }

            Application.Run(new TrayAppContext());
        }
    }
}
