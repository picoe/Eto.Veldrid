using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Gtk;
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

			var platform = new Eto.GtkSharp.Platform();

			if (backend == GraphicsBackend.OpenGL)
			{
				platform.Add<GLSurface.IHandler>(() => new GtkGlSurfaceHandler());
			}

			var app = new Application(platform);

			var form = new MainForm(LinuxInit, backend);

			app.Run(form);
		}

		public static void LinuxInit(VeldridSurface surface, GraphicsBackend backend, Action draw, Action<int, int> resize)
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
	}
}
