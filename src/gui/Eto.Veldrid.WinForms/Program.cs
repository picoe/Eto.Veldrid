using Eto.WinForms.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Veldrid;
using Veldrid.OpenGL;

namespace PlaceholderName
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

		public new VeldridSurface.ICallback Callback => (VeldridSurface.ICallback)base.Callback;
		public new VeldridSurface Widget => (VeldridSurface)base.Widget;

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

				Callback.InitializeOpenGL(Widget);
			}
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
				(uint)Widget.Width,
				(uint)Widget.Height);

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
			platform.Add<VeldridSurface.IHandler>(() => new WinFormsVeldridSurfaceHandler());

			new Eto.Forms.Application(platform).Run(new MainForm(backend));
		}
	}
}
