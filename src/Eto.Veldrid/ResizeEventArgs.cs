using Eto.Drawing;
using System;

namespace Eto.Veldrid
{
	public class InitializeEventArgs : ResizeEventArgs
	{
		public InitializeEventArgs(Size size) : base(size)
		{
		}
	}

	public class ResizeEventArgs : EventArgs
	{
		public Size Size { get; }
		public int Width => Size.Width;
		public int Height => Size.Height;

		public ResizeEventArgs(Size size)
		{
			Size = size;
		}
	}
}
