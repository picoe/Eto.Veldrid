using Eto.Drawing;
using Eto.Forms;
using Eto.Veldrid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.SPIRV;

namespace VeldridEto
{
	public struct VertexPositionColor
	{
		public static uint SizeInBytes = (uint)Marshal.SizeOf(typeof(VertexPositionColor));

		Vector3 Position;
		RgbaFloat Color;

		public VertexPositionColor(Vector3 position, RgbaFloat color)
		{
			Position = position;
			Color = color;
		}
	}

	/// <summary>
	/// A class that controls rendering to a VeldridSurface.
	/// </summary>
	/// <remarks>
	/// VeldridSurface is only a basic control that lets you render to the screen
	/// using Veldrid. How exactly to do that is up to you; this driver class is
	/// only one possible approach, and in all likelihood not the most efficient.
	/// </remarks>
	public class VeldridDriver
	{
		public string ExecutableDirectory { get; set; }
		public string ShaderSubdirectory { get; set; }

		public VeldridSurface Surface;

		public UITimer Clock = new UITimer();

		public delegate void updateHost();
		public updateHost updateHostFunc { get; set; }

		public bool ok;
		public bool savedLocation_valid;
		PointF savedLocation;

		VertexPositionColor[] polyArray;
		uint[] polyFirst;
		uint[] polyVertexCount;

		VertexPositionColor[] tessArray;
		uint[] tessFirst;
		uint[] tessVertexCount;

		VertexPositionColor[] lineArray;
		uint[] lineFirst;
		uint[] lineVertexCount;

		VertexPositionColor[] pointsArray;
		uint[] pointsFirst;

		VertexPositionColor[] gridArray;
		ushort[] gridIndices;

		VertexPositionColor[] axesArray;
		ushort[] axesIndices;

		public OVPSettings ovpSettings;

		CommandList CommandList;
		DeviceBuffer GridVertexBuffer;
		DeviceBuffer GridIndexBuffer;
		DeviceBuffer AxesVertexBuffer;
		DeviceBuffer AxesIndexBuffer;

		DeviceBuffer LinesVertexBuffer;
		DeviceBuffer PointsVertexBuffer;
		DeviceBuffer PolysVertexBuffer;
		DeviceBuffer TessVertexBuffer;

		Pipeline PointsPipeline;
		Pipeline LinePipeline;
		Pipeline LinesPipeline;
		Pipeline FilledPipeline;

		Matrix4x4 ModelMatrix = Matrix4x4.Identity;
		DeviceBuffer ModelBuffer;
		ResourceSet ModelMatrixSet;

		Matrix4x4 ViewMatrix;
		DeviceBuffer ViewBuffer;
		ResourceSet ViewMatrixSet;

		private bool Ready = false;
		float pointWidth = 0.50f;

		public VeldridDriver(ref OVPSettings svpSettings, ref VeldridSurface surface)
		{
			try
			{
				ovpSettings = svpSettings;
				Surface = surface;
				Surface.MouseDown += downHandler;
				Surface.MouseMove += dragHandler;
				Surface.MouseUp += upHandler;
				Surface.MouseWheel += zoomHandler;
				Surface.GotFocus += addKeyHandler;
				// MouseHover += addKeyHandler;
				Surface.LostFocus += removeKeyHandler;
			}
			catch (Exception)
			{

			}
			Clock.Interval = 1.0f / 60.0f;
			Clock.Elapsed += Clock_Elapsed;

			Surface.Resize += (sender, e) =>
			{
				pUpdateViewport();
			};
		}

		private void Clock_Elapsed(object sender, EventArgs e)
		{
			if (!ovpSettings.changed)
			{
				return;
			}
			ovpSettings.changed = false;

			Draw();
		}

		private DateTime CurrentTime;
		private DateTime PreviousTime = DateTime.Now;

		float axisZ;
		float gridZ;

		// Use for drag handling.
		bool dragging;
		float x_orig;
		float y_orig;

		ContextMenu menu;

		public void setContextMenu(ref ContextMenu menu_)
		{
			menu = menu_;
		}

		public void changeSettingsRef(ref OVPSettings newSettings)
		{
			ovpSettings = newSettings;
			updateViewport();
		}

