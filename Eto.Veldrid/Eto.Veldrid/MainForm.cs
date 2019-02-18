using Eto.Forms;
using Eto.Gl;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Eto.VeldridSurface
{
    public partial class MainForm : Form
    {
        public Action<GLSurface, VeldridDriver> PrepVeldrid;

        public Action<GLSurface> MakeUncurrent;

        private bool canDraw = false;

        public VeldridDriver VeldridDriver = new VeldridDriver();

        public GLSurface Surface;

        public MainForm(GLSurface s, Action<GLSurface> stripHandlers, Action<GLSurface, VeldridDriver> prepVeldrid)
        {
            Surface = s;

            PrepVeldrid = prepVeldrid;

			InitializeComponent();

            //var doobles = surfaceType.GetType();
            //var blumbus = GetTypeFromName(surfaceType.FullName);

            //var veldridSurface = (Eto.Forms.Control)Activator.CreateInstance(surfaceType);


            //Veldrid.Sdl2.Sdl2Native.SDL_Init(Veldrid.Sdl2.SDLInitFlags.Video);
            //Shown += MainForm_Shown;


            
            
            
            
            
            
            
            
            
            
            // Apparently each one of these handler additions is adding more
            // updateViewHandler instances to the surface's SizeChanged and
            // Paint events; they stack up, so they need to be removed each
            // time. Not sure what that's about.
            Surface.GLInitalized += (sender, e) =>
            {
                PrepVeldrid.Invoke(Surface, VeldridDriver);
                MakeUncurrent.Invoke(Surface);
                VeldridDriver.SetUpVeldrid();
                //VeldridDriver.Clock.Start();
            };

            stripHandlers.Invoke(Surface);

            //Surface.SizeChanged += (sender, e) =>
            //{
            //    MakeUncurrent.Invoke(Surface);
            //    VeldridDriver.Draw();
            //};

            stripHandlers.Invoke(Surface);




            var panel = new Panel { Content = Surface };

            panel.SizeChanged += (sender, e) =>
            {
                VeldridDriver.Resize(panel.Width, panel.Height);
                VeldridDriver.Draw();
            };

            Content = panel;


        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            //var thurman = Sdl2Native.SDL_CreateWindowFrom(NativeHandle);

            //var sb = new StringBuilder();
            //unsafe
            //{
            //    byte* error = Sdl2Native.SDL_GetError();

            //    byte[] c = new byte[1] { (byte)'.' };
            //    int offset = 0;
            //    while (c[0] != '\0')
            //    {
            //        Marshal.Copy((IntPtr)error + offset, c, 0, 1);
            //        sb.Append((char)c[0]);
            //        offset++;
            //    }
            //}

            //var jerry = new Sdl2Window(this.NativeHandle, false);
        }

        public static Type GetTypeFromName(string name)
        {
            Type type = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name);

                if (type != null)
                {
                    break;
                }
            }

            return type;
        }
    }
}
