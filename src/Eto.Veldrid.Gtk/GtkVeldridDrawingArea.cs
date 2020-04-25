using Gtk;

namespace Eto.Veldrid.Gtk
{
	public class GtkVeldridDrawingArea : GLArea
	{
		public GtkVeldridDrawingArea()
		{
			CanFocus = true;

			// Veldrid technically supports as low as OpenGL 3.0, but the full
			// complement of features is only available with 3.3 and higher.
			SetRequiredVersion(3, 3);

			HasDepthBuffer = true;
			HasStencilBuffer = true;
		}
	}
}
