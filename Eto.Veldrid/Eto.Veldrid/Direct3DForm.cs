using System;

namespace Eto.VeldridSurface
{
    public partial class Direct3DForm : VeldridForm
    {
        public Direct3DForm()
        {
            Shown += Direct3DForm_Shown;
        }

        private void Direct3DForm_Shown(object sender, EventArgs e)
        {
            VeldridDriver.SetUpVeldrid();
            VeldridDriver.Clock.Start();
        }
    }
}
