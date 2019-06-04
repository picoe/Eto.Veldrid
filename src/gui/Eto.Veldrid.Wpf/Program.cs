using Eto.Forms;
using Eto.Gl;
using Eto.Gl.WPF_WFControl;
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

	public class WpfRenderTargetHandler : Eto.Wpf.Forms.WindowsFormsHostHandler<System.Windows.Forms.Panel, RenderTarget, RenderTarget.ICallback>, RenderTarget.IHandler
	{
		public IntPtr IntegrationHandle => WinFormsControl.Handle;

		public WpfRenderTargetHandler() : base(new System.Windows.Forms.Panel())
		{
			Control.Focusable = true;

			Control.KeyDown += Control_KeyDown;
			Control.MouseDown += Control_MouseDown;

			WinFormsControl.KeyDown += WinFormsControl_KeyDown;
			WinFormsControl.MouseDown += WinFormsControl_MouseDown;
		}

		// Simple test handlers to demonstrate the odd control flow.
		//
		// WinForms mouse click (works as expected):
		//   RenderTarget.OnMouseDown
		//   VeldridSurface.OnMouseDown
		//
		// WPF mouse click (almost works, but event gets swallowed somewhere):
		//   WpfRenderTargetHandler.WinFormsControl_MouseDown
		//   RenderTarget.OnMouseDown
		//   ? (should bubble up to the parent VeldridSurface.OnMouseDown)
		//
		// WinForms key press (works as expected):
		//   RenderTarget.OnKeyDown
		//   VeldridSurface.OnKeyDown
		//
		// WPF key press (works, but never passes through RenderTarget for some reason):
		//   WpfRenderTargetHandler.Control_KeyDown (not WinFormsControl_KeyDown)
		//   ? (skips RenderTarget.OnKeyDown)
		//   VeldridSurface.OnKeyDown
		//
		// With Control.Focusable set to false instead, the MouseDown event
		// behaves the same way, making it to RenderTarget.OnMouseDown before
		// vanishing without making it up to VeldridSurface.OnMouseDown; in
		// addition, KeyDown events stop working altogether.

		private void Control_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}
		private void Control_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}

		private void WinFormsControl_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}
		private void WinFormsControl_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}

		public override void OnPreLoad(EventArgs e)
		{
			base.OnPreLoad(e);

			WinFormsControl.Width = Widget.Width;
			WinFormsControl.Height = Widget.Height;
		}
	}

	public class WpfVeldridSurfaceHandler : Eto.Wpf.Forms.Controls.PanelHandler, VeldridSurface.IHandler
	{
		public new VeldridSurface.ICallback Callback => (VeldridSurface.ICallback)base.Callback;
		public new VeldridSurface Widget => (VeldridSurface)base.Widget;

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
				var target = new RenderTarget { Size = Widget.Size };
				Widget.Content = target;

				// To embed Veldrid in an Eto control, all these platform-specific
				// versions of InitializeOtherApi use the technique outlined here:
				//
				//   https://github.com/mellinoe/veldrid/issues/155
				//
				var source = SwapchainSource.CreateWin32(
					target.Handler.IntegrationHandle,
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
			platform.Add<RenderTarget.IHandler>(() => new WpfRenderTargetHandler());
			platform.Add<VeldridSurface.IHandler>(() => new WpfVeldridSurfaceHandler());

			new Application(platform).Run(new MainForm(backend));
		}
	}
}
