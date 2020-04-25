using Eto.Forms;
using Eto.Veldrid;
using System;

namespace TestEtoVeldrid.Gtk
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var platform = new Eto.GtkSharp.Platform();

			new Application(platform).Run(new MainForm());
		}
	}
}
