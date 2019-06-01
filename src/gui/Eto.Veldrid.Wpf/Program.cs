using Eto.Forms;
using Eto.Gl;
using Eto.Gl.WPF_WFControl;
using OpenTK;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms.Integration;
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

	public class WpfVeldridSurfaceHandler : Eto.Wpf.Forms.Controls.PanelHandler, VeldridSurface.IHandler
	{
		public new VeldridSurface.ICallback Callback => (VeldridSurface.ICallback)base.Callback;
		public new VeldridSurface Widget => (VeldridSurface)base.Widget;

		// Provides a WinForms child control, from which a native handle can be
		// obtained for Veldrid integration purposes. WPF otherwise doesn't use
		// native handles, and asking for one from Eto returns only a handle for
		// the top level window client area.
		private WindowsFormsHost Host { get; } = new WindowsFormsHost
		{
			Child = new System.Windows.Forms.Panel()
		};

		// TODO: There's some sort of issue here; with this commented out, the
		// application window can be resized with no flickering, but terrible
		// performance as it enlarges. With this constructor uncommented, the
		// performance improves, but the view flickers during any resize.
		//public WpfVeldridSurfaceHandler()
		//{
		//	Control = new System.Windows.Controls.Border();
		//}

		/// <summary>
		/// Prepare this VeldridSurface to use a graphics API other than OpenGL.
		/// </summary>
		public void InitializeOtherApi()
		{
			Control.Loaded += (sender, e) =>
			{
				Host.Child.Width = Widget.Width;
				Host.Child.Height = Widget.Height;

				Content = Host.ToEto();

				// To embed Veldrid in an Eto control, all these platform-specific
				// versions of InitializeOtherApi use the technique outlined here:
				//
				//   https://github.com/mellinoe/veldrid/issues/155
				//
				var source = SwapchainSource.CreateWin32(
					Host.Child.Handle,
					Marshal.GetHINSTANCE(typeof(VeldridSurface).Module));

				Widget.Swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
					new SwapchainDescription(
						source,
						(uint)Widget.Width,
						(uint)Widget.Height,
						PixelFormat.R32_Float,
						false));

				Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
			};
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
					Host.Child.Paint += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
					break;
				case VeldridSurface.ResizeEvent:
					Host.Child.SizeChanged += (sender, e) => Callback.OnResize(Widget, new ResizeEventArgs(Widget.Size));
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}
	}

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

			var platform = new Eto.Wpf.Platform();
			platform.Add<GLSurface.IHandler>(() => new PuppetWPFWFGLSurfaceHandler());
			platform.Add<VeldridSurface.IHandler>(() => new WpfVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm(backend));
		}
	}
}
