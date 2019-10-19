using Eto.Forms;
using Gdk;
using Gtk;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;

namespace Eto.Veldrid.Gtk2
{
	public class Gtk2VeldridDrawingArea : DrawingArea
	{
		GraphicsMode Mode = new GraphicsMode(new ColorFormat(32), 8, 8);

		public IWindowInfo WindowInfo { get; set; }
		public GraphicsContext Context { get; private set; }

		public Gtk2VeldridDrawingArea()
		{
			CanFocus = true;
		}

		public void CreateOpenGLContext()
		{
			IntPtr display = X11Interop.gdk_x11_display_get_xdisplay(Display.Handle);
			int screen = Screen.Number;

			IntPtr visualInfo;
			if (Mode.Index.HasValue)
			{
				var info = new X11Interop.XVisualInfo { VisualID = Mode.Index.Value };

				visualInfo = X11Interop.XGetVisualInfo(display, (IntPtr)(int)X11Interop.XVisualInfoMask.ID, ref info, out _);
			}
			else
			{
				visualInfo = X11Interop.GetVisualInfo(display, screen);
			}

			WindowInfo = Utilities.CreateX11WindowInfo(
				display,
				screen,
				X11Interop.gdk_x11_drawable_get_xid(GdkWindow.Handle),
				X11Interop.gdk_x11_drawable_get_xid(RootWindow.Handle),
				visualInfo);

			X11Interop.XFree(visualInfo);

			Context = new GraphicsContext(Mode, WindowInfo, 3, 3, GraphicsContextFlags.ForwardCompatible);

			Context.MakeCurrent(WindowInfo);
		}

		public void MakeCurrent(IntPtr context)
		{
			Context.MakeCurrent(WindowInfo);
		}
	}
}
