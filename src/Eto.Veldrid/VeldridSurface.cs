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
			int RenderWidth { get; }
			int RenderHeight { get; }

			Swapchain CreateSwapchain();
		}

		public new IHandler Handler => (IHandler)base.Handler;

		public new interface ICallback : Control.ICallback
		{
			void InitializeGraphicsBackend(VeldridSurface s);
			void OnDraw(VeldridSurface s, EventArgs e);
			void OnResize(VeldridSurface s, ResizeEventArgs e);
			void OnVeldridInitialized(VeldridSurface s, EventArgs e);
		}

		protected new class Callback : Control.Callback, ICallback
		{
			public void InitializeGraphicsBackend(VeldridSurface s) => s?.InitializeGraphicsBackend();
			public void OnDraw(VeldridSurface s, EventArgs e) => s?.OnDraw(e);
			public void OnResize(VeldridSurface s, ResizeEventArgs e) => s?.OnResize(e);
			public void OnVeldridInitialized(VeldridSurface s, EventArgs e) => s?.OnVeldridInitialized(e);
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
		/// The render area's width, which may differ from the control's width
		/// (e.g. with high DPI displays).
		/// </summary>
		public int RenderWidth => Handler.RenderWidth;
		/// <summary>
		/// The render area's height, which may differ from the control's height
		/// (e.g. with high DPI displays).
		/// </summary>
		public int RenderHeight => Handler.RenderHeight;

		public GraphicsBackend Backend { get; set; } = PreferredBackend;
		public GraphicsDevice GraphicsDevice { get; set; }
		public GraphicsDeviceOptions GraphicsDeviceOptions { get; private set; } = new GraphicsDeviceOptions();
		public Swapchain Swapchain { get; set; }

		public const string VeldridInitializedEvent = "VeldridSurface.VeldridInitialized";
		public const string DrawEvent = "VeldridSurface.Draw";
		public const string ResizeEvent = "VeldridSurface.Resize";

		public event EventHandler<EventArgs> VeldridInitialized
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

		private void InitializeGraphicsBackend()
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
						(uint)RenderWidth,
						(uint)RenderHeight);
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

			OnVeldridInitialized(EventArgs.Empty);
		}

		protected virtual void OnDraw(EventArgs e)
		{
			if (_resizeEvent != null)
			{
				OnResize(_resizeEvent);
				_resizeEvent = null;
			}

			Properties.TriggerEvent(DrawEvent, this, e);
		}

		protected virtual void OnResize(ResizeEventArgs e)
		{
			if (Swapchain != null && e != null)
			{
				Swapchain.Resize((uint)e.Width, (uint)e.Height);
			}

			Properties.TriggerEvent(ResizeEvent, this, e);
		}

		ResizeEventArgs _resizeEvent;

		protected virtual void OnVeldridInitialized(EventArgs e) => Properties.TriggerEvent(VeldridInitializedEvent, this, e);

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (!Loaded)
			{
				return;
			}

			_resizeEvent = new ResizeEventArgs(RenderWidth, RenderHeight);
		}
	}
}
