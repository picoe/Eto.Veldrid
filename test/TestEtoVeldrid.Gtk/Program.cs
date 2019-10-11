using Eto.Veldrid;
using Eto.Veldrid.Gtk;
using OpenTK;
using System;
using Veldrid;

namespace TestEtoVeldrid.Gtk
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
			platform.Add<VeldridSurface.IHandler>(() => new GtkVeldridSurfaceHandler());

			new Eto.Forms.Application(platform).Run(new MainForm(backend));
		}
	}
}