		Point WorldToScreen(float x, float y)
		{
			return new Point((int)((x - ovpSettings.cameraPosition.X / (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + Surface.RenderWidth / 2),
					(int)((y - ovpSettings.cameraPosition.Y / (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + Surface.RenderHeight / 2));
		}

		Size WorldToScreen(SizeF pt)
		{
			Point pt1 = WorldToScreen(0, 0);
			Point pt2 = WorldToScreen(pt.Width, pt.Height);
			return new Size(pt2.X - pt1.X, pt2.Y - pt1.Y);
		}

		PointF ScreenToWorld(int x, int y)
		{
			return new PointF((x - Surface.RenderWidth / 2) * (ovpSettings.zoomFactor * ovpSettings.base_zoom) + ovpSettings.cameraPosition.X,
					 (y - Surface.RenderHeight / 2) * (ovpSettings.zoomFactor * ovpSettings.base_zoom) + ovpSettings.cameraPosition.Y);
		}

		void downHandler(object sender, MouseEventArgs e)
		{
			if (e.Buttons == MouseButtons.Primary)
			{
				setDown(e.Location.X, e.Location.Y);
			}
			//e.Handled = true;
		}

		void setDown(float x, float y)
		{
			if (!dragging && !ovpSettings.lockedViewport) // might not be needed, but seemed like a safe approach to avoid re-setting these in a drag event.
			{
				x_orig = x;
				y_orig = y;
				dragging = true;
			}
		}

		public void saveLocation()
		{
			savedLocation = new PointF(ovpSettings.cameraPosition.X, ovpSettings.cameraPosition.Y);
			savedLocation_valid = true;
		}

		public void zoomExtents()
		{
			getExtents();

			if (((ovpSettings.polyList.Count == 0) && (ovpSettings.lineList.Count == 0)) ||
				((ovpSettings.minX == 0) && (ovpSettings.maxX == 0)) ||
				((ovpSettings.minY == 0) && (ovpSettings.maxY == 0)))
			{
				reset();
				return;
			}

			// Locate camera at center of the polygon field.
			float dX = ovpSettings.maxX - ovpSettings.minX;
			float dY = ovpSettings.maxY - ovpSettings.minY;
			float cX = (dX / 2.0f) + ovpSettings.minX;
			float cY = (dY / 2.0f) + ovpSettings.minY;

			// Now need to get the zoom level organized.
			float zoomLevel_x = dX / Surface.Width;
			float zoomLevel_y = dY / Surface.Height;

			if (zoomLevel_x > zoomLevel_y)
			{
				ovpSettings.zoomFactor = zoomLevel_x / ovpSettings.base_zoom;
			}
			else
			{
				ovpSettings.zoomFactor = zoomLevel_y / ovpSettings.base_zoom;
			}
			ovpSettings.changed = true;

			goToLocation(cX, cY);
		}

		public void loadLocation()
		{
			if (savedLocation_valid)
			{
				ovpSettings.cameraPosition = new PointF(savedLocation.X, savedLocation.Y);
				ovpSettings.changed = true;
				updateViewport();
			}
		}

		public void goToLocation(float x, float y)
		{
			ovpSettings.cameraPosition = new PointF(x, y);
			updateViewport();
		}

		void dragHandler(object sender, MouseEventArgs e)
		{
			if (ovpSettings.lockedViewport)
			{
				return;
			}
			if (e.Buttons == MouseButtons.Primary)
			{
				if (!dragging)
				{
					setDown(e.Location.X, e.Location.Y);
				}
				object locking = new object();
				lock (locking)
				{
					float new_X = (ovpSettings.cameraPosition.X - ((e.Location.X - x_orig) * ovpSettings.zoomFactor * ovpSettings.base_zoom));
					float new_Y = (ovpSettings.cameraPosition.Y + ((e.Location.Y - y_orig) * ovpSettings.zoomFactor * ovpSettings.base_zoom));
					ovpSettings.cameraPosition = new PointF(new_X, new_Y);
					ovpSettings.changed = true;
					x_orig = e.Location.X;
					y_orig = e.Location.Y;
				}
			}
			updateViewport();
			//e.Handled = true;
		}

		public void freeze_thaw()
		{
			ovpSettings.lockedViewport = !ovpSettings.lockedViewport;
			ovpSettings.changed = true;
			updateHostFunc?.Invoke();
		}

		void upHandler(object sender, MouseEventArgs e)
		{
			if (e.Buttons == MouseButtons.Alternate)
			{
				if (menu != null)
				{
					menu.Show(Surface);
				}
			}
			if (ovpSettings.lockedViewport)
			{
				return;
			}
			if (e.Buttons == MouseButtons.Primary)
			{
				dragging = false;
			}
			//e.Handled = true
		}

		public void zoomIn(float delta)
		{
			if (ovpSettings.lockedViewport)
			{
				return;
			}
			ovpSettings.zoomFactor += (ovpSettings.zoomStep * 0.01f * delta);
			ovpSettings.changed = true;
		}

		public void zoomOut(float delta)
		{
			if (ovpSettings.lockedViewport)
			{
				return;
			}
			ovpSettings.zoomFactor -= (ovpSettings.zoomStep * 0.01f * delta);
			if (ovpSettings.zoomFactor < 0.0001)
			{
				ovpSettings.zoomFactor = 0.0001f; // avoid any chance of getting to zero.
			}
			ovpSettings.changed = true;
		}

		void panVertical(float delta)
		{
			if (ovpSettings.lockedViewport)
			{
				return;
			}
			ovpSettings.cameraPosition.Y += delta / 10;
			ovpSettings.changed = true;
		}

		void panHorizontal(float delta)
		{
			if (ovpSettings.lockedViewport)
			{
				return;
			}
			ovpSettings.cameraPosition.X += delta / 10;
			ovpSettings.changed = true;
		}

		void addKeyHandler(object sender, EventArgs e)
		{
			Surface.KeyDown += keyHandler;
		}

		void removeKeyHandler(object sender, EventArgs e)
		{
			Surface.KeyDown -= keyHandler;
		}

		public void reset()
		{
			if (ovpSettings.lockedViewport)
			{
				return;
			}
			ovpSettings.cameraPosition = new PointF(ovpSettings.default_cameraPosition.X, ovpSettings.default_cameraPosition.Y);
			ovpSettings.zoomFactor = 1.0f;
			ovpSettings.changed = true;
		}

		void keyHandler(object sender, KeyEventArgs e)
		{
			if (ovpSettings.lockedViewport)
			{
				if (e.Key != Keys.F)
				{
					return;
				}
				ovpSettings.lockedViewport = false;
				return;
			}

			if (e.Key == Keys.F)
			{
				ovpSettings.lockedViewport = true;
				return;
			}

			if (e.Key == Keys.R)
			{
				reset();
			}

			float stepping = 10.0f * ovpSettings.zoomFactor;

			bool doUpdate = true;
			if (e.Key == Keys.A)
			{
				panHorizontal(-stepping);
			}
			if (e.Key == Keys.D)
			{
				panHorizontal(stepping);
			}
			if (e.Key == Keys.W)
			{
				panVertical(stepping);
			}
			if (e.Key == Keys.S)
			{
				panVertical(-stepping);
			}
			if (e.Key == Keys.N)
			{
				zoomOut(-1);
			}
			if (e.Key == Keys.M)
			{
				zoomIn(-1);
			}

			if (e.Key == Keys.X)
			{
				zoomExtents();
				doUpdate = false; // update performed in extents
			}

			if (doUpdate)
			{
				updateViewport();
			}
			e.Handled = true;
		}

		void zoomHandler(object sender, MouseEventArgs e)
		{
			if (ovpSettings.lockedViewport)
			{
				return;
			}

			float wheelZoom = e.Delta.Height; // SystemInformation.MouseWheelScrollLines;
			if (wheelZoom > 0)
			{
				zoomIn(wheelZoom);
			}
			if (wheelZoom < 0)
			{
				zoomOut(-wheelZoom);
			}
			updateViewport();
			//e.Handled = true;
		}

		void getExtents()
		{
			float minX = 0;
			float maxX = 0;
			float minY = 0, maxY = 0;

			if ((ovpSettings.polyList.Count == 0) && (ovpSettings.lineList.Count == 0))
			{
				ovpSettings.minX = 0;
				ovpSettings.maxX = 0;
				ovpSettings.minY = 0;
				ovpSettings.maxY = 0;
				return;
			}

			if (ovpSettings.polyList.Count != 0)
			{
				minX = ovpSettings.polyList[0].poly[0].X;
				maxX = ovpSettings.polyList[0].poly[0].X;
				minY = ovpSettings.polyList[0].poly[0].Y;
				maxY = ovpSettings.polyList[0].poly[0].Y;
				for (int poly = 0; poly < ovpSettings.polyList.Count; poly++)
				{
					float tMinX = ovpSettings.polyList[poly].poly.Min(p => p.X);
					if (tMinX < minX)
					{
						minX = tMinX;
					}
					float tMaxX = ovpSettings.polyList[poly].poly.Max(p => p.X);
					if (tMaxX > maxX)
					{
						maxX = tMaxX;
					}
					float tMinY = ovpSettings.polyList[poly].poly.Min(p => p.Y);
					if (tMinY < minY)
					{
						minY = tMinY;
					}
					float tMaxY = ovpSettings.polyList[poly].poly.Max(p => p.Y);
					if (tMaxY > maxY)
					{
						maxY = tMaxY;
					}
				}
			}

			if (ovpSettings.lineList.Count != 0)
			{
				for (int line = 0; line < ovpSettings.lineList.Count; line++)
				{
					float tMinX = ovpSettings.lineList[line].poly.Min(p => p.X);
					if (tMinX < minX)
					{
						minX = tMinX;
					}
					float tMaxX = ovpSettings.lineList[line].poly.Max(p => p.X);
					if (tMaxX > maxX)
					{
						maxX = tMaxX;
					}
					float tMinY = ovpSettings.lineList[line].poly.Min(p => p.Y);
					if (tMinY < minY)
					{
						minY = tMinY;
					}
					float tMaxY = ovpSettings.lineList[line].poly.Max(p => p.Y);
					if (tMaxY > maxY)
					{
						maxY = tMaxY;
					}
				}
			}

			ovpSettings.minX = minX;
			ovpSettings.maxX = maxX;
			ovpSettings.minY = minY;
			ovpSettings.maxY = maxY;

			ovpSettings.changed = true;
		}

		void drawPolygons()
		{
			try
			{
				List<VertexPositionColor> polyList = new List<VertexPositionColor>();

				List<VertexPositionColor> pointsList = new List<VertexPositionColor>();

				List<VertexPositionColor> tessPolyList = new List<VertexPositionColor>();

				int polyListCount = ovpSettings.polyList.Count();
				int bgPolyListCount = ovpSettings.bgPolyList.Count();
				int tessPolyListCount = ovpSettings.tessPolyList.Count();

				// Carve our Z-space up to stack polygons
				int numPolys = 1;

				numPolys = polyListCount + bgPolyListCount;
				// Create our first and count arrays for the vertex indices, to enable polygon separation when rendering.
				polyFirst = new uint[numPolys];
				polyVertexCount = new uint[numPolys];

				tessFirst = new uint[tessPolyListCount];
				tessVertexCount = new uint[tessPolyListCount];

				List<uint> tFirst = new List<uint>();

				uint tCounter = 0;

				if (ovpSettings.enableFilledPolys)
				{
					numPolys += tessPolyListCount;
				}

				float polyZStep = 1.0f / Math.Max(1, numPolys + 1); // avoid a div by zero risk; pad the poly number also to reduce risk of adding a poly beyond the clipping range

				int counter = 0; // vertex count that will be used to define 'first' index for each polygon.
				int previouscounter = 0; // will be used to derive the number of vertices in each polygon.

				float polyZ = 0;

				if (ovpSettings.enableFilledPolys)
				{
					for (int poly = 0; poly < tessPolyListCount; poly++)
					{
						tessFirst[poly] = (uint)(poly * 3);
						float alpha = ovpSettings.tessPolyList[poly].alpha;
						polyZ += polyZStep;
						for (int pt = 0; pt < 3; pt++)
						{
							tessPolyList.Add(new VertexPositionColor(new Vector3(ovpSettings.tessPolyList[poly].poly[pt].X, ovpSettings.tessPolyList[poly].poly[pt].Y, polyZ),
												new RgbaFloat(ovpSettings.tessPolyList[poly].color.R, ovpSettings.tessPolyList[poly].color.G, ovpSettings.tessPolyList[poly].color.B, alpha)));
						}
						tessVertexCount[poly] = 3;
					}
				}

				// Pondering options here - this would make a nice border construct around the filled geometry, amongst other things.
				for (int poly = 0; poly < polyListCount; poly++)
				{
					float alpha = ovpSettings.polyList[poly].alpha;
					if (ovpSettings.enableFilledPolys)
					{
						alpha = 1.0f;
					}
					polyZ += polyZStep;
					polyFirst[poly] = (uint)counter;
					previouscounter = counter;
					int polyLength = ovpSettings.polyList[poly].poly.Length - 1;
					for (int pt = 0; pt < polyLength; pt++)
					{
						polyList.Add(new VertexPositionColor(new Vector3(ovpSettings.polyList[poly].poly[pt].X, ovpSettings.polyList[poly].poly[pt].Y, polyZ),
										new RgbaFloat(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha)));
						counter++;
						polyList.Add(new VertexPositionColor(new Vector3(ovpSettings.polyList[poly].poly[pt + 1].X, ovpSettings.polyList[poly].poly[pt + 1].Y, polyZ),
										new RgbaFloat(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha)));
						counter++;

						if (ovpSettings.drawPoints)
						{
							tFirst.Add(tCounter);
							pointsList.Add(new VertexPositionColor(new Vector3(ovpSettings.polyList[poly].poly[pt].X - (pointWidth / 2.0f), ovpSettings.polyList[poly].poly[pt].Y - (pointWidth / 2.0f), 1.0f), new RgbaFloat(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha)));
							tCounter++;
							pointsList.Add(new VertexPositionColor(new Vector3(ovpSettings.polyList[poly].poly[pt].X - (pointWidth / 2.0f), ovpSettings.polyList[poly].poly[pt].Y + (pointWidth / 2.0f), 1.0f), new RgbaFloat(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha)));
							tCounter++;
							pointsList.Add(new VertexPositionColor(new Vector3(ovpSettings.polyList[poly].poly[pt].X + (pointWidth / 2.0f), ovpSettings.polyList[poly].poly[pt].Y - (pointWidth / 2.0f), 1.0f), new RgbaFloat(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha)));
							tCounter++;

							tFirst.Add(tCounter);
							pointsList.Add(new VertexPositionColor(new Vector3(ovpSettings.polyList[poly].poly[pt].X + (pointWidth / 2.0f), ovpSettings.polyList[poly].poly[pt].Y - (pointWidth / 2.0f), 1.0f), new RgbaFloat(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha)));
							tCounter++;
							pointsList.Add(new VertexPositionColor(new Vector3(ovpSettings.polyList[poly].poly[pt].X - (pointWidth / 2.0f), ovpSettings.polyList[poly].poly[pt].Y + (pointWidth / 2.0f), 1.0f), new RgbaFloat(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha)));
							tCounter++;
							pointsList.Add(new VertexPositionColor(new Vector3(ovpSettings.polyList[poly].poly[pt].X + (pointWidth / 2.0f), ovpSettings.polyList[poly].poly[pt].Y + (pointWidth / 2.0f), 1.0f), new RgbaFloat(ovpSettings.polyList[poly].color.R, ovpSettings.polyList[poly].color.G, ovpSettings.polyList[poly].color.B, alpha)));
							tCounter++;
						}
					}
					polyVertexCount[poly] = (uint)(counter - previouscounter); // set our vertex count for the polygon.
				}

				polyZ = 0;
				for (int poly = 0; poly < bgPolyListCount; poly++)
				{
					float alpha = ovpSettings.bgPolyList[poly].alpha;
					polyZ += polyZStep;
					polyFirst[poly + polyListCount] = (uint)counter;
					previouscounter = counter;

					int bgPolyLength = ovpSettings.bgPolyList[poly].poly.Length - 1;
					for (int pt = 0; pt < bgPolyLength; pt++)
					{
						polyList.Add(new VertexPositionColor(new Vector3(ovpSettings.bgPolyList[poly].poly[pt].X, ovpSettings.bgPolyList[poly].poly[pt].Y, polyZ),
										new RgbaFloat(ovpSettings.bgPolyList[poly].color.R, ovpSettings.bgPolyList[poly].color.G, ovpSettings.bgPolyList[poly].color.B, alpha)));
						counter++;
						polyList.Add(new VertexPositionColor(new Vector3(ovpSettings.bgPolyList[poly].poly[pt + 1].X, ovpSettings.bgPolyList[poly].poly[pt + 1].Y, polyZ),
										new RgbaFloat(ovpSettings.bgPolyList[poly].color.R, ovpSettings.bgPolyList[poly].color.G, ovpSettings.bgPolyList[poly].color.B, alpha)));
						counter++;
					}
					polyVertexCount[poly + polyListCount] = (uint)(counter - previouscounter); // set our vertex count for the polygon.
				}

				polyArray = polyList.ToArray();

				pointsArray = pointsList.ToArray();
				pointsFirst = tFirst.ToArray();

				tessArray = tessPolyList.ToArray();
			}
			catch (Exception)
			{
				// Can ignore - not critical.
			}

			if (polyArray.Length > 0)
			{
				updateBuffer(ref PolysVertexBuffer, polyArray, VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer);
			}
			if (pointsArray.Length > 0)
			{
				updateBuffer(ref PointsVertexBuffer, pointsArray, VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer);
			}
			if (tessArray.Length > 0)
			{
				updateBuffer(ref TessVertexBuffer, tessArray, VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer);
			}
		}

		void drawLines()
		{
			try
			{
				List<VertexPositionColor> lineList = new List<VertexPositionColor>();

				// Carve our Z-space up to stack polygons
				float polyZStep = 1.0f / ovpSettings.lineList.Count();

				// Create our first and count arrays for the vertex indices, to enable polygon separation when rendering.
				int tmp = ovpSettings.lineList.Count();
				lineFirst = new uint[tmp];
				lineVertexCount = new uint[tmp];

				for (int poly = 0; poly < tmp; poly++)
				{
					float alpha = ovpSettings.lineList[poly].alpha;
					float polyZ = poly * polyZStep;
					lineFirst[poly] = (uint)lineList.Count;
					for (int pt = 0; pt < ovpSettings.lineList[poly].poly.Length; pt++)
					{
						lineList.Add(new VertexPositionColor(new Vector3(ovpSettings.lineList[poly].poly[pt].X, ovpSettings.lineList[poly].poly[pt].Y, polyZ), new RgbaFloat(ovpSettings.lineList[poly].color.R, ovpSettings.lineList[poly].color.G, ovpSettings.lineList[poly].color.B, alpha)));
					}
					lineVertexCount[poly] = (uint)ovpSettings.lineList[poly].poly.Length; // set our vertex count for the polygon.
				}

				lineArray = lineList.ToArray();

				updateBuffer(ref LinesVertexBuffer, lineArray, VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer);
			}
			catch (Exception)
			{
				// Can ignore - not critical.
			}
		}

		void drawGrid()
		{
			if (ovpSettings.showGrid)
			{
				float spacing = ovpSettings.gridSpacing;
				if (ovpSettings.dynamicGrid)
				{
					while (WorldToScreen(new SizeF(spacing, 0.0f)).Width > 12.0f)
						spacing /= 10.0f;

					while (WorldToScreen(new SizeF(spacing, 0.0f)).Width < 4.0f)
						spacing *= 10.0f;
				}

				List<VertexPositionColor> grid = new List<VertexPositionColor>();

				if (WorldToScreen(new SizeF(spacing, 0.0f)).Width >= 4.0f)
				{
					int k = 0;
					for (float i = 0; i > -(Surface.RenderWidth * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.X; i -= spacing)
					{
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9)
						{
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10)
						{
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add(new VertexPositionColor(new Vector3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Surface.RenderHeight, gridZ), new RgbaFloat(r, g, b, 1.0f)));
						grid.Add(new VertexPositionColor(new Vector3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Surface.RenderHeight, gridZ), new RgbaFloat(r, g, b, 1.0f)));
					}
					k = 0;
					for (float i = 0; i < (Surface.RenderWidth * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.X; i += spacing)
					{
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9)
						{
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10)
						{
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add(new VertexPositionColor(new Vector3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Surface.RenderHeight, gridZ), new RgbaFloat(r, g, b, 1.0f)));
						grid.Add(new VertexPositionColor(new Vector3(i, ovpSettings.cameraPosition.Y + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Surface.RenderHeight, gridZ), new RgbaFloat(r, g, b, 1.0f)));
					}
					k = 0;
					for (float i = 0; i > -(Surface.RenderHeight * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.Y; i -= spacing)
					{
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9)
						{
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10)
						{
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add(new VertexPositionColor(new Vector3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Surface.RenderWidth, i, gridZ), new RgbaFloat(r, g, b, 1.0f)));
						grid.Add(new VertexPositionColor(new Vector3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Surface.RenderWidth, i, gridZ), new RgbaFloat(r, g, b, 1.0f)));
					}
					k = 0;
					for (float i = 0; i < (Surface.RenderHeight * (ovpSettings.zoomFactor * ovpSettings.base_zoom)) + ovpSettings.cameraPosition.Y; i += spacing)
					{
						float r = 0.0f;
						float g = 0.0f;
						float b = 0.0f;
						if (k <= 9)
						{
							r = ovpSettings.minorGridColor.R;
							g = ovpSettings.minorGridColor.G;
							b = ovpSettings.minorGridColor.B;
						}
						if (k == 10)
						{
							r = ovpSettings.majorGridColor.R;
							g = ovpSettings.majorGridColor.G;
							b = ovpSettings.majorGridColor.B;
							k = 0;
						}
						k++;
						grid.Add(new VertexPositionColor(new Vector3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * Surface.RenderWidth, i, gridZ), new RgbaFloat(r, g, b, 1.0f)));
						grid.Add(new VertexPositionColor(new Vector3(ovpSettings.cameraPosition.X + (ovpSettings.zoomFactor * ovpSettings.base_zoom) * -Surface.RenderWidth, i, gridZ), new RgbaFloat(r, g, b, 1.0f)));
					}
					gridArray = grid.ToArray();
					gridIndices = new ushort[gridArray.Length];
					for (ushort i = 0; i < gridIndices.Length; i++)
					{
						gridIndices[i] = i;
					}
				}

				updateBuffer(ref GridVertexBuffer, gridArray, VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer);
				updateBuffer(ref GridIndexBuffer, gridIndices, sizeof(ushort), BufferUsage.IndexBuffer);
			}
		}

		void drawAxes()
		{
			if (ovpSettings.showAxes)
			{
				axesArray = new VertexPositionColor[4];
				axesArray[0] = new VertexPositionColor(new Vector3(0.0f, ovpSettings.cameraPosition.Y + Surface.RenderHeight * (ovpSettings.zoomFactor * ovpSettings.base_zoom), axisZ), new RgbaFloat(ovpSettings.axisColor.R, ovpSettings.axisColor.G, ovpSettings.axisColor.B, 1.0f));
				axesArray[1] = new VertexPositionColor(new Vector3(0.0f, ovpSettings.cameraPosition.Y - Surface.RenderHeight * (ovpSettings.zoomFactor * ovpSettings.base_zoom), axisZ), new RgbaFloat(ovpSettings.axisColor.R, ovpSettings.axisColor.G, ovpSettings.axisColor.B, 1.0f));
				axesArray[2] = new VertexPositionColor(new Vector3(ovpSettings.cameraPosition.X + Surface.RenderWidth * (ovpSettings.zoomFactor * ovpSettings.base_zoom), 0.0f, axisZ), new RgbaFloat(ovpSettings.axisColor.R, ovpSettings.axisColor.G, ovpSettings.axisColor.B, 1.0f));
				axesArray[3] = new VertexPositionColor(new Vector3(ovpSettings.cameraPosition.X - Surface.RenderWidth * (ovpSettings.zoomFactor * ovpSettings.base_zoom), 0.0f, axisZ), new RgbaFloat(ovpSettings.axisColor.R, ovpSettings.axisColor.G, ovpSettings.axisColor.B, 1.0f));

				axesIndices = new ushort[4] { 0, 1, 2, 3 };

				updateBuffer(ref AxesVertexBuffer, axesArray, VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer);
				updateBuffer(ref AxesIndexBuffer, axesIndices, sizeof(ushort), BufferUsage.IndexBuffer);
			}

		}

		/// <summary>
		/// Fills the given buffer with the contents of 'data', creating or
		/// resizing it as necessary.
		/// </summary>
		/// <param name="buffer">The Veldrid.DeviceBuffer to fill.</param>
		/// <param name="data">The array of elements to put in the buffer.</param>
		/// <param name="elementSize">The size in bytes of each element.</param>
		/// <param name="usage">The Veldrid.BufferUsage type of 'buffer'.</param>
		public void updateBuffer<T>(ref DeviceBuffer buffer, T[] data, uint elementSize, BufferUsage usage)
			where T : struct
		{
			buffer?.Dispose();

			ResourceFactory factory = Surface.GraphicsDevice.ResourceFactory;

			buffer = factory.CreateBuffer(new BufferDescription(elementSize * (uint)data.Length, usage));

			Surface.GraphicsDevice.UpdateBuffer(buffer, 0, data);
		}

		public void updateViewport()
		{
			if (!Surface.ControlReady)
			{
				return;
			}

			if (!ovpSettings.changed)
			{
				return;
			}

			pUpdateViewport();
		}

		void pUpdateViewport()
		{
			drawAxes();
			drawGrid();
			drawLines();
			drawPolygons();
			updateHostFunc?.Invoke();
			Draw();
		}

		public void Draw()
		{
			if (!Ready)
			{
				return;
			}

			CommandList.Begin();

			ModelMatrix *= Matrix4x4.CreateFromAxisAngle(
				new Vector3(0, 0, 1), 0);
			CommandList.UpdateBuffer(ModelBuffer, 0, ModelMatrix);

			float zoom = ovpSettings.zoomFactor * ovpSettings.base_zoom;

			float left = ovpSettings.cameraPosition.X - (Surface.RenderWidth / 2) * zoom;
			float right = ovpSettings.cameraPosition.X + (Surface.RenderWidth / 2) * zoom;
			float bottom = ovpSettings.cameraPosition.Y + (Surface.RenderHeight / 2) * zoom;
			float top = ovpSettings.cameraPosition.Y - (Surface.RenderHeight / 2) * zoom;

			ViewMatrix = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, 0.0f, 1.0f);
			CommandList.UpdateBuffer(ViewBuffer, 0, ViewMatrix);

			CommandList.SetFramebuffer(Surface.Swapchain.Framebuffer);

			// These commands differ from the stock Veldrid "Getting Started"
			// tutorial in two ways. First, the viewport is cleared to pink
			// instead of black so as to more easily distinguish between errors
			// in creating a graphics context and errors drawing vertices within
			// said context. Second, this project creates its swapchain with a
			// depth buffer, and that buffer needs to be reset at the start of
			// each frame.
			CommandList.ClearColorTarget(0, RgbaFloat.White);
			CommandList.ClearDepthStencil(1.0f);

			if (GridVertexBuffer != null)
			{
				CommandList.SetVertexBuffer(0, GridVertexBuffer);
				CommandList.SetIndexBuffer(GridIndexBuffer, IndexFormat.UInt16);
				CommandList.SetPipeline(LinePipeline);
				CommandList.SetGraphicsResourceSet(0, ViewMatrixSet);
				CommandList.SetGraphicsResourceSet(1, ModelMatrixSet);

				CommandList.DrawIndexed(
					indexCount: (uint)gridIndices.Length,
					instanceCount: 1,
					indexStart: 0,
					vertexOffset: 0,
					instanceStart: 0);
			}

			if (AxesVertexBuffer != null)
			{
				CommandList.SetVertexBuffer(0, AxesVertexBuffer);
				CommandList.SetIndexBuffer(AxesIndexBuffer, IndexFormat.UInt16);
				CommandList.SetPipeline(LinePipeline);
				CommandList.SetGraphicsResourceSet(0, ViewMatrixSet);
				CommandList.SetGraphicsResourceSet(1, ModelMatrixSet);

				CommandList.DrawIndexed(
					indexCount: (uint)axesIndices.Length,
					instanceCount: 1,
					indexStart: 0,
					vertexOffset: 0,
					instanceStart: 0);
			}

			if ((LinesVertexBuffer != null) && (ovpSettings.showDrawn))
			{
				lock (LinesVertexBuffer)
				{
					CommandList.SetVertexBuffer(0, LinesVertexBuffer);
					CommandList.SetPipeline(LinesPipeline);
					CommandList.SetGraphicsResourceSet(0, ViewMatrixSet);
					CommandList.SetGraphicsResourceSet(1, ModelMatrixSet);

					for (int l = 0; l < lineVertexCount.Length; l++)
					{
						CommandList.Draw(lineVertexCount[l], 1, lineFirst[l], 0);
					}
				}
			}

			if (ovpSettings.enableFilledPolys)
			{
				if (TessVertexBuffer != null)
				{
					CommandList.SetVertexBuffer(0, TessVertexBuffer);
					CommandList.SetPipeline(FilledPipeline);
					CommandList.SetGraphicsResourceSet(0, ViewMatrixSet);
					CommandList.SetGraphicsResourceSet(1, ModelMatrixSet);

					for (int l = 0; l < tessVertexCount.Length; l++)
					{
						CommandList.Draw(tessVertexCount[l], 1, tessFirst[l], 0);
					}
				}
			}

			if (PolysVertexBuffer != null)
			{
				CommandList.SetVertexBuffer(0, PolysVertexBuffer);
				CommandList.SetPipeline(LinesPipeline);
				CommandList.SetGraphicsResourceSet(0, ViewMatrixSet);
				CommandList.SetGraphicsResourceSet(1, ModelMatrixSet);

				for (int l = 0; l < polyVertexCount.Length; l++)
				{
					CommandList.Draw(polyVertexCount[l], 1, polyFirst[l], 0);
				}
			}

			if (ovpSettings.drawPoints)
			{
				if (PointsVertexBuffer != null)
				{
					CommandList.SetVertexBuffer(0, PointsVertexBuffer);
					CommandList.SetPipeline(FilledPipeline);
					CommandList.SetGraphicsResourceSet(0, ViewMatrixSet);
					CommandList.SetGraphicsResourceSet(1, ModelMatrixSet);

					for (int l = 0; l < pointsFirst.Length; l++)
					{
						CommandList.Draw(3, 1, pointsFirst[l], 0);
					}
				}
			}
			CommandList.End();

			try
			{
				lock (CommandList)
				{
					Surface.GraphicsDevice.SubmitCommands(CommandList);
				}
				Surface.GraphicsDevice.SwapBuffers(Surface.Swapchain);
			}
			catch (Exception)
			{

			}
		}

