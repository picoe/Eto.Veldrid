using Eto.Mac.Forms;
using Eto.Veldrid;
using Eto.Veldrid.Mac;
using System;
using Veldrid;
using MonoMac.CoreVideo;
using Eto.Drawing;

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
		CVDisplayLink _displayLink;
		Size? _newRenderSize;

		public Size RenderSize => Size.Round((SizeF)Widget.Size * Scale);

		float Scale => Widget.ParentWindow?.LogicalPixelSize ?? 1;

		public override NSView ContainerControl => Control;

		public override bool Enabled { get; set; }

		public MacVeldridSurfaceHandler()
		{
			Control = new MacVeldridView();

			Control.Draw += Control_Draw;
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

				var renderSize = RenderSize;
				swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
					new SwapchainDescription(
						source,
						(uint)renderSize.Width,
						(uint)renderSize.Height,
						Widget.GraphicsDeviceOptions.SwapchainDepthFormat,
						Widget.GraphicsDeviceOptions.SyncToVerticalBlank,
						Widget.GraphicsDeviceOptions.SwapchainSrgbFormat));
			}

			return swapchain;
		}

		private void Control_Draw(object sender, EventArgs e)
		{
			Callback.OnInitializeBackend(Widget, new InitializeEventArgs(RenderSize));

			if (Widget.Backend == GraphicsBackend.Metal)
			{
				_displayLink = new CVDisplayLink();
				_displayLink.SetOutputCallback(HandleDisplayLinkOutputCallback);
				_displayLink.Start();
			}

			Control.Draw -= Control_Draw;
			Widget.SizeChanged += Widget_SizeChanged;
		}

		private void Widget_SizeChanged(object sender, EventArgs e)
		{
			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				Callback.OnResize(Widget, new ResizeEventArgs(RenderSize));
			}
			else
			{
				_newRenderSize = RenderSize;
			}
		}

		private CVReturn HandleDisplayLinkOutputCallback(CVDisplayLink displayLink, ref CVTimeStamp inNow, ref CVTimeStamp inOutputTime, CVOptionFlags flagsIn, ref CVOptionFlags flagsOut)
		{
			if (_newRenderSize != null)
			{
				Callback.OnResize(Widget, new ResizeEventArgs(_newRenderSize.Value));
				_newRenderSize = null;
			}

			Callback.OnDraw(Widget, EventArgs.Empty);
			return CVReturn.Success;
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
					if (Widget.Backend == GraphicsBackend.OpenGL)
					{
						Control.Draw += (sender, e) => Callback.OnDraw(Widget, e);
					}
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}
	}
}
