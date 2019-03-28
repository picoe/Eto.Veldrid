using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Windows;
using Eto.VeldridSurface;
using OpenTK;
using System;
using System.Runtime.InteropServices;
using Veldrid;

namespace PlaceholderName
{
	public class PuppetWinGLSurfaceHandler : WinGLSurfaceHandler
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
					Control.Paint += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
					break;
				case GLSurface.SizeChangedEvent:
					Control.SizeChanged += (sender, e) => Callback.OnSizeChanged(Widget, EventArgs.Empty);
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}
	}

	public class WinFormsVeldridSurfaceHandler : VeldridSurfaceHandler
	{
		public override void InitializeGraphicsApi()
		{
			// OpenGL initialization is technically platform-dependent, but it
			// happens by way of GLSurface, which for users of the class is
			// cross platform. See VeldridSurface for initialization details.
			if (Callback.Backend == GraphicsBackend.Vulkan)
			{
				Callback.GraphicsDevice = GraphicsDevice.CreateVulkan(new GraphicsDeviceOptions());
			}
			else if (Callback.Backend == GraphicsBackend.Direct3D11)
			{
				Callback.GraphicsDevice = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
			}
			else
			{
				string message;
				if (!Enum.IsDefined(typeof(GraphicsBackend), Callback.Backend))
				{
					message = "Unrecognized backend!";
				}
				else
				{
					message = "Specified backend not supported on this platform!";
				}

				throw new ArgumentException(message);
			}

			var source = SwapchainSource.CreateWin32(
				Control.NativeHandle, Marshal.GetHINSTANCE(typeof(VeldridSurface).Module));
			Callback.Swapchain = Callback.GraphicsDevice.ResourceFactory.CreateSwapchain(
				new SwapchainDescription(source, (uint)Widget.Width, (uint)Widget.Height, null, false));
		}
	}

	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			GraphicsBackend backend = VeldridSurface.PreferredBackend;

			if (backend == GraphicsBackend.OpenGL)
			{
				Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });
			}

			var platform = new Eto.WinForms.Platform();

			if (backend == GraphicsBackend.OpenGL)
			{
				platform.Add<GLSurface.IHandler>(() => new PuppetWinGLSurfaceHandler());
			}

			platform.Add<VeldridSurface.IHandler>(() => new WinFormsVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm(backend));
		}
	}
}
