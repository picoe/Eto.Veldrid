using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Gtk;
using OpenTK;
using System;
using Veldrid;

namespace PlaceholderName
{
	public class GtkVeldridSurfaceHandler : Eto.GtkSharp.Forms.Controls.PanelHandler, VeldridSurface.IHandler
	{
		public new VeldridSurface.ICallback Callback => (VeldridSurface.ICallback)base.Callback;
		public new VeldridSurface Widget => (VeldridSurface)base.Widget;

		public void InitializeOtherApi()
		{
			// To embed Veldrid in an Eto control, all these platform-specific
			// versions of InitializeOtherApi use the technique outlined here:
			//
			//   https://github.com/mellinoe/veldrid/issues/155
			//
			var source = SwapchainSource.CreateXlib(
				Control.Display.Handle,
				Control.GdkWindow.Handle);

			Widget.Swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
				new SwapchainDescription(
					source,
					(uint)Widget.Width,
					(uint)Widget.Height,
					PixelFormat.R32_Float,
					false));

			Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
		}
	}

	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			GraphicsBackend backend = VeldridSurface.PreferredBackend;

			if (backend == GraphicsBackend.OpenGL)
			{
				Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });
			}

			var platform = new Eto.GtkSharp.Platform();
			platform.Add<GLSurface.IHandler>(() => new GtkGlSurfaceHandler());
			platform.Add<VeldridSurface.IHandler>(() => new GtkVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm(backend));
		}
	}
}
