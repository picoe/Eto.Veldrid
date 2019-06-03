using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Windows;
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

	public class WinFormsRenderTargetHandler : Eto.WinForms.Forms.Controls.ControlHandler, RenderTarget.IHandler
	{
		public IntPtr IntegrationHandle => Control.Handle;
	}

	public class WinFormsVeldridSurfaceHandler : Eto.WinForms.Forms.Controls.PanelHandler, VeldridSurface.IHandler
	{
		public new VeldridSurface.ICallback Callback => (VeldridSurface.ICallback)base.Callback;
		public new VeldridSurface Widget => (VeldridSurface)base.Widget;

		public void InitializeOtherApi()
		{
			var target = new RenderTarget();
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
			platform.Add<GLSurface.IHandler>(() => new PuppetWinGLSurfaceHandler());
			platform.Add<VeldridSurface.IHandler>(() => new WinFormsVeldridSurfaceHandler());
			platform.Add<RenderTarget.IHandler>(() => new WinFormsRenderTargetHandler());

			new Application(platform).Run(new MainForm(backend));
		}
	}
}
