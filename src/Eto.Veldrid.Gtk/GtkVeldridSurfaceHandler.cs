using Eto.Drawing;
using Eto.GtkSharp.Forms;
using Eto.Veldrid;
using Eto.Veldrid.Gtk;
using Gtk;
using System;
using Veldrid;

[assembly: Eto.ExportHandler(typeof(VeldridSurface), typeof(GtkVeldridSurfaceHandler))]

namespace Eto.Veldrid.Gtk
{
	public class GtkVeldridSurfaceHandler : GtkControl<GtkVeldridDrawingArea, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler, VeldridSurface.IOpenGL
	{
		public Size RenderSize => Size.Round((SizeF)Widget.Size * Scale);

		float Scale => Widget.ParentWindow?.LogicalPixelSize ?? 1;

		public GtkVeldridSurfaceHandler()
		{
			Control = new GtkVeldridDrawingArea();

			Control.Render += Control_InitializeGraphicsBackend;
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
					X11Interop.gdk_x11_window_get_xid(Control.Window.Handle));

				var renderSize = RenderSize;
				swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
					new SwapchainDescription(
						source,
						(uint)renderSize.Width,
						(uint)renderSize.Height,
						Widget.GraphicsDeviceOptions.SwapchainDepthFormat,
						Widget.GraphicsDeviceOptions.SyncToVerticalBlank,
						Widget.GraphicsDeviceOptions.SwapchainSrgbFormat));
			}

			return swapchain;
		}

		void Control_InitializeGraphicsBackend(object o, RenderArgs args)
		{
			Callback.OnInitializeBackend(Widget, new InitializeEventArgs(RenderSize));

			Control.Render -= Control_InitializeGraphicsBackend;
			Control.Render += Control_Render;
			Widget.SizeChanged += Widget_SizeChanged;
		}

		private void Widget_SizeChanged(object sender, EventArgs e) => Callback.OnResize(Widget, new ResizeEventArgs(RenderSize));

		void Control_Render(object o, RenderArgs args) => Callback.OnDraw(Widget, args);

		// TODO: Figure this one out! The docstring for this property in Veldrid's OpenGLPlatformInfo is ambiguous.
		IntPtr VeldridSurface.IOpenGL.OpenGLContextHandle => ((VeldridSurface.IOpenGL)this).GetCurrentContext();

		IntPtr VeldridSurface.IOpenGL.GetProcAddress(string name) => X11Interop.glXGetProcAddress(name);

		void VeldridSurface.IOpenGL.MakeCurrent(IntPtr context) => Control.MakeCurrent();

		IntPtr VeldridSurface.IOpenGL.GetCurrentContext() => Gdk.GLContext.Current.Handle;

		void VeldridSurface.IOpenGL.ClearCurrentContext() => Gdk.GLContext.ClearCurrent();

		void VeldridSurface.IOpenGL.DeleteContext(IntPtr context)
		{
		}

		void VeldridSurface.IOpenGL.SwapBuffers()
		{
			// This happens automatically in GLArea, so no need to do anything.
		}

		void VeldridSurface.IOpenGL.SetSyncToVerticalBlank(bool on)
		{
		}

		void VeldridSurface.IOpenGL.SetSwapchainFramebuffer()
		{
		}

		void VeldridSurface.IOpenGL.ResizeSwapchain(uint width, uint height)
		{
		}
	}
}
