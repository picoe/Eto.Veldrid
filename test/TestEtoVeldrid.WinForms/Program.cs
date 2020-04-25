using Eto.Forms;
using System;

namespace TestEtoVeldrid.WinForms
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var platform = new Eto.WinForms.Platform();

			new Application(platform).Run(new MainForm());
		}
	}
}
