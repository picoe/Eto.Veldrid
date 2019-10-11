using Eto.Forms;
using Eto.Veldrid;
using Eto.Veldrid.Gtk;
using System;

namespace TestEtoVeldrid.Gtk
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var platform = new Eto.GtkSharp.Platform();
			platform.Add<VeldridSurface.IHandler>(() => new GtkVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm());
		}
	}
}
