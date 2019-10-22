using OpenTK.Graphics;
using System;
using System.Reflection;

namespace Eto.Veldrid
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
		// Depending on a private method like CreateGetAddress is hardly in
		// line with best practices, but it's the only way to grant Veldrid
		// access to OpenTK's internals. It's not pretty, but this Eto
		// integration was designed specifically for OpenTK 3.x, so sticking
		// to that branch should ensure private things stay where they are.
		static readonly Type _utilitiesType = typeof(OpenTK.Platform.Utilities);
		static readonly MethodInfo _createGetAddress = _utilitiesType.GetMethod("CreateGetAddress", BindingFlags.NonPublic | BindingFlags.Static);
		static readonly GraphicsContext.GetAddressDelegate _getProcAddress = (GraphicsContext.GetAddressDelegate)_createGetAddress.Invoke(null, Array.Empty<string>());

		// TODO: Find out if this is correct! The docs just say that the
		// 'openGLContextHandle' parameter of an OpenGLPlatformInfo is, and I
		// quote, "The OpenGL context handle", which isn't terribly helpful. Doing
		// this seems to work, but then if all Veldrid needs is the current
		// context, it could use the method I specify for GetCurrentContext,
		// couldn't it? There must be more to this.
		public static IntPtr GetGLContextHandle() => GetCurrentContext();

		public static IntPtr GetProcAddress(string name) => _getProcAddress.Invoke(name);

		public static IntPtr GetCurrentContext() => GraphicsContext.CurrentContextHandle.Handle;

		public static void ClearCurrentContext() => GraphicsContext.CurrentContext.MakeCurrent(null);

		public static void DeleteContext(IntPtr context)
		{
			// TODO: Update this! Eto.Gl/Eto.OpenTK is no longer in use, so I'll need
			// to dispose the specified context manually. I think.

			// Do nothing! With this Eto.Gl-based approach, Veldrid should never
			// need to destroy an OpenGL context on its own; let the GLSurface
			// take care of context deletion upon its own disposal.
		}

		public static void SwapBuffers() => GraphicsContext.CurrentContext.SwapBuffers();

		public static void SetVSync(bool on) => GraphicsContext.CurrentContext.SwapInterval = on ? 1 : 0;

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
}
