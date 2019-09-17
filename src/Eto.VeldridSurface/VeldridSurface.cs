using Eto;
using Eto.Drawing;
using Eto.Forms;
using OpenTK.Graphics;
using System;
using System.Reflection;
using Veldrid;

// TODO: Come up with a suitable namespace. Eto.Veldrid will conflict with the
// global Veldrid, and Eto.VeldridSurface makes the VeldridSurface class harder
// to use. Suggestions welcome!
namespace PlaceholderName
{
	/// <summary>
	/// A collection of helper methods to grant Veldrid access to an OpenGL context.
	/// </summary>
	/// <remarks>
	/// These are potentially dangerous methods, so please don't try to use them
	/// for your own purposes. Hand them off to Veldrid as VeldridSurfaceHandler
	/// does in InitializeOpenGL, and forget about them.
	/// </remarks>
	public static class VeldridGL
	{
		public static IntPtr GetGLContextHandle()
		{
			return GetCurrentContext();
		}

		// It should be noted that both GetProcAddress and MakeCurrent aren't
		// exactly following best practices here; they depend on a private
		// method and field, respectively, to grant Veldrid access to OpenTK's
		// internals. It's not pretty, but since this Eto integration was
		// designed specifically with OpenTK 3.0.1 in mind, sticking with that
		// version will guarantee that anything private will stay where it is.

		public static IntPtr GetProcAddress(string name)
		{
			Type type = typeof(OpenTK.Platform.Utilities);

			MethodInfo createGetAddress = type.GetMethod("CreateGetAddress", BindingFlags.NonPublic | BindingFlags.Static);
			var getAddress = (GraphicsContext.GetAddressDelegate)createGetAddress.Invoke(null, Array.Empty<string>());

			return getAddress.Invoke(name);
		}

		public static IntPtr GetCurrentContext()
		{
			return GraphicsContext.CurrentContextHandle.Handle;
		}

		public static void ClearCurrentContext()
		{
			GraphicsContext.CurrentContext.MakeCurrent(null);
		}

		public static void DeleteContext(IntPtr context)
		{
			// Do nothing! With this Eto.Gl-based approach, Veldrid should never
			// need to destroy an OpenGL context on its own; let the GLSurface
			// take care of context deletion upon its own disposal.
		}

		public static void SwapBuffers()
		{
			GraphicsContext.CurrentContext.SwapBuffers();
		}

		public static void SetVSync(bool on)
		{
			GraphicsContext.CurrentContext.SwapInterval = on ? 1 : 0;
		}

		// It's perfectly acceptable to create an instance of OpenGLPlatformInfo
		// without providing these last two methods, if indeed you don't need
		// them. They're stubbed out here only to serve as a reminder that they
		// can be customized should the occasion call for it.

		public static void SetSwapchainFramebuffer()
		{
		}

		public static void ResizeSwapchain(uint width, uint height)
		{
		}
	}

	public class ResizeEventArgs : EventArgs
	{
		public int Width { get; set; }
		public int Height { get; set; }

		public ResizeEventArgs()
		{
		}
		public ResizeEventArgs(int width, int height)
		{
			Width = width;
			Height = height;
		}
		public ResizeEventArgs(Size size)
		{
			Width = size.Width;
			Height = size.Height;
		}
	}

	/// <summary>
	/// A simple control that allows drawing with Veldrid.
	/// </summary>
	[Handler(typeof(VeldridSurface.IHandler))]
	public class VeldridSurface : Control
	{
		public new interface IHandler : Control.IHandler
		{
			void InitializeOpenGL();
			void InitializeOtherApi();
		}

		public new IHandler Handler => (IHandler)base.Handler;

		public new interface ICallback : Control.ICallback
		{
			void InitializeOpenGL(VeldridSurface s);

			void OnDraw(VeldridSurface s, EventArgs e);
			void OnResize(VeldridSurface s, ResizeEventArgs e);
			void OnVeldridInitialized(VeldridSurface s, EventArgs e);
		}

		protected new class Callback : Control.Callback, ICallback
		{
			public void InitializeOpenGL(VeldridSurface s)
			{
				s.GLReady = true;
			}

			public void OnDraw(VeldridSurface s, EventArgs e)
			{
				s.OnDraw(e);
			}

			public void OnResize(VeldridSurface s, ResizeEventArgs e)
			{
				s.OnResize(e);
			}

			public void OnVeldridInitialized(VeldridSurface s, EventArgs e)
			{
				s.OnVeldridInitialized(e);
			}
		}

