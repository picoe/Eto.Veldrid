using Eto.Veldrid;
using Eto.Veldrid.WinForms;
using Eto.WinForms.Forms;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Veldrid;
using Veldrid.OpenGL;

[assembly: Eto.ExportHandler(typeof(VeldridSurface), typeof(WinFormsVeldridSurfaceHandler))]

namespace Eto.Veldrid.WinForms
{
	public class WinVeldridUserControl : UserControl
	{
		GraphicsMode Mode = new GraphicsMode(new ColorFormat(32), 8, 8);

		public IWindowInfo WindowInfo { get; set; }
		public GraphicsContext Context { get; private set; }

		protected override CreateParams CreateParams
		{
			get
			{
				const int
					CS_VREDRAW = 0x01,
					CS_HREDRAW = 0x02,
					CS_OWNDC = 0x20;

				CreateParams cp = base.CreateParams;

				if (OpenTK.Configuration.RunningOnWindows)
				{
					// Necessary for OpenGL in Windows.
					cp.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;
				}

				return cp;
			}
		}

		public WinVeldridUserControl()
		{
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			DoubleBuffered = false;

			BackColor = System.Drawing.Color.HotPink;
		}

		public void CreateOpenGLContext()
		{
			WindowInfo = Utilities.CreateWindowsWindowInfo(Handle);

			Context = new GraphicsContext(Mode, WindowInfo, 3, 3, GraphicsContextFlags.ForwardCompatible);
		}

		public void MakeCurrent(IntPtr context)
		{
			Context.MakeCurrent(WindowInfo);
		}
	}

	public class WinFormsVeldridSurfaceHandler : WindowsControl<WinVeldridUserControl, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		public int RenderWidth => Control.Width;
		public int RenderHeight => Control.Height;

		public WinFormsVeldridSurfaceHandler()
		{
			Control = new WinVeldridUserControl();

			Control.HandleCreated += Control_HandleCreated;
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
					Control.Paint += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}

		private void Control_HandleCreated(object sender, EventArgs e)
		{
			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				Control.CreateOpenGLContext();

				Callback.OnOpenGLReady(Widget, EventArgs.Empty);
			}

			Callback.OnControlReady(Widget, EventArgs.Empty);
		}

		/// <summary>
		/// Prepare this VeldridSurface to use OpenGL.
		/// </summary>
		public void InitializeOpenGL()
		{
			var platformInfo = new OpenGLPlatformInfo(
				VeldridGL.GetGLContextHandle(),
				VeldridGL.GetProcAddress,
				Control.MakeCurrent,
				VeldridGL.GetCurrentContext,
				VeldridGL.ClearCurrentContext,
				VeldridGL.DeleteContext,
				VeldridGL.SwapBuffers,
				VeldridGL.SetVSync,
				VeldridGL.SetSwapchainFramebuffer,
				VeldridGL.ResizeSwapchain);

			Widget.GraphicsDevice = GraphicsDevice.CreateOpenGL(
				Widget.GraphicsDeviceOptions,
				platformInfo,
				(uint)RenderWidth,
				(uint)RenderHeight);

			Widget.Swapchain = Widget.GraphicsDevice.MainSwapchain;

			Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
		}

		public void InitializeOtherApi()
		{
			// To embed Veldrid in an Eto control, all these platform-specific
			// versions of InitializeOtherApi use the technique outlined here:
			//
			//   https://github.com/mellinoe/veldrid/issues/155
			//
			var source = SwapchainSource.CreateWin32(
				Control.Handle,
				Marshal.GetHINSTANCE(typeof(VeldridSurface).Module));

			Widget.Swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
				new SwapchainDescription(
					source,
					(uint)RenderWidth,
					(uint)RenderHeight,
					Widget.GraphicsDeviceOptions.SwapchainDepthFormat,
					Widget.GraphicsDeviceOptions.SyncToVerticalBlank,
					Widget.GraphicsDeviceOptions.SwapchainSrgbFormat));

			Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
		}
	}
}
