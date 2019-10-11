using Eto.Forms;
using Eto.Veldrid;
using Eto.Veldrid.WinForms;
using System;

namespace TestEtoVeldrid.WinForms
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var platform = new Eto.WinForms.Platform();
			platform.Add<VeldridSurface.IHandler>(() => new WinFormsVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm());
		}
	}
}
