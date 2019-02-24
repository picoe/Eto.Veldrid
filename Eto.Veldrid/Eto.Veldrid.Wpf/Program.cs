using Eto.Forms;
using System;

namespace PlaceholderName
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			new Application(Eto.Platforms.Wpf).Run(new OpenGLForm());
		}
	}
}
