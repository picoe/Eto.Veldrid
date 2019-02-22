using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Windows;
using Eto.VeldridSurface;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
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
            var type = typeof(OpenTK.Platform.Utilities);

            MethodInfo createGetAddress = type.GetMethod("CreateGetAddress", BindingFlags.NonPublic | BindingFlags.Static);
            var getAddress = (GraphicsContext.GetAddressDelegate)createGetAddress.Invoke(null, Array.Empty<string>());

            return getAddress.Invoke(name);
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
                        if (!Contexts.ContainsKey(context))
                        {
                            Contexts.Add(context, s);
                        }
                        Contexts[context].MakeCurrent();

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
            GraphicsContext.CurrentContext.MakeCurrent(null);
        }

        internal void DeleteContext(IntPtr context)
        {
            // Do nothing! With this Eto.Gl-based approach, Veldrid should never
            // need to destroy an OpenGL context on its own; let the GLSurface
            // handle context deletion when it gets disposed of.
        }

        internal void SwapBuffers()
        {
            GraphicsContext.CurrentContext.SwapBuffers();
        }

        internal void SetVSync(bool on)
        {
            GraphicsContext.CurrentContext.SwapInterval = on ? 1 : 0;
        }

        // It's perfectly acceptable to create an instance of OpenGLPlatformInfo
        // without providing these last two methods, if indeed you don't need
        // them. They're stubbed out here only to serve as a reminder that they
        // can be customized should the occasion call for it.

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

    public class PuppetWinGLSurfaceHandler : WinGLSurfaceHandler
    {
        public override void AttachEvent(string id)
        {
            switch (id)
            {
                // Prevent the base surface handler class from attaching its own
                // internal event handler to these events; said handler calls
                // MakeCurrent, uses GL.Viewport, and swaps buffers. That's
                // undesirable here, so just attach the appropriate callback.
                case GLSurface.ShownEvent:
                    break;
                case GLSurface.GLDrawEvent:
                    Control.Paint += (sender, e) => Callback.OnDraw(Widget, EventArgs.Empty);
                    break;
                case GLSurface.SizeChangedEvent:
                    Control.SizeChanged += (sender, e) => Callback.OnSizeChanged(Widget, EventArgs.Empty);
                    break;
                default:
                    base.AttachEvent(id);
                    break;
            }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            GraphicsBackend backend = VeldridDriver.PreferredBackend;

            if (backend == GraphicsBackend.OpenGL)
            {
                Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });
            }

            var platform = new Eto.WinForms.Platform();

            if (backend == GraphicsBackend.OpenGL)
            {
                platform.Add<GLSurface.IHandler>(() => new PuppetWinGLSurfaceHandler());
            }

            var app = new Application(platform);

            Form form;
            if (backend == GraphicsBackend.Vulkan)
            {
                form = MakeVulkanForm();
            }
            else if (backend == GraphicsBackend.Direct3D11)
            {
                form = MakeDirect3DForm();
            }
            else
            {
                form = MakeOpenGLForm();
            }

            app.Run(form);
        }

        private static VeldridForm MakeDirect3DForm()
        {
            var form = new Direct3DForm();

            var gd = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
            var source = SwapchainSource.CreateWin32(
                form.Panel.NativeHandle, Marshal.GetHINSTANCE(typeof(VeldridForm).Module));
            var sc = gd.ResourceFactory.CreateSwapchain(
                new SwapchainDescription(source, 640, 480, null, false));

            form.VeldridDriver.GraphicsDevice = gd;
            form.VeldridDriver.SwapchainSource = source;
            form.VeldridDriver.Swapchain = sc;

            return form;
        }

        private static OpenGLForm MakeOpenGLForm()
        {
            var prep = new VeldridPrep();

            var form = new OpenGLForm(
                new GLSurface(),
                (s, d) => prep.PrepVeldrid(s, d));

            return form;
        }

        private static VeldridForm MakeVulkanForm()
        {
            var form = new Direct3DForm();

            var gd = GraphicsDevice.CreateVulkan(new GraphicsDeviceOptions());
            var source = SwapchainSource.CreateWin32(
                form.Panel.NativeHandle, Marshal.GetHINSTANCE(typeof(VeldridForm).Module));
            var sc = gd.ResourceFactory.CreateSwapchain(
                new SwapchainDescription(source, 640, 480, null, false));

            form.VeldridDriver.GraphicsDevice = gd;
            form.VeldridDriver.SwapchainSource = source;
            form.VeldridDriver.Swapchain = sc;

            return form;
        }
    }
}