		protected override object GetCallback()
		{
			return new Callback();
		}

		// A depth buffer isn't strictly necessary for this project, which uses
		// only 2D vertex coordinates, but it's helpful to create one for the
		// sake of demonstration.
		//
		// The "improved" resource binding model changes how resource slots are
		// assigned in the Metal backend, allowing it to work like the others,
		// so the numbers used in calls to CommandList.SetGraphicsResourceSet
		// will make more sense to developers used to e.g. OpenGL or Direct3D.
		public GraphicsDeviceOptions GraphicsDeviceOptions { get; } =
			new GraphicsDeviceOptions(
				false,
				Veldrid.PixelFormat.R32_Float,
				false,
				ResourceBindingModel.Improved);

		public static GraphicsBackend PreferredBackend
		{
			get
			{
				GraphicsBackend backend;
				if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal))
				{
					backend = GraphicsBackend.Metal;
				}
				else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan))
				{
					backend = GraphicsBackend.Vulkan;
				}
				else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11))
				{
					backend = GraphicsBackend.Direct3D11;
				}
				else
				{
					backend = GraphicsBackend.OpenGL;
				}

				return backend;
			}
		}

		public GraphicsBackend Backend { get; set; }

		public GraphicsDevice GraphicsDevice { get; set; }
		public Swapchain Swapchain { get; set; }

		private bool? _glReady = null;
		public bool? GLReady
		{
			get { return _glReady; }
			private set
			{
				_glReady = value;

				InitializeGraphicsApi();
			}
		}

		private bool _controlReady = false;
		public bool ControlReady
		{
			get { return _controlReady; }
			private set
			{
				_controlReady = value;

				InitializeGraphicsApi();
			}
		}

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
		public event EventHandler<EventArgs> Resize
		{
			add { Properties.AddHandlerEvent(ResizeEvent, value); }
			remove { Properties.RemoveEvent(ResizeEvent, value); }
		}

		public VeldridSurface() : this(PreferredBackend)
		{
		}
		public VeldridSurface(GraphicsBackend backend)
		{
			Backend = backend;

			if (Backend == GraphicsBackend.OpenGL)
			{
				GLReady = false;
			}

			LoadComplete += (sender, e) => ControlReady = true;
		}

		public void InitializeGraphicsApi()
		{
			if (!ControlReady)
			{
				return;
			}

			switch (GLReady)
			{
				case false:
					return;
				case true:
					Handler.InitializeOpenGL();
					break;
				case null:
					InitializeOtherApi();
					break;
			}

			// Ideally Callback.OnVeldridInitialized would be called here, but
			// WPF needs to delay raising that event until after its control has
			// been Loaded. Each platform's XVeldridSurfaceHandler therefore has
			// to call OnVeldridInitialized itself.
		}

		private void InitializeOtherApi()
		{
			if (Backend == GraphicsBackend.Metal)
			{
				GraphicsDevice = GraphicsDevice.CreateMetal(GraphicsDeviceOptions);
			}
			else if (Backend == GraphicsBackend.Vulkan)
			{
				GraphicsDevice = GraphicsDevice.CreateVulkan(GraphicsDeviceOptions);
			}
			else if (Backend == GraphicsBackend.Direct3D11)
			{
				GraphicsDevice = GraphicsDevice.CreateD3D11(GraphicsDeviceOptions);
			}
			else
			{
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

			Handler.InitializeOtherApi();
		}

		protected virtual void OnVeldridInitialized(EventArgs e)
		{
			Properties.TriggerEvent(VeldridInitializedEvent, this, e);
		}
		protected virtual void OnDraw(EventArgs e)
		{
			Properties.TriggerEvent(DrawEvent, this, e);
		}
		protected virtual void OnResize(ResizeEventArgs e)
		{
			Swapchain?.Resize((uint)e.Width, (uint)e.Height);

			Properties.TriggerEvent(ResizeEvent, this, e);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			OnResize(new ResizeEventArgs(Width, Height));
		}
	}
}
