using System;
using Eto.Forms;
using Eto.VeldridSurface;

namespace PlaceholderName
{
	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Eto.Platforms.Wpf).Run(new OpenGLForm());
		}
	}
}
