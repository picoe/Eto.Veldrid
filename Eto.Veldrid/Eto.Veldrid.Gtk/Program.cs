using System;
using Eto.Forms;
using Eto.VeldridSurface;

namespace Eto.Veldrid.Gtk
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Eto.Platforms.Gtk).Run(new OpenGLForm());
		}
	}
}
