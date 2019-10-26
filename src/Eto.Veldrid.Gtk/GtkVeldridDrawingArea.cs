using Eto.Forms;
using Gdk;
using Gtk;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;

namespace Eto.Veldrid.Gtk
{
	public class GtkVeldridDrawingArea : DrawingArea
	{
		public IWindowInfo WindowInfo { get; protected set; }

		public event EventHandler WindowInfoUpdated;

		public GtkVeldridDrawingArea()
		{
			CanFocus = true;
		}

		public IWindowInfo UpdateWindowInfo(GraphicsMode mode)
		{
			WindowInfo?.Dispose();

			IntPtr display = X11Interop.gdk_x11_display_get_xdisplay(Display.Handle);
			int screen = X11Interop.gdk_x11_screen_get_screen_number(Screen.Handle);

			IntPtr visualInfo;
			if (mode.Index.HasValue)
			{
				var template = new X11Interop.XVisualInfo { VisualID = mode.Index.Value };

				visualInfo = X11Interop.XGetVisualInfo(display, (IntPtr)(int)X11Interop.XVisualInfoMask.ID, ref template, out _);
			}
			else
			{
				visualInfo = X11Interop.GetVisualInfo(display, screen);
			}

			WindowInfo = Utilities.CreateX11WindowInfo(
				display,
				screen,
#if GTK3
				X11Interop.gdk_x11_window_get_xid(Window.Handle),
				X11Interop.gdk_x11_window_get_xid(Screen.RootWindow.Handle),
#else
				X11Interop.gdk_x11_drawable_get_xid(GdkWindow.Handle),
				X11Interop.gdk_x11_drawable_get_xid(RootWindow.Handle),
#endif
				visualInfo);

			X11Interop.XFree(visualInfo);

			WindowInfoUpdated?.Invoke(this, EventArgs.Empty);

			return WindowInfo;
		}
	}
}
