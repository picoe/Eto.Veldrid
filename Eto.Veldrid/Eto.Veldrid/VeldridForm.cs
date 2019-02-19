using Eto.Drawing;
using Eto.Forms;

namespace Eto.VeldridSurface
{
    public partial class VeldridForm : Form
    {
        public VeldridDriver VeldridDriver { get; } = new VeldridDriver();

        public Panel Panel { get; } = new Panel();

        public VeldridForm()
        {
            InitializeComponent();

            Panel.SizeChanged += (sender, e) =>
            {
                VeldridDriver.Resize(Panel.Width, Panel.Height);
                VeldridDriver.Draw();
            };

            Content = Panel;
        }
    }
}
