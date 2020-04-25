using Eto.Veldrid;
using Eto.Veldrid.Wpf;
using System;
using System.Runtime.InteropServices;
using Veldrid;

[assembly: Eto.ExportHandler(typeof(VeldridSurface), typeof(WpfVeldridSurfaceHandler))]

namespace Eto.Veldrid.Wpf
{
	public class WpfVeldridSurfaceHandler : Eto.Wpf.Forms.ManualBubbleWindowsFormsHostHandler<WinForms.WinFormsVeldridUserControl, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		public int RenderWidth => WinFormsControl.Width;
		public int RenderHeight => WinFormsControl.Height;

		public WpfVeldridSurfaceHandler() : base(new WinForms.WinFormsVeldridUserControl())
		{
			Control.Loaded += Control_Loaded;
		}

		public Swapchain CreateSwapchain()
		{
			Swapchain swapchain;

			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				swapchain = Widget.GraphicsDevice.MainSwapchain;
			}
			else
			{
				// To embed Veldrid in an Eto control, these platform-specific
				// versions of CreateSwapchain use the technique outlined here:
				//
				//   https://github.com/mellinoe/veldrid/issues/155
				//
				var source = SwapchainSource.CreateWin32(
					WinFormsControl.Handle,
					Marshal.GetHINSTANCE(typeof(VeldridSurface).Module));

				swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
					new SwapchainDescription(
						source,
						(uint)RenderWidth,
						(uint)RenderHeight,
						Widget.GraphicsDeviceOptions.SwapchainDepthFormat,
						Widget.GraphicsDeviceOptions.SyncToVerticalBlank,
						Widget.GraphicsDeviceOptions.SwapchainSrgbFormat));
			}

			return swapchain;
		}

		private void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			Callback.InitializeGraphicsBackend(Widget);

			Control.Loaded -= Control_Loaded;
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
					WinFormsControl.Paint += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}
	}
}
