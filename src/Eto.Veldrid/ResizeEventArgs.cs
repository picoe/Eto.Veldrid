using Eto.Drawing;
using System;

namespace Eto.Veldrid
{
	public class ResizeEventArgs : EventArgs
	{
		public int Width { get; set; }
		public int Height { get; set; }

		public ResizeEventArgs()
		{
		}
		public ResizeEventArgs(int width, int height)
		{
			Width = width;
			Height = height;
		}
		public ResizeEventArgs(Size size)
		{
			Width = size.Width;
			Height = size.Height;
		}
	}
}
