using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Mac;
using Eto.VeldridSurface;
using OpenTK;
using System;
using Veldrid;

namespace PlaceholderName
{
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

			if (backend == GraphicsBackend.OpenGL)
			{
				platform.Add<GLSurface.IHandler>(() => new MacGLSurfaceHandler());
			}

			var app = new Application(platform);

			var form = new MainForm(MacInit, backend);

			app.Run(form);
		}

		public static void MacInit(VeldridSurface surface, GraphicsBackend backend, Action draw, Action<int, int> resize)
		{
			if (backend == GraphicsBackend.Metal)
			{
				surface.GraphicsDevice = GraphicsDevice.CreateMetal(new GraphicsDeviceOptions());
			}
			else
			{
				string message;
				if (!Enum.IsDefined(typeof(GraphicsBackend), backend))
				{
					message = "Unrecognized backend!";
				}
				else
				{
					message = "Specified backend not supported on this platform!";
				}

				throw new ArgumentException(message);
			}

			var source = SwapchainSource.CreateNSView(surface.NativeHandle);
			surface.Swapchain = surface.GraphicsDevice.ResourceFactory.CreateSwapchain(
				new SwapchainDescription(source, 640, 480, null, false));
		}
	}
}
