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

            Content = Panel;
        }
    }
}
