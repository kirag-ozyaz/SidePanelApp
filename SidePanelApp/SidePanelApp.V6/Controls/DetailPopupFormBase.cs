using System.Windows.Forms;

namespace SidePanelApp.V6.Controls
{
    /// <summary>
    /// Всплывающие карточки: не забирают фокус и не показывают курсор ожидания при Show.
    /// </summary>
    public class DetailPopupFormBase : Form
    {
        private const int WS_EX_NOACTIVATE = 0x08000000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
    }
}
