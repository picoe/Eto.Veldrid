using System.Windows.Forms;

namespace Eto.Veldrid.WinForms
{
	public class WinFormsVeldridUserControl : UserControl
	{
		public WinFormsVeldridUserControl()
		{
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			DoubleBuffered = false;
		}
	}
}
