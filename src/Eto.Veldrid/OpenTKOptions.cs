using OpenTK.Graphics;

namespace Eto.Veldrid
{
	public class OpenTKOptions
	{
		public GraphicsMode Mode { get; private set; } = new GraphicsMode();

		public int MajorVersion { get; private set; } = 1;
		public int MinorVersion { get; private set; } = 0;

		public GraphicsContextFlags Flags { get; private set; } = GraphicsContextFlags.Default;

		public OpenTKOptions()
		{
		}
		public OpenTKOptions(GraphicsMode mode)
		{
			Mode = mode;
		}
		public OpenTKOptions(int major, int minor)
		{
			MajorVersion = major;
			MinorVersion = minor;
		}
		public OpenTKOptions(GraphicsContextFlags flags)
		{
			Flags = flags;
		}
		public OpenTKOptions(GraphicsMode mode, int major, int minor, GraphicsContextFlags flags)
		{
			Mode = mode;
			MajorVersion = major;
			MinorVersion = minor;
			Flags = flags;
		}
	}
}
