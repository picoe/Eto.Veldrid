using Eto.Forms;
using Eto.Gl;
using Eto.Gl.Windows;
using Eto.VeldridSurface;
using OpenTK;
using System;
using System.Runtime.InteropServices;
using Veldrid;

namespace PlaceholderName
{
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
            GraphicsBackend backend = VeldridSurface.PreferredBackend;

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

            var form = new MainForm(WindowsInit, backend);

            app.Run(form);
        }

        public static void WindowsInit(VeldridSurface surface, GraphicsBackend backend)
        {
            if (backend == GraphicsBackend.Vulkan)
            {
                var gd = GraphicsDevice.CreateVulkan(new GraphicsDeviceOptions());
                var source = SwapchainSource.CreateWin32(
                    surface.NativeHandle, Marshal.GetHINSTANCE(typeof(VeldridSurface).Module));
                var sc = gd.ResourceFactory.CreateSwapchain(
                    new SwapchainDescription(source, 640, 480, null, false));

                surface.GraphicsDevice = gd;
                surface.Swapchain = sc;
            }
            else if (backend == GraphicsBackend.Direct3D11)
            {
                var gd = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
                var source = SwapchainSource.CreateWin32(
                    surface.NativeHandle, Marshal.GetHINSTANCE(typeof(VeldridSurface).Module));
                var sc = gd.ResourceFactory.CreateSwapchain(
                    new SwapchainDescription(source, 640, 480, null, false));

                surface.GraphicsDevice = gd;
                surface.Swapchain = sc;
            }
            else
            {
                string message;
                if (!Enum.IsDefined(typeof(GraphicsBackend), backend))
                {
                    message = "Unrecognized backend!";
                }
                else
                {
                    message = "Specified backend not supported on this platform!";
                }

                throw new ArgumentException(message);
            }
        }
    }
}
