using Eto.Forms;
using Eto.Gl;
using System;
using System.Reflection;

namespace Eto.VeldridSurface
{
    public partial class MainForm : Form
    {
        public Action<GLSurface, VeldridDriver> PrepVeldrid;

        public Action<GLSurface> MakeUncurrent;

        public VeldridDriver VeldridDriver = new VeldridDriver();

        public GLSurface Surface;

        public MainForm(GLSurface s, Action<GLSurface> stripHandlers, Action<GLSurface, VeldridDriver> prepVeldrid)
        {
            Surface = s;

            PrepVeldrid = prepVeldrid;

			InitializeComponent();

            var panel = new Panel { Content = Surface };

            // Apparently each one of these handler additions is adding more
            // updateViewHandler instances to the surface's SizeChanged and
            // Paint events; they stack up, so they need to be removed each
            // time. Not sure what that's about.
            Surface.GLInitalized += (sender, e) =>
            {
                PrepVeldrid.Invoke(Surface, VeldridDriver);
                MakeUncurrent.Invoke(Surface);
                VeldridDriver.SetUpVeldrid();
                VeldridDriver.Resize(panel.Width, panel.Height);
                VeldridDriver.Clock.Start();
            };

            stripHandlers.Invoke(Surface);

            panel.SizeChanged += (sender, e) =>
            {
                VeldridDriver.Resize(panel.Width, panel.Height);
                VeldridDriver.Draw();
            };

            Content = panel;
        }
    }
}
