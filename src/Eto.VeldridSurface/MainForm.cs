using Eto.Drawing;
using Eto.Forms;
using System;
using Veldrid;

namespace PlaceholderName
{
	public partial class MainForm : Form
	{
		VeldridSurface Surface;

		VeldridDriver Driver;

		OVPSettings ovpSettings;

		private bool _veldridReady = false;
		public bool VeldridReady
		{
			get { return _veldridReady; }
			private set
			{
				_veldridReady = value;

				SetUpVeldrid();
			}
		}

		private bool _formReady = false;
		public bool FormReady
		{
			get { return _formReady; }
			set
			{
				_formReady = value;

				SetUpVeldrid();
			}
		}

		public MainForm(GraphicsBackend backend)
		{
			InitializeComponent();

			Shown += (sender, e) => FormReady = true;

			Surface = new VeldridSurface(backend);
			Surface.VeldridInitialized += (sender, e) => VeldridReady = true;

			Content = Surface;

			ovpSettings = new OVPSettings();
			ovpSettings.enableFilledPolys = true;
			ovpSettings.drawPoints = true;

			PointF[] testPoly = new PointF[6];
			testPoly[0] = new PointF(2.0f, 2.0f);
			testPoly[1] = new PointF(15.0f, 12.0f);
			testPoly[2] = new PointF(8.0f, 24.0f);
			testPoly[3] = new PointF(8.0f, 15.0f);
			testPoly[4] = new PointF(3.0f, 2.0f);
			testPoly[5] = new PointF(2.0f, 2.0f);


			PointF[] testPoly2 = new PointF[6];
			testPoly2[0] = new PointF(12.0f, 2.0f);
			testPoly2[1] = new PointF(25.0f, 12.0f);
			testPoly2[2] = new PointF(18.0f, 24.0f);
			testPoly2[3] = new PointF(18.0f, 15.0f);
			testPoly2[4] = new PointF(13.0f, 2.0f);
			testPoly2[5] = new PointF(12.0f, 2.0f);

			ovpSettings.addPolygon(testPoly2, Color.FromArgb(0, 255, 255), 1.0f, true);

			ovpSettings.addPolygon(testPoly, Color.FromArgb(255, 0, 0), 1.0f, true);

			ovpSettings.addPolygon(testPoly2, Color.FromArgb(0, 255, 255), 1.0f, false);

			ovpSettings.addPolygon(testPoly, Color.FromArgb(255, 0, 0), 1.0f, false);


			Driver = new VeldridDriver(ref ovpSettings, ref Surface);
		}

		ContextMenu vp_menu;
		private void SetUpVeldrid()
		{
			if (!(FormReady && VeldridReady))
			{
				return;
			}

			Driver.SetUpVeldrid();

			Title = $"Veldrid backend: {Surface.Backend.ToString()}";

			Driver.Clock.Start();
			createVPContextMenu();
		}

