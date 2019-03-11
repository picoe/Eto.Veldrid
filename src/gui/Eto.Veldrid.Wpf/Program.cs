using Eto.Drawing;
using Eto.Forms;
using Eto.Gl;
using Eto.Gl.WPF_WFControl;
using Eto.VeldridSurface;
using OpenTK;
using System;
using System.Runtime.InteropServices;
using Veldrid;

namespace PlaceholderName
{
	public class PuppetWPFWFGLSurfaceHandler : WPFWFGLSurfaceHandler
	{
		public override void AttachEvent(string id)
		{
			switch (id)
			{
				// Prevent the base surface handler class from attaching its own
				// internal event handler to these events; said handler calls
				// MakeCurrent, uses GL.Viewport, and swaps buffers. That's
				// undesirable here, so just attach the appropriate callback.
				case GLSurface.ShownEvent:
					break;
				case GLSurface.GLDrawEvent:
					WinFormsControl.Paint += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
					break;
				case GLSurface.SizeChangedEvent:
					WinFormsControl.SizeChanged += (sender, e) => Callback.OnSizeChanged(Widget, EventArgs.Empty);
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}
	}

	//public class WpfVeldridSurfaceHandler : ThemedContainerHandler<Panel, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IVeldridSurface
	//{
	//	public WpfVeldridSurfaceHandler()
	//	{
	//		Control = new Panel();
	//	}

	//	public void Skiboodle(bool fail)
	//	{
	//		int flumbus = 4;
	//	}
	//}

	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			GraphicsBackend backend = VeldridSurface.PreferredBackend;
			//backend = GraphicsBackend.OpenGL;
			//backend = GraphicsBackend.Direct3D11;
			if (backend == GraphicsBackend.OpenGL)
			{
				Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });
			}

			var platform = new Eto.Wpf.Platform();

			if (backend == GraphicsBackend.OpenGL)
			{
				platform.Add<GLSurface.IHandler>(() => new PuppetWPFWFGLSurfaceHandler());
			}

			//platform.Add<VeldridSurface.IVeldridSurface>(() => new WpfVeldridSurfaceHandler());


			var app = new Application(platform);
			var test = new VeldridSurface(WindowsInit, GraphicsBackend.Direct3D11);

			app.Run(new MainForm(WindowsInit, backend));
		}

		public static void WindowsInit(VeldridSurface surface, GraphicsBackend backend, Action draw)
		{
			// OpenGL initialization is technically platform-dependent, but it
			// happens by way of GLSurface, which for users of the class is
			// cross platform. See VeldridSurface for initialization details.
			if (backend == GraphicsBackend.Vulkan)
			{
				surface.GraphicsDevice = GraphicsDevice.CreateVulkan(new GraphicsDeviceOptions());
			}
			else if (backend == GraphicsBackend.Direct3D11)
			{
				surface.GraphicsDevice = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
			}
			else
			{
				string message;
				if (!Enum.IsDefined(typeof(GraphicsBackend), backend))
				{
					message = "Unrecognized backend!";
				}
				else
				{
					message = "Specified backend not supported on this platform!";
				}

				throw new ArgumentException(message);
			}

			var dummy = new WpfVeldridHost();
			dummy.Loaded += (sender, e) =>
			{
				var source = SwapchainSource.CreateWin32(
					dummy.Hwnd, Marshal.GetHINSTANCE(typeof(VeldridSurface).Module));
				surface.Swapchain = surface.GraphicsDevice.ResourceFactory.CreateSwapchain(
					new SwapchainDescription(source, 640, 480, null, false));
			};
			dummy.Draw = draw;

			surface.Content = WpfHelpers.ToEto(dummy);
		}
	}
}
