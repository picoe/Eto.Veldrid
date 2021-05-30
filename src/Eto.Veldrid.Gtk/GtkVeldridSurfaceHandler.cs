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
	public class GtkVeldridSurfaceHandler : GtkControl<EtoEventBox, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler, VeldridSurface.IOpenGL
	{
		GLArea glArea;
		public Size RenderSize => Size.Round((SizeF)Widget.Size * Scale);

		float Scale => Widget.ParentWindow?.Screen?.LogicalPixelSize ?? 1;

		public override global::Gtk.Widget ContainerContentControl => glArea ?? base.ContainerContentControl;

		public GtkVeldridSurfaceHandler()
		{
			Control = new EtoEventBox { Handler = this };
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

		void glArea_InitializeGraphicsBackend(object sender, EventArgs e)
		{
			glArea.Context.MakeCurrent();
			Callback.OnInitializeBackend(Widget, new InitializeEventArgs(RenderSize));

			glArea.Render += glArea_Render;
			glArea.Resize += glArea_Resize;
		}

		void Control_InitializeGraphicsBackend(object sender, EventArgs e)
		{
			Callback.OnInitializeBackend(Widget, new InitializeEventArgs(RenderSize));
		}

		bool skipDraw;

		private void glArea_Resize(object o, ResizeArgs args)
		{
			skipDraw = false;
			Callback.OnResize(Widget, new ResizeEventArgs(RenderSize));
		}

		void glArea_Render(object o, RenderArgs args)
		{
			if (!skipDraw)
			{
				skipDraw = true;
				Callback.OnDraw(Widget, EventArgs.Empty);
			}
			skipDraw = false;
		}

		// TODO: Figure this one out! The docstring for this property in Veldrid's OpenGLPlatformInfo is ambiguous.
		IntPtr VeldridSurface.IOpenGL.OpenGLContextHandle => glArea?.Context.Handle ?? IntPtr.Zero;

		IntPtr VeldridSurface.IOpenGL.GetProcAddress(string name) => X11Interop.glXGetProcAddress(name);

		void VeldridSurface.IOpenGL.MakeCurrent(IntPtr context) => glArea?.MakeCurrent();

		IntPtr VeldridSurface.IOpenGL.GetCurrentContext() => Gdk.GLContext.Current.Handle;

		void VeldridSurface.IOpenGL.ClearCurrentContext() => Gdk.GLContext.ClearCurrent();

		void VeldridSurface.IOpenGL.DeleteContext(IntPtr context)
		{
		}

		void VeldridSurface.IOpenGL.SwapBuffers()
		{
			// GLArea doesn't support drawing directly, so we queue a render but don't actually call OnDraw
			if (skipDraw)
				return;

			skipDraw = true;
			glArea?.QueueRender();
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

		void Eto.Forms.Control.IHandler.Invalidate(Rectangle rect, bool invalidateChildren)
		{
			skipDraw = false;
			glArea?.QueueRender();
		}

		void Eto.Forms.Control.IHandler.Invalidate(bool invalidateChildren)
		{
			skipDraw = false;
			glArea?.QueueRender();
		}


		protected override void Initialize()
		{
			base.Initialize();
			
			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				glArea = new GLArea();
				glArea.CanFocus = true;

				// Veldrid technically supports as low as OpenGL 3.0, but the full
				// complement of features is only available with 3.3 and higher.
				glArea.SetRequiredVersion(3, 3);

				glArea.HasDepthBuffer = true;
				glArea.HasStencilBuffer = true;
				Control.Child = glArea;
				glArea.Realized += glArea_InitializeGraphicsBackend;
			}
			else
			{
				Control.CanFocus = true;
				Control.Realized += Control_InitializeGraphicsBackend;				
			}

		}
	}
}
