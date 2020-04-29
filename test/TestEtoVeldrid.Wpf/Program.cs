using Eto.Forms;
using System;

namespace TestEtoVeldrid.Wpf
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var platform = new Eto.Wpf.Platform();

			new Application(platform).Run(new MainForm());
		}
	}
}
