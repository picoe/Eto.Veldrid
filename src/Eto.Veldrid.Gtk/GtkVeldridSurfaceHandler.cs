using Eto.GtkSharp.Forms;
using Eto.Veldrid;
using Eto.Veldrid.Gtk;
using Gtk;
using System;
using Veldrid;
using Veldrid.OpenGL;

[assembly: Eto.ExportHandler(typeof(VeldridSurface), typeof(GtkVeldridSurfaceHandler))]

namespace Eto.Veldrid.Gtk
{
	public class GtkVeldridSurfaceHandler : GtkControl<GtkVeldridDrawingArea, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		// TODO: Find out if Gtk3 even supports different DPI settings, and if
		// so test it out and get this to return the correct values.
		public int RenderWidth => Widget.Width;
		public int RenderHeight => Widget.Height;

		public GtkVeldridSurfaceHandler()
		{
			Control = new GtkVeldridDrawingArea();

#if GTK3
			Control.Drawn += Control_Drawn;
#else
			Control.ExposeEvent += Control_ExposeEvent;
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

#if GTK3
		void Control_Drawn(object o, DrawnArgs args)
#else
		void Control_ExposeEvent(object o, ExposeEventArgs args)
#endif
		{
			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				Control.CreateOpenGLContext();

				Callback.OnOpenGLReady(Widget, EventArgs.Empty);
			}

			Callback.OnControlReady(Widget, EventArgs.Empty);

#if GTK3
			Control.Drawn -= Control_Drawn;
#else
			Control.ExposeEvent -= Control_ExposeEvent;
#endif
		}

		/// <summary>
		/// Prepare this VeldridSurface to use OpenGL.
		/// </summary>
		public void InitializeOpenGL()
		{
			var platformInfo = new OpenGLPlatformInfo(
				VeldridGL.GetGLContextHandle(),
				VeldridGL.GetProcAddress,
				Control.MakeCurrent,
				VeldridGL.GetCurrentContext,
				VeldridGL.ClearCurrentContext,
				VeldridGL.DeleteContext,
				VeldridGL.SwapBuffers,
				VeldridGL.SetVSync,
				VeldridGL.SetSwapchainFramebuffer,
				VeldridGL.ResizeSwapchain);

			Widget.GraphicsDevice = GraphicsDevice.CreateOpenGL(
				Widget.GraphicsDeviceOptions,
				platformInfo,
				(uint)RenderWidth,
				(uint)RenderHeight);

			Widget.Swapchain = Widget.GraphicsDevice.MainSwapchain;

			Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
		}

		public void InitializeOtherApi()
		{
			// To embed Veldrid in an Eto control, all these platform-specific
			// versions of InitializeOtherApi use the technique outlined here:
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

			Widget.Swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
				new SwapchainDescription(
					source,
					(uint)RenderWidth,
					(uint)RenderHeight,
					Widget.GraphicsDeviceOptions.SwapchainDepthFormat,
					Widget.GraphicsDeviceOptions.SyncToVerticalBlank,
					Widget.GraphicsDeviceOptions.SwapchainSrgbFormat));

			Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
		}
	}
}
