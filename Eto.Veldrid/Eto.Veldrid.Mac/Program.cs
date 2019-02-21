using System;
using System.Collections.Generic;
using System.Reflection;
using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Mac;
using Eto.VeldridSurface;
using OpenTK;
using OpenTK.Graphics;
using Veldrid;

namespace PlaceholderName
{
	public class VeldridPrep
	{
		public Dictionary<IntPtr, GLSurface> Contexts = new Dictionary<IntPtr, GLSurface>();

		public List<GLSurface> Surfaces = new List<GLSurface>();

		internal IntPtr GetGLContextHandle()
		{
			return GetCurrentContext();
		}

		internal IntPtr GetProcAddress(string name)
		{
			var current = (GraphicsContext)GraphicsContext.CurrentContext;

			var instance = new OpenTK.Graphics.OpenGL.GL();
			var type = instance.GetType();

			var getAddress = type.GetMethod("GetAddress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			return (IntPtr)getAddress.Invoke(instance, new string[] { name });
		}

		internal void MakeCurrent(IntPtr context)
		{
			var type = typeof(GraphicsContext);

			var available = (Dictionary<ContextHandle, IGraphicsContext>)type
				.GetField("available_contexts", BindingFlags.NonPublic | BindingFlags.Static)
				.GetValue(null);

			bool found = false;
			foreach (var pair in available)
			{
				foreach (GLSurface s in Surfaces)
				{
					if (pair.Key.Handle == context)
					{
						if (!Contexts.ContainsKey(pair.Key.Handle))
						{
							Contexts.Add(pair.Key.Handle, s);
						}
						Contexts[pair.Key.Handle].MakeCurrent();

						found = true;
					}

					if (found)
					{
						break;
					}
				}

				if (found)
				{
					break;
				}
			}
		}

		internal IntPtr GetCurrentContext()
		{
			return GraphicsContext.CurrentContextHandle.Handle;
		}

		internal void ClearCurrentContext()
		{

		}

		internal void DeleteContext(IntPtr context)
		{

		}

		internal void SwapBuffers()
		{
			GraphicsContext.CurrentContext.SwapBuffers();
		}

		internal void SetVSync(bool on)
		{

		}

		internal void SetSwapchainFramebuffer()
		{

		}

		internal void ResizeSwapchain(uint width, uint height)
		{

		}

		public void PrepVeldrid(GLSurface surface, VeldridDriver driver)
		{
			Surfaces.Add(surface);

			var platformInfo = new Veldrid.OpenGL.OpenGLPlatformInfo(
				GetGLContextHandle(),
				GetProcAddress,
				MakeCurrent,
				GetCurrentContext,
				ClearCurrentContext,
				DeleteContext,
				SwapBuffers,
				SetVSync,
				SetSwapchainFramebuffer,
				ResizeSwapchain);

			driver.GraphicsDevice = GraphicsDevice.CreateOpenGL(
				new GraphicsDeviceOptions(),
				platformInfo,
				640,
				480);

			driver.Swapchain = driver.GraphicsDevice.MainSwapchain;
		}
	}

	class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			GraphicsBackend backend = VeldridDriver.PreferredBackend;

			if (backend == GraphicsBackend.OpenGL)
			{
				Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });
			}

			var platform = new Eto.Mac.Platform();

			if (backend == GraphicsBackend.OpenGL)
			{
				platform.Add<GLSurface.IHandler>(() => new MacGLSurfaceHandler());
			}

			var app = new Application(platform);

			Form form;
			if (backend == GraphicsBackend.Metal)
			{
				throw new NotImplementedException("Metal not yet supported!");
			}
			else
			{
				form = MakeOpenGLForm();
			}

			app.Run(form);
		}

		private static OpenGLForm MakeOpenGLForm()
		{
			var prep = new VeldridPrep();

			var mode = new GraphicsMode(new ColorFormat(32), 8, 8, 8);

			var form = new OpenGLForm(
				new GLSurface(mode, 3, 2, GraphicsContextFlags.ForwardCompatible),
				prep.PrepVeldrid)
			{
				MakeUncurrent = (s) =>
				{
				}
			};

			return form;
		}
	}
}
