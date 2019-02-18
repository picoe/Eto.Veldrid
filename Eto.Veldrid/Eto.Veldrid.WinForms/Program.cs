using Eto.Gl;
using Eto.Gl.Windows;
using Eto.VeldridSurface;
using Eto.WinForms.Forms.Controls;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Veldrid;
using swf = System.Windows.Forms;

namespace PlaceholderName
{
    public class MyCustomHandler : DrawableHandler, MyCustomControl.IMyCustomControl
    {
        public string MyProperty { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public new class EtoDrawable : DrawableHandler.EtoDrawable
        {
            protected override swf.CreateParams CreateParams
            {
                get
                {
                    const int CS_VREDRAW = 0x1;
                    const int CS_HREDRAW = 0x2;
                    const int CS_OWNDC = 0x20;

                    base.CreateParams.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;

                    return base.CreateParams;
                }
            }

            public EtoDrawable(DrawableHandler handler) : base(handler)
            {
                SetStyle(swf.ControlStyles.Opaque, true);
                SetStyle(swf.ControlStyles.UserPaint, true);
                SetStyle(swf.ControlStyles.AllPaintingInWmPaint, true);
            }

            public new void SetStyle(swf.ControlStyles flag, bool value)
            {
                base.SetStyle(flag, value);
            }

            public new bool CanFocusMe => base.CanFocusMe;

            protected override void OnGotFocus(EventArgs e)
            {
                base.OnGotFocus(e);
                Invalidate();
            }

            protected override void OnLostFocus(EventArgs e)
            {
                base.OnLostFocus(e);
                Invalidate();
            }

            protected override bool ProcessDialogKey(swf.Keys keyData)
            {
                var e = new swf.KeyEventArgs(keyData);

                OnKeyDown(e);

                if (!e.Handled)
                {
                    if (CanFocusMe && keyData == swf.Keys.Tab || keyData == (swf.Keys.Tab | swf.Keys.Shift))
                    {
                        return base.ProcessDialogKey(keyData);
                    }
                    Handler.LastKeyDown = Eto.WinForms.KeyMap.ToEto(e.KeyData);
                }

                return e.Handled;
            }

            protected override void OnPaint(swf.PaintEventArgs e)
            {
                base.OnPaint(e);
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
            }
        }
    }

    public class TestHandler : DrawableHandler
    {
        public new class EtoDrawable : DrawableHandler.EtoDrawable
        {
            protected override swf.CreateParams CreateParams
            {
                get
                {
                    const int CS_VREDRAW = 0x1;
                    const int CS_HREDRAW = 0x2;
                    const int CS_OWNDC = 0x20;

                    swf.CreateParams cp = base.CreateParams;

                    cp.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;

                    return cp;
                }
            }

            public EtoDrawable(TestHandler handler) : base(handler)
            {
                SetStyle(swf.ControlStyles.Opaque, true);
                SetStyle(swf.ControlStyles.UserPaint, true);
                SetStyle(swf.ControlStyles.AllPaintingInWmPaint, true);
            }

            public new void SetStyle(swf.ControlStyles flag, bool value)
            {
                base.SetStyle(flag, value);
            }

            public new bool CanFocusMe => base.CanFocusMe;

            protected override void OnGotFocus(EventArgs e)
            {
                base.OnGotFocus(e);
                Invalidate();
            }

            protected override void OnLostFocus(EventArgs e)
            {
                base.OnLostFocus(e);
                Invalidate();
            }

            protected override bool ProcessDialogKey(swf.Keys keyData)
            {
                var e = new swf.KeyEventArgs(keyData);

                OnKeyDown(e);

                if (!e.Handled)
                {
                    if (CanFocusMe && keyData == swf.Keys.Tab || keyData == (swf.Keys.Tab | swf.Keys.Shift))
                    {
                        return base.ProcessDialogKey(keyData);
                    }
                    Handler.LastKeyDown = Eto.WinForms.KeyMap.ToEto(e.KeyData);
                }

                return e.Handled;
            }

            protected override void OnPaint(swf.PaintEventArgs e)
            {
                base.OnPaint(e);
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
            }
        }

        public TestHandler()
        {

        }

        public TestHandler(EtoDrawable control)
        {
            Control = control;
        }

        public new void Create()
        {
            Control = new EtoDrawable(this);
        }


    }

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

            // This is a bit low level, but if push comes to shove it
            // should be possible to parse the string myself, I imagine. There's
            // a matching field of some sort that contains the IntPtr addresses,
            // and I think one could relatively easily connect the two.
            //var gabbant = (byte[])type.GetField("EntryPointNames", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);

            var getAddress = type.GetMethod("GetAddress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            return (IntPtr)getAddress.Invoke(instance, new string[] { name });
        }

