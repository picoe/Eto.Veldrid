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
            var current = (GraphicsContext)GraphicsContext.CurrentContext;

            var instance = new OpenTK.Graphics.OpenGL.GL();
            var type = instance.GetType();

            // This is a bit low level, but if push comes to shove it should be
            // possible to parse the string myself, I imagine. There's a
            // matching field of some sort that contains the IntPtr addresses,
            // and I think one could relatively easily connect the two.
            //var names = (byte[])type.GetField("EntryPointNames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);

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
                    var co = s.ControlObject as WinGLUserControl;

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

    public static class Program
    {
        [STAThread]
		public static void Main(string[] args)
        {
            GraphicsBackend backend = Veldrid.StartupUtilities.VeldridStartup.GetPlatformDefaultBackend();
            //backend = GraphicsBackend.OpenGL;

            if (backend == GraphicsBackend.OpenGL)
            {
                Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });
            }

            var platform = new Eto.WinForms.Platform();

            if (backend == GraphicsBackend.OpenGL)
            {
                platform.Add<GLSurface.IHandler>(() => new WinGLSurfaceHandler());
            }

            var app = new Application(platform);

            Form form;
            if (backend == GraphicsBackend.Direct3D11)
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
                (s) =>
                {
                    if (s.Handler is WinGLSurfaceHandler h)
                    {
                        // Prevent GLSurface from automatically refreshing, to avoid
                        // calls to MakeCurrent on the wrong thread.
                        h.Control.SizeChanged -= h.updateViewHandler;
                        h.Control.Paint -= h.updateViewHandler;
                    }
                },
                (s, d) => prep.PrepVeldrid(s, d))
            {
                MakeUncurrent = (s) =>
                {
                    var co = s.ControlObject as WinGLUserControl;

                    // Make this surface's context inactive on the main UI
                    // thread, allowing Veldrid to handle all of that.
                    co.Context.MakeCurrent(null);
                }
            };

            return form;
        }
    }
}
