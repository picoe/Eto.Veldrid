using Eto.Forms;
using System;

namespace Eto.Veldrid.Gtk
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Eto.Platforms.Gtk).Run(new OpenGLForm());
		}
	}
}