        internal void MakeCurrent(IntPtr context)
        {
            // Basically, keep a list of GLSurfaces that exist, or rather a dictionary
            // of IntPtrs and GLSurfaces, and just look that up to use the GLSurface's
            // MakeCurrent method. I guess.
            //OpenTK.Graphics.

            // Maybe just leave this as a stub? Veldrid should never need to make
            // anything current, I don't think; use etoViewport to handle that, just
            // make the appropriate GLSurface current at the start of every screen
            // refresh, and everything should work out. I think. Maybe not.

            var type = typeof(GraphicsContext);

            var duhboobles = (Dictionary<ContextHandle, IGraphicsContext>)type.GetField("available_contexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
            foreach (var pair in duhboobles)
            {
                foreach (GLSurface s in Surfaces)
                {
                    var co = s.ControlObject as WinGLUserControl;

                    if (co.Context == pair.Value)
                    {
                        if (!Contexts.ContainsKey(pair.Key.Handle))
                        {
                            Contexts.Add(pair.Key.Handle, s);
                        }
                        Contexts[pair.Key.Handle].MakeCurrent();
                        //co.Context.MakeCurrent(null);
                        break;
                    }
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
        }
    }

    public static class Program
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowPos(
            IntPtr handle,
            IntPtr handleAfter,
            int x, 
            int y,
            int cx,
            int cy,
            uint flags);

        

        

        [STAThread]
		public static void Main(string[] args)
        {
            Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });

            // Note: see https://gamedev.stackexchange.com/questions/110205/context-is-null-with-sdl-createwindowfrom-win32
            // for an explanation of the fact that SDL2 does not, in fact, offer GL
            // init in its CreateWindowFrom thing, which Veldrid uses if you make
            // an Sdl2Window from an existing window (either with the Sdl2Window class,
            // or with Sdl2Native.SDL_CreateWindowFrom). So after all this, it wasn't
            // simply the class style of the window that caused "isn't an OpenGL window",
            // it's impossible to do things that way. Damn it.

            //Sdl2Native.SDL_Init(SDLInitFlags.Video);
            var platform = new Eto.WinForms.Platform();

            //platform.Add<MyCustomControl.IMyCustomControl>(() => new MyCustomHandler());
            //platform.Add<Drawable.IHandler>(() => new TestHandler());





            platform.Add<GLSurface.IHandler>(() => new WinGLSurfaceHandler());
            var app = new Eto.Forms.Application(platform);


            var prep = new VeldridPrep();


            var form = new MainForm(
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

            app.Run(form);


            //var doomp = new Eto.WinForms.Forms.Controls.DrawableHandler.EtoDrawable();

            //var flumpun = new Eto.WinForms.Forms.Controls.DrawableHandler()
            //platform.Add<Drawable.IHandler>(()=>);

            //var thing = new WindowsVeldridControl();
            //thing.CreateControl();

            //Eto.Forms.Control eto = thing.ToEto();

            //var form = new MainForm
            //{
            //    Content = eto
            //};

            //var veldridSurface = new VeldridSurface();

            //var thing = veldridSurface.Farbarbus.Handle.ToEto

            //var surface = new VeldridSurface();
            //surface.SwapchainSource = SwapchainSource.CreateWin32(surface.NativeHandle, Marshal.GetHINSTANCE(typeof(MainForm).Module));

            //var surface = new VeldridSurface(new Action<Eto.Forms.Form, VeldridSurface>((f, d) =>
            //{
                //var coords = (Eto.Drawing.Point)d.PointToScreen(d.Location);

                //SetWindowPos(d.Sdl2Window.Handle, (IntPtr)(-1), 0, 0, 640, 480, 0);

                //if (d.Width > -1 && d.Height > -1)
                //{
                //    d.Sdl2Window.Width = d.Width;
                //    d.Sdl2Window.Height = d.Height;
                //
                //    SetWindowPos(d.Sdl2Window.Handle, d.ParentWindow.NativeHandle, 25, 50, d.Width, d.Height, 0);
                //
                //    Sdl2Native.SDL_SetWindowSize(d.Sdl2Window.SdlWindowHandle, 50, 666);
                //}

            //}));

            //surface.Sdl2Window = new Sdl2Window("FLAAAAAAHbeb", 0, 0, 640, 480, SDL_WindowFlags.OpenGL | SDL_WindowFlags.Borderless | SDL_WindowFlags.SkipTaskbar, false);

            //var form = new MainForm
            //{
                //Content = surface
            //};

            //SetParent(surface.Sdl2Window.Handle, surface.NativeHandle);
            //SetParent(surface.Sdl2Window.SdlWindowHandle, surface.NativeHandle);



            //app.Run(form);
        }
	}
}
