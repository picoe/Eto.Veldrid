using Eto.GtkSharp.Forms;
using Eto.Veldrid;
using Eto.Veldrid.Gtk;
using Gtk;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;
using Veldrid;

[assembly: Eto.ExportHandler(typeof(VeldridSurface), typeof(GtkVeldridSurfaceHandler))]

namespace Eto.Veldrid.Gtk
{
	public class GtkVeldridSurfaceHandler : GtkControl<GtkVeldridDrawingArea, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		// TODO: Find out if Gtk3 even supports different DPI settings, and if
		// so test it out and get this to return the correct values.
		public int RenderWidth => Widget.Width;
		public int RenderHeight => Widget.Height;

		public IWindowInfo WindowInfo => Control.WindowInfo;

		public Action<uint, uint> ResizeSwapchain { get; protected set; }

		public GtkVeldridSurfaceHandler()
		{
			Control = new GtkVeldridDrawingArea();

#if GTK3
			Control.Drawn += Control_Drawn;
#else
			Control.ExposeEvent += Control_ExposeEvent;
#endif
			Control.WindowInfoUpdated += (sender, e) => Callback.OnWindowInfoUpdated(Widget, EventArgs.Empty);

			ResizeSwapchain = (w, h) => { };
		}

		public Swapchain CreateSwapchain()
		{
			Swapchain swapchain;

			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				swapchain = Widget.GraphicsDevice.MainSwapchain;
			}
			else
			{
				// To embed Veldrid in an Eto control, these platform-specific
				// versions of CreateSwapchain use the technique outlined here:
				//
				//   https://github.com/mellinoe/veldrid/issues/155
				//
				var source = SwapchainSource.CreateXlib(
					X11Interop.gdk_x11_display_get_xdisplay(Control.Display.Handle),
#if GTK3
					X11Interop.gdk_x11_window_get_xid(Control.Window.Handle));
#else
					X11Interop.gdk_x11_drawable_get_xid(Control.GdkWindow.Handle));
#endif

				swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
					new SwapchainDescription(
						source,
						(uint)RenderWidth,
						(uint)RenderHeight,
						Widget.GraphicsDeviceOptions.SwapchainDepthFormat,
						Widget.GraphicsDeviceOptions.SyncToVerticalBlank,
						Widget.GraphicsDeviceOptions.SwapchainSrgbFormat));
			}

			return swapchain;
		}

		public IWindowInfo UpdateWindowInfo(GraphicsMode mode) => Control.UpdateWindowInfo(mode);

#if GTK3
		private void Control_Drawn(object o, DrawnArgs args)
#else
		private void Control_ExposeEvent(object o, ExposeEventArgs args)
#endif
		{
			Callback.InitializeGraphicsBackend(Widget);

#if GTK3
			Control.Drawn -= Control_Drawn;
#else
			Control.ExposeEvent -= Control_ExposeEvent;
#endif
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
#if GTK3
					Control.Drawn += (sender, e) => Callback.OnDraw(Widget, e);
#else
					Control.ExposeEvent += (sender, e) => Callback.OnDraw(Widget, e);
#endif
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}
	}
}
