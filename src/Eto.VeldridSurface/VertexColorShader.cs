using ShaderGen;
using System.Numerics;

[assembly: ShaderSet("VertexColor", "Eto.VeldridSurface.VertexColor.VS", "Eto.VeldridSurface.VertexColor.FS")]

namespace Eto.VeldridSurface
{
	public struct VertexInput
	{
		[PositionSemantic]
		public Vector3 Position;

		[ColorSemantic]
		public Vector4 Color;
	}

	public struct FragmentInput
	{
		[SystemPositionSemantic]
		public Vector4 Position;

		[ColorSemantic]
		public Vector4 Color;
	}

	public class VertexColor
	{
		[VertexShader]
		public FragmentInput VS(VertexInput input)
		{
			FragmentInput output;
			output.Position = new Vector4(input.Position, 1.0f);
			output.Color = input.Color;

			return output;
		}

		[FragmentShader]
		public Vector4 FS(FragmentInput input)
		{
			return input.Color;
		}
	}
}