		void createVPContextMenu()
        {
            // Single viewport now mandates regeneration of the context menu each time, to allow for entry screening.
            vp_menu = new ContextMenu();

            int itemIndex = 0;
            vp_menu.Items.Add(new ButtonMenuItem { Text = "Reset" });
            vp_menu.Items[itemIndex].Click += delegate
            {
				Driver.reset();
				Driver.updateViewport();
            };
            itemIndex++;

            var VPMenuDisplayOptionsMenu = vp_menu.Items.GetSubmenu("Display Options");
            itemIndex++;
            int displayOptionsSubItemIndex = 0;
            VPMenuDisplayOptionsMenu.Items.Add(new ButtonMenuItem { Text = "Toggle AA" });
            VPMenuDisplayOptionsMenu.Items[displayOptionsSubItemIndex].Click += delegate
            {
				Driver.ovpSettings.antiAlias = !Driver.ovpSettings.antiAlias;
				Driver.updateViewport();
            };
            displayOptionsSubItemIndex++;
            VPMenuDisplayOptionsMenu.Items.Add(new ButtonMenuItem { Text = "Toggle Fill" });
            VPMenuDisplayOptionsMenu.Items[displayOptionsSubItemIndex].Click += delegate
            {
				Driver.ovpSettings.enableFilledPolys = !Driver.ovpSettings.enableFilledPolys;
				Driver.updateViewport();
            };
            displayOptionsSubItemIndex++;
            VPMenuDisplayOptionsMenu.Items.Add(new ButtonMenuItem { Text = "Toggle Points" });
            VPMenuDisplayOptionsMenu.Items[displayOptionsSubItemIndex].Click += delegate
            {
				Driver.ovpSettings.drawPoints = !Driver.ovpSettings.drawPoints;
				Driver.updateViewport();
            };
            displayOptionsSubItemIndex++;

            {
                if (Driver.ovpSettings.lockedViewport)
                {
                    vp_menu.Items.Add(new ButtonMenuItem { Text = "Thaw" });
                }
                else
                {
                    vp_menu.Items.Add(new ButtonMenuItem { Text = "Freeze" });
                }
                vp_menu.Items[itemIndex].Click += delegate
                {
					Driver.freeze_thaw();
                };
                itemIndex++;
                vp_menu.Items.AddSeparator();
                itemIndex++;
                vp_menu.Items.Add(new ButtonMenuItem { Text = "Save bookmark" });
                vp_menu.Items[itemIndex].Click += delegate
                {
					Driver.saveLocation();
                };
                itemIndex++;
                vp_menu.Items.Add(new ButtonMenuItem { Text = "Load bookmark" });
                vp_menu.Items[itemIndex].Click += delegate
                {
					Driver.loadLocation();
                };
                if (!Driver.savedLocation_valid)
                {
                    vp_menu.Items[itemIndex].Enabled = false;
                }
                itemIndex++;
            }
            vp_menu.Items.AddSeparator();
            itemIndex++;
            vp_menu.Items.Add(new ButtonMenuItem { Text = "Zoom Extents" });
            vp_menu.Items[itemIndex].Click += delegate
            {
				Driver.zoomExtents();
            };
            itemIndex++;
            vp_menu.Items.AddSeparator();
            itemIndex++;
            vp_menu.Items.Add(new ButtonMenuItem { Text = "Zoom In" });
            vp_menu.Items[itemIndex].Click += delegate
            {
				Driver.zoomIn(-1);
				Driver.updateViewport();
            };
            itemIndex++;

            var VPMenuZoomInMenu = vp_menu.Items.GetSubmenu("Fast Zoom In");
            itemIndex++;
            int zoomInSubItemIndex = 0;
            VPMenuZoomInMenu.Items.Add(new ButtonMenuItem { Text = "Zoom In (x5)" });
            VPMenuZoomInMenu.Items[zoomInSubItemIndex].Click += delegate
            {
				Driver.zoomIn(-50);
				Driver.updateViewport();
            };
            zoomInSubItemIndex++;
            VPMenuZoomInMenu.Items.Add(new ButtonMenuItem { Text = "Zoom In (x10)" });
            VPMenuZoomInMenu.Items[zoomInSubItemIndex].Click += delegate
            {
				Driver.zoomIn(-100);
				Driver.updateViewport();
            };
            zoomInSubItemIndex++;
            VPMenuZoomInMenu.Items.Add(new ButtonMenuItem { Text = "Zoom In (x50)" });
            VPMenuZoomInMenu.Items[zoomInSubItemIndex].Click += delegate
            {
				Driver.zoomIn(-500);
				Driver.updateViewport();
            };
            zoomInSubItemIndex++;
            VPMenuZoomInMenu.Items.Add(new ButtonMenuItem { Text = "Zoom In (x100)" });
            VPMenuZoomInMenu.Items[zoomInSubItemIndex].Click += delegate
            {
				Driver.zoomIn(-1000);
				Driver.updateViewport();
            };
            zoomInSubItemIndex++;

            vp_menu.Items.AddSeparator();
            itemIndex++;

            vp_menu.Items.Add(new ButtonMenuItem { Text = "Zoom Out" });
            vp_menu.Items[itemIndex].Click += delegate
            {
				Driver.zoomOut(-1);
				Driver.updateViewport();
            };
            itemIndex++;

            var VPMenuZoomOutMenu = vp_menu.Items.GetSubmenu("Fast Zoom Out");
            itemIndex++;
            int zoomOutSubItemIndex = 0;
            VPMenuZoomOutMenu.Items.Add(new ButtonMenuItem { Text = "Zoom Out (x5)" });
            VPMenuZoomOutMenu.Items[zoomOutSubItemIndex].Click += delegate
            {
				Driver.zoomOut(-50);
				Driver.updateViewport();
            };
            zoomOutSubItemIndex++;
            VPMenuZoomOutMenu.Items.Add(new ButtonMenuItem { Text = "Zoom Out (x10)" });
            VPMenuZoomOutMenu.Items[zoomOutSubItemIndex].Click += delegate
            {
				Driver.zoomOut(-100);
				Driver.updateViewport();
            };
            zoomOutSubItemIndex++;
            VPMenuZoomOutMenu.Items.Add(new ButtonMenuItem { Text = "Zoom Out (x50)" });
            VPMenuZoomOutMenu.Items[zoomOutSubItemIndex].Click += delegate
            {
				Driver.zoomOut(-500);
				Driver.updateViewport();
            };
            zoomOutSubItemIndex++;
            VPMenuZoomOutMenu.Items.Add(new ButtonMenuItem { Text = "Zoom Out (x100)" });
            VPMenuZoomOutMenu.Items[zoomOutSubItemIndex].Click += delegate
            {
                Driver.zoomOut(-1000);
				Driver.updateViewport();
            };
            zoomOutSubItemIndex++;

            Driver.setContextMenu(ref vp_menu);
        }
	}
}
