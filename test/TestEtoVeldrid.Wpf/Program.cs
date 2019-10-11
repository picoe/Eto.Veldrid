using Eto.Forms;
using Eto.Veldrid;
using Eto.Veldrid.Wpf;
using System;

namespace TestEtoVeldrid.Wpf
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var platform = new Eto.Wpf.Platform();
			platform.Add<VeldridSurface.IHandler>(() => new WpfVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm());
		}
	}
}
