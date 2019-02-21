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

        public OpenGLForm(GLSurface s, Action<GLSurface, VeldridDriver> prepVeldrid)
        {
            Surface = s;

			PrepVeldrid = prepVeldrid;
            
            Surface.GLInitalized += (sender, e) =>
            {
                PrepVeldrid.Invoke(Surface, VeldridDriver);
                MakeUncurrent.Invoke(Surface);
                VeldridDriver.SetUpVeldrid();
            };

			Surface.Draw += Surface_Draw;
			Surface.LoadComplete += Surface_LoadComplete;
			Surface.SizeChanged += Surface_SizeChanged;

			Panel.Content = Surface;
		}

		void Surface_Draw(object sender, EventArgs e)
		{
			VeldridDriver.Draw();
		}

		void Surface_LoadComplete(object sender, EventArgs e)
		{
			VeldridDriver.Resize(Panel.Width, Panel.Height);
			VeldridDriver.Draw();
		}

		void Surface_SizeChanged(object sender, EventArgs e)
		{
			VeldridDriver.Resize(Surface.Width, Surface.Height);
		}
	}
}
