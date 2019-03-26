using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Gtk;
using Eto.VeldridSurface;
using OpenTK;
using System;
using Veldrid;

namespace PlaceholderName
{
	public class GtkVeldridSurfaceHandler : VeldridSurfaceHandler
	{
		public override void InitializeGraphicsApi(Action draw, Action<int, int> resize)
		{
			string message;
			if (!Enum.IsDefined(typeof(GraphicsBackend), Callback.Backend))
			{
				message = "Unrecognized backend!";
			}
			else
			{
				message = "Specified backend not supported on this platform!";
			}

			throw new ArgumentException(message);
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

			if (backend == GraphicsBackend.OpenGL)
			{
				platform.Add<GLSurface.IHandler>(() => new GtkGlSurfaceHandler());
			}

			platform.Add<VeldridSurface.IHandler>(() => new GtkVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm(backend));
		}
	}
}