		public void SetUpVeldrid()
		{
			CreateResources();

			Ready = true;
		}

		private void CreateResources()
		{
			ResourceFactory factory = Surface.GraphicsDevice.ResourceFactory;

			// Veldrid.SPIRV as of 1.0.12 uses "vdspv_X_Y" as the naming scheme
			// for uniform blocks, where X is the set and Y is the binding, as
			// defined in your GLSL shader code.
			ResourceLayout viewMatrixLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription(
						"vdspv_0_0", // "ViewMatrix",
						ResourceKind.UniformBuffer,
						ShaderStages.Vertex)));

			ViewBuffer = factory.CreateBuffer(
				new BufferDescription(64, BufferUsage.UniformBuffer));

			ViewMatrixSet = factory.CreateResourceSet(new ResourceSetDescription(
				viewMatrixLayout, ViewBuffer));

			// Veldrid.SPIRV as of 1.0.12 uses "vdspv_X_Y" as the naming scheme
			// for uniform blocks, where X is the set and Y is the binding, as
			// defined in your GLSL shader code.
			ResourceLayout modelMatrixLayout = factory.CreateResourceLayout(
				new ResourceLayoutDescription(
					new ResourceLayoutElementDescription(
						"vdspv_1_0", // "ModelMatrix",
						ResourceKind.UniformBuffer,
						ShaderStages.Vertex)));

