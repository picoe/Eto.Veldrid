using Eto.Forms;
using OpenTK;
using System;
using System.Diagnostics;
using System.IO;
using Veldrid;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
#elif XAMMAC2
using AppKit;
using CoreGraphics;
#endif

namespace PlaceholderName
{
	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			GraphicsBackend backend = VeldridSurface.PreferredBackend;

			if (backend == GraphicsBackend.OpenGL)
			{
				Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });
			}

			var platform = new Eto.Mac.Platform();
			platform.Add<VeldridSurface.IHandler>(() => new MacVeldridSurfaceHandler());

			// AppContext.BaseDirectory is too simple for the case of the Mac
			// projects. When building an app bundle that depends on the Mono
			// framework being installed, it properly returns the path of the
			// executable in Eto.Veldrid.app/Contents/MacOS. When building an
			// app bundle that instead bundles Mono by way of mkbundle, on the
			// other hand, it returns the directory containing the .app..
			new Application(platform).Run(new MainForm(
				backend,
				Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
				Path.Combine("..", "Resources", "shaders")));
		}
	}
}
