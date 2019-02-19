using Eto.Forms;
using Eto.Gl;
using System;

namespace Eto.VeldridSurface
{
    public partial class OpenGLForm : VeldridForm
    {
        public Action<GLSurface, VeldridDriver> PrepVeldrid;

        public Action<GLSurface> MakeUncurrent;

        public GLSurface Surface;

        public OpenGLForm(GLSurface s, Action<GLSurface> stripHandlers, Action<GLSurface, VeldridDriver> prepVeldrid)
        {
            Surface = s;

            PrepVeldrid = prepVeldrid;

            // Apparently each one of these handler additions is adding more
            // updateViewHandler instances to the surface's SizeChanged and
            // Paint events; they stack up, so they need to be removed each
            // time. Not sure what that's about.
            Surface.GLInitalized += (sender, e) =>
            {
                PrepVeldrid.Invoke(Surface, VeldridDriver);
                MakeUncurrent.Invoke(Surface);
                VeldridDriver.SetUpVeldrid();
                VeldridDriver.Resize(Panel.Width, Panel.Height);
                VeldridDriver.Clock.Start();
            };

            stripHandlers.Invoke(Surface);

            Panel.Content = Surface;
        }
    }
}
