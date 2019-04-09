using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Mac;
using OpenTK;
using System;
using Veldrid;

namespace PlaceholderName
{
	public class MacVeldridSurfaceHandler : VeldridSurfaceHandler
	{
		protected override void InitializeOtherApi()
		{
			base.InitializeOtherApi();

			var source = SwapchainSource.CreateNSView(Control.NativeHandle);
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

			var platform = new Eto.Mac.Platform();
			platform.Add<GLSurface.IHandler>(() => new MacGLSurfaceHandler());
			platform.Add<VeldridSurface.IHandler>(() => new MacVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm(backend));
		}
	}
}
