using Eto.Forms;
using Eto.Veldrid;
using System;

namespace TestEtoVeldrid.Wpf
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			VeldridSurface.InitializeOpenTK();

			var platform = new Eto.Wpf.Platform();

			new Application(platform).Run(new MainForm());
		}
	}
}
