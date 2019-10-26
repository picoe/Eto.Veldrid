using Eto.Mac.Forms;
using Eto.Veldrid;
using Eto.Veldrid.Mac;
using System;
using Veldrid;
using OpenTK.Graphics;
using OpenTK.Platform;

#if MONOMAC
using MonoMac.AppKit;
#elif XAMMAC2
using AppKit;
#endif

[assembly: Eto.ExportHandler(typeof(VeldridSurface), typeof(MacVeldridSurfaceHandler))]

namespace Eto.Veldrid.Mac
{
	public class MacVeldridSurfaceHandler : MacView<MacVeldridView, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		// TODO: Set up some way to test HiDPI in macOS and figure out how to
		// get the right values here.
		public int RenderWidth => Widget.Width;
		public int RenderHeight => Widget.Height;

		public IWindowInfo WindowInfo => Control.WindowInfo;

		public override NSView ContainerControl => Control;

		public override bool Enabled { get; set; }

		public Action<uint, uint> ResizeSwapchain { get; protected set; }

		public MacVeldridSurfaceHandler()
		{
			Control = new MacVeldridView();

			Control.Draw += Control_Draw;
			Control.WindowInfoUpdated += (sender, e) => Callback.OnWindowInfoUpdated(Widget, EventArgs.Empty);

			ResizeSwapchain = (w, h) => Control.UpdateWindowInfo();
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
				var source = SwapchainSource.CreateNSView(Control.Handle);

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

		public IWindowInfo UpdateWindowInfo(GraphicsMode mode) => Control.UpdateWindowInfo(mode);

		private void Control_Draw(object sender, EventArgs e)
		{
			Callback.InitializeGraphicsBackend(Widget);

			Control.Draw -= Control_Draw;
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
					Control.Draw += (sender, e) => Callback.OnDraw(Widget, e);
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}
	}
}
