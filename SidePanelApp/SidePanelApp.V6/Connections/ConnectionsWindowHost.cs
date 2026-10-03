using System.Windows.Forms;
using SidePanelApp.V6.Network;

namespace SidePanelApp.V6.Connections
{
    internal static class ConnectionsWindowHost
    {
        private static ConnectionsForm _form;

        public static void ShowOrActivate(IWin32Window owner)
        {
            if (_form == null || _form.IsDisposed)
            {
                _form = new ConnectionsForm();
            }

            if (!_form.Visible)
            {
                if (owner != null)
                {
                    _form.Show(owner);
                }
                else
                {
                    _form.Show();
                }
            }
            else
            {
                _form.BringToFront();
                _form.Activate();
            }
        }
    }
}
