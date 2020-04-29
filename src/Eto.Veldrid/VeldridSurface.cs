using Eto.Drawing;
using Eto.Forms;
using System;
using Veldrid;
using Veldrid.OpenGL;

namespace Eto.Veldrid
{
	/// <summary>
	/// A simple control that allows drawing with Veldrid.
	/// </summary>
	[Handler(typeof(VeldridSurface.IHandler))]
	public class VeldridSurface : Control
	{
		public new interface IHandler : Control.IHandler
		{
			Size RenderSize { get; }
			Swapchain CreateSwapchain();
		}

		new IHandler Handler => (IHandler)base.Handler;

		public new interface ICallback : Control.ICallback
		{
			void OnInitializeBackend(VeldridSurface s, InitializeEventArgs e);
			void OnDraw(VeldridSurface s, EventArgs e);
			void OnResize(VeldridSurface s, ResizeEventArgs e);
		}

		protected new class Callback : Control.Callback, ICallback
		{
			public void OnInitializeBackend(VeldridSurface s, InitializeEventArgs e) => s?.InitializeGraphicsBackend(e);
			public void OnDraw(VeldridSurface s, EventArgs e) => s?.OnDraw(e);
			public void OnResize(VeldridSurface s, ResizeEventArgs e) => s?.OnResize(e);
		}

		protected override object GetCallback() => new Callback();

		public interface IOpenGL
		{
			IntPtr OpenGLContextHandle { get; }
			IntPtr GetProcAddress(string name);
			void MakeCurrent(IntPtr context);
			IntPtr GetCurrentContext();
			void ClearCurrentContext();
			void DeleteContext(IntPtr context);
			void SwapBuffers();
			void SetSyncToVerticalBlank(bool enable);
			void SetSwapchainFramebuffer();
			void ResizeSwapchain(uint width, uint height);
		}

		public IOpenGL OpenGL => (IOpenGL)Handler;

		public static GraphicsBackend PreferredBackend { get; } = GetPreferredBackend();

		/// <summary>
		/// The render area's size, which may differ from the control's size
		/// (e.g. with high DPI displays).
		/// </summary>
		public Size RenderSize => Handler.RenderSize;
		/// <summary>
		/// The render area's width, which may differ from the control's width
		/// (e.g. with high DPI displays).
		/// </summary>
		public int RenderWidth => RenderSize.Width;
		/// <summary>
		/// The render area's height, which may differ from the control's height
		/// (e.g. with high DPI displays).
		/// </summary>
		public int RenderHeight => RenderSize.Height;

		public GraphicsBackend Backend { get; private set; }
		public GraphicsDevice GraphicsDevice { get; private set; }
		public GraphicsDeviceOptions GraphicsDeviceOptions { get; private set; } = new GraphicsDeviceOptions();
		public Swapchain Swapchain { get; private set; }

		public const string VeldridInitializedEvent = "VeldridSurface.VeldridInitialized";
		public const string DrawEvent = "VeldridSurface.Draw";
		public const string ResizeEvent = "VeldridSurface.Resize";

		public event EventHandler<InitializeEventArgs> VeldridInitialized
		{
			add { Properties.AddHandlerEvent(VeldridInitializedEvent, value); }
			remove { Properties.RemoveEvent(VeldridInitializedEvent, value); }
		}
		public event EventHandler<EventArgs> Draw
		{
			add { Properties.AddHandlerEvent(DrawEvent, value); }
			remove { Properties.RemoveEvent(DrawEvent, value); }
		}
		public event EventHandler<ResizeEventArgs> Resize
		{
			add { Properties.AddHandlerEvent(ResizeEvent, value); }
			remove { Properties.RemoveEvent(ResizeEvent, value); }
		}

		public VeldridSurface()
			: this(PreferredBackend)
		{
		}
		public VeldridSurface(GraphicsBackend backend)
		{
			Backend = backend;
		}
		public VeldridSurface(GraphicsBackend backend, GraphicsDeviceOptions gdOptions)
		{
			Backend = backend;
			GraphicsDeviceOptions = gdOptions;
		}

		private static GraphicsBackend GetPreferredBackend()
		{
			GraphicsBackend? backend = null;

			if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal))
			{
				backend = GraphicsBackend.Metal;
			}
			else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11))
			{
				backend = GraphicsBackend.Direct3D11;
			}
			else if (EtoEnvironment.Platform.IsLinux && GraphicsDevice.IsBackendSupported(GraphicsBackend.OpenGL))
			{
				backend = GraphicsBackend.OpenGL;
			}

			if (backend == null)
			{
				throw new VeldridException("VeldridSurface: No supported Veldrid backend found!");
			}

			return (GraphicsBackend)backend;
		}

		private void InitializeGraphicsBackend(InitializeEventArgs e)
		{
			switch (Backend)
			{
				case GraphicsBackend.Metal:
					GraphicsDevice = GraphicsDevice.CreateMetal(GraphicsDeviceOptions);
					break;
				case GraphicsBackend.Direct3D11:
					GraphicsDevice = GraphicsDevice.CreateD3D11(GraphicsDeviceOptions);
					break;
				case GraphicsBackend.Vulkan:
					GraphicsDevice = GraphicsDevice.CreateVulkan(GraphicsDeviceOptions);
					break;
				case GraphicsBackend.OpenGL:
					GraphicsDevice = GraphicsDevice.CreateOpenGL(
						GraphicsDeviceOptions,
						new OpenGLPlatformInfo(
							OpenGL.OpenGLContextHandle,
							OpenGL.GetProcAddress,
							OpenGL.MakeCurrent,
							OpenGL.GetCurrentContext,
							OpenGL.ClearCurrentContext,
							OpenGL.DeleteContext,
							OpenGL.SwapBuffers,
							OpenGL.SetSyncToVerticalBlank,
							OpenGL.SetSwapchainFramebuffer,
							OpenGL.ResizeSwapchain),
						(uint)e.Width,
						(uint)e.Height);
					break;
				default:
					string message;
					if (!Enum.IsDefined(typeof(GraphicsBackend), Backend))
					{
						message = "Unrecognized backend!";
					}
					else
					{
						message = "Specified backend not supported on this platform!";
					}

					throw new ArgumentException(message);
			}

			Swapchain = Handler.CreateSwapchain();

			OnVeldridInitialized(e);
		}

		protected virtual void OnDraw(EventArgs e) => Properties.TriggerEvent(DrawEvent, this, e);

		protected virtual void OnResize(ResizeEventArgs e)
		{
			if (e == null)
				throw new ArgumentNullException(nameof(e));

			Swapchain?.Resize((uint)e.Width, (uint)e.Height);

			Properties.TriggerEvent(ResizeEvent, this, e);
		}

		protected virtual void OnVeldridInitialized(InitializeEventArgs e) => Properties.TriggerEvent(VeldridInitializedEvent, this, e);
	}
}
