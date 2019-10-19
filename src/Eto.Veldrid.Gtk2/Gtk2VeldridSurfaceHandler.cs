using Eto.GtkSharp.Forms;
using Eto.Veldrid;
using Eto.Veldrid.Gtk2;
using Gtk;
using System;
using Veldrid;
using Veldrid.OpenGL;

[assembly: Eto.ExportHandler(typeof(VeldridSurface), typeof(Gtk2VeldridSurfaceHandler))]

namespace Eto.Veldrid.Gtk2
{
	public class Gtk2VeldridSurfaceHandler : GtkControl<Gtk2VeldridDrawingArea, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		// TODO: Find out if Gtk2 even supports different DPI settings, and if
		// so test it out and get this to return the correct values.
		public int RenderWidth => Widget.Width;
		public int RenderHeight => Widget.Height;

		public Gtk2VeldridSurfaceHandler()
		{
			Control = new Gtk2VeldridDrawingArea();

			Control.ExposeEvent += Control_ExposeEvent;
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
					Control.ExposeEvent += (sender, e) => Callback.OnDraw(Widget, e);
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}

		void Control_ExposeEvent(object o, ExposeEventArgs args)
		{
			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				Control.CreateOpenGLContext();

				Callback.OnOpenGLReady(Widget, EventArgs.Empty);
			}

			Control.ExposeEvent -= Control_ExposeEvent;

			Callback.OnControlReady(Widget, EventArgs.Empty);
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
				X11Interop.gdk_x11_drawable_get_xid(Control.GdkWindow.Handle));

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