			ModelBuffer = factory.CreateBuffer(
				new BufferDescription(64, BufferUsage.UniformBuffer));

			ModelMatrixSet = factory.CreateResourceSet(new ResourceSetDescription(
				modelMatrixLayout, ModelBuffer));

			drawGrid();

			// Veldrid.SPIRV, when cross-compiling to HLSL, will always produce
			// TEXCOORD semantics; VertexElementSemantic.TextureCoordinate thus
			// becomes necessary to let D3D11 work alongside Vulkan and OpenGL.
			//
			//   https://github.com/mellinoe/veldrid/issues/121
			//
			var vertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
				new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));

			// Veldrid.SPIRV is an additional library that complements Veldrid
			// by simplifying the development of cross-backend shaders, and is
			// currently the recommended approach to doing so:
			//
			//   https://veldrid.dev/articles/portable-shaders.html
			//
			// If you decide against using it, you can try out Veldrid developer
			// mellinoe's other project, ShaderGen, or drive yourself crazy by
			// writing and maintaining custom shader code for each platform.
			byte[] vertexShaderSpirvBytes = LoadSpirvBytes(ShaderStages.Vertex);
			byte[] fragmentShaderSpirvBytes = LoadSpirvBytes(ShaderStages.Fragment);

			var options = new CrossCompileOptions();
			switch (Surface.GraphicsDevice.BackendType)
			{
				// InvertVertexOutputY and FixClipSpaceZ address two major
				// differences between Veldrid's various graphics APIs, as
				// discussed here:
				//
				//   https://veldrid.dev/articles/backend-differences.html
				//
				// Note that the only reason those options are useful in this
				// example project is that the vertices being drawn are stored
				// the way Vulkan stores vertex data. The options will therefore
				// properly convert from the Vulkan style to whatever's used by
				// the destination backend. If you store vertices in a different
				// coordinate system, these may not do anything for you, and
				// you'll need to handle the difference in your shader code.
				case GraphicsBackend.Metal:
					options.InvertVertexOutputY = true;
					break;
				case GraphicsBackend.Direct3D11:
					options.InvertVertexOutputY = true;
					break;
				case GraphicsBackend.OpenGL:
					options.FixClipSpaceZ = true;
					options.InvertVertexOutputY = true;
					break;
				default:
					break;
			}

			var vertex = new ShaderDescription(ShaderStages.Vertex, vertexShaderSpirvBytes, "main", true);
			var fragment = new ShaderDescription(ShaderStages.Fragment, fragmentShaderSpirvBytes, "main", true);
			Shader[] shaders = factory.CreateFromSpirv(vertex, fragment, options);

			PointsPipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription
			{
				BlendState = BlendStateDescription.SingleOverrideBlend,
				DepthStencilState = new DepthStencilStateDescription(
					depthTestEnabled: false,
					depthWriteEnabled: false,
					comparisonKind: ComparisonKind.LessEqual),
				RasterizerState = new RasterizerStateDescription(
					cullMode: FaceCullMode.None,
					fillMode: PolygonFillMode.Solid,
					frontFace: FrontFace.Clockwise,
					depthClipEnabled: false,
					scissorTestEnabled: false),
				PrimitiveTopology = PrimitiveTopology.LineStrip,
				ResourceLayouts = new[] { viewMatrixLayout, modelMatrixLayout },
				ShaderSet = new ShaderSetDescription(
					vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
					shaders: shaders),
				Outputs = Surface.Swapchain.Framebuffer.OutputDescription
			});

			LinePipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription
			{
				BlendState = BlendStateDescription.SingleOverrideBlend,
				DepthStencilState = new DepthStencilStateDescription(
					depthTestEnabled: true,
					depthWriteEnabled: true,
					comparisonKind: ComparisonKind.LessEqual),
				RasterizerState = new RasterizerStateDescription(
					cullMode: FaceCullMode.Back,
					fillMode: PolygonFillMode.Solid,
					frontFace: FrontFace.Clockwise,
					depthClipEnabled: true,
					scissorTestEnabled: false),
				PrimitiveTopology = PrimitiveTopology.LineList,
				ResourceLayouts = new[] { viewMatrixLayout, modelMatrixLayout },
				ShaderSet = new ShaderSetDescription(
					vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
					shaders: shaders),
				Outputs = Surface.Swapchain.Framebuffer.OutputDescription
			});

			LinesPipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription
			{
				BlendState = BlendStateDescription.SingleOverrideBlend,
				DepthStencilState = new DepthStencilStateDescription(
					depthTestEnabled: false,
					depthWriteEnabled: false,
					comparisonKind: ComparisonKind.LessEqual),
				RasterizerState = new RasterizerStateDescription(
					cullMode: FaceCullMode.Back,
					fillMode: PolygonFillMode.Solid,
					frontFace: FrontFace.Clockwise,
					depthClipEnabled: false,
					scissorTestEnabled: false),
				PrimitiveTopology = PrimitiveTopology.LineStrip,
				ResourceLayouts = new[] { viewMatrixLayout, modelMatrixLayout },
				ShaderSet = new ShaderSetDescription(
					vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
					shaders: shaders),
				Outputs = Surface.Swapchain.Framebuffer.OutputDescription
			});

			FilledPipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription
			{
				BlendState = BlendStateDescription.SingleAlphaBlend,
				DepthStencilState = new DepthStencilStateDescription(
					depthTestEnabled: false,
					depthWriteEnabled: false,
					comparisonKind: ComparisonKind.LessEqual),
				RasterizerState = new RasterizerStateDescription(
					cullMode: FaceCullMode.None,
					fillMode: PolygonFillMode.Solid,
					frontFace: FrontFace.CounterClockwise,
					depthClipEnabled: false,
					scissorTestEnabled: false),
				PrimitiveTopology = PrimitiveTopology.TriangleStrip,
				ResourceLayouts = new[] { viewMatrixLayout, modelMatrixLayout },
				ShaderSet = new ShaderSetDescription(
					vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
					shaders: shaders),
				Outputs = Surface.Swapchain.Framebuffer.OutputDescription
			});

			CommandList = factory.CreateCommandList();
		}

		private byte[] LoadSpirvBytes(ShaderStages stage)
		{
			byte[] bytes;

			string name = $"VertexColor-{stage.ToString().ToLower()}.450.glsl";
			string full = Path.Combine(ExecutableDirectory, ShaderSubdirectory, name);

			// Precompiled SPIR-V bytecode can speed up program start by saving
			// the need to load text files and compile them before converting
			// the result to the final backend shader format. If they're not
			// available, though, the plain .glsl files will do just fine. Look
			// up glslangValidator to learn how to compile SPIR-V binary files.
			try
			{
				bytes = File.ReadAllBytes($"{full}.spv");
			}
			catch (FileNotFoundException)
			{
				bytes = File.ReadAllBytes(full);
			}

			return bytes;
		}
	}
}
