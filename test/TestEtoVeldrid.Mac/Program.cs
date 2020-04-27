using Eto;
using Eto.Forms;
using Eto.Veldrid;
using System;
using System.Diagnostics;
using System.IO;

namespace TestEtoVeldrid.Mac
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			//VeldridSurface.InitializeOpenTK();

			var platform = new Eto.Mac.Platform();

			// FIXME: This seems to be necessary in order for Mac Release builds
			// to run when double-clicked from Finder. Running the executable
			// from a Terminal works without this. I suspect it has something to
			// do with the use of mkbundle, and whatever effect that has on
			// loading handlers exported from assemblies with Eto.ExportHandler.
			platform.Add<VeldridSurface.IHandler>(() => new Eto.Veldrid.Mac.MacVeldridSurfaceHandler());

			// AppContext.BaseDirectory is too simple for the case of the Mac
			// projects. When building an app bundle that depends on the Mono
			// framework being installed, it properly returns the path of the
			// executable in Eto.Veldrid.app/Contents/MacOS. When building an
			// app bundle that instead bundles Mono by way of mkbundle, on the
			// other hand, it returns the directory containing the .app..
			new Application(platform).Run(new MainForm(
				Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
				Path.Combine(EtoEnvironment.GetFolderPath(EtoSpecialFolder.ApplicationResources), "shaders")));
		}
	}
}
