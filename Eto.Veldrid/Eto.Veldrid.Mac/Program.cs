using System;
using Eto.Forms;
using Eto.VeldridSurface;

namespace Eto.Veldrid.Mac
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Eto.Platforms.Mac64).Run(new OpenGLForm());
		}
	}
}
