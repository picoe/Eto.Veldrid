using Eto.Forms;
using Eto.Veldrid;
using System;

namespace TestEtoVeldrid.Gtk2
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			VeldridSurface.InitializeOpenTK();

			var platform = new Eto.GtkSharp.Platform();

			new Application(platform).Run(new MainForm());
		}
	}
}
