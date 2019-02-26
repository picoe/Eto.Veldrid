using Eto.Forms;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace Eto.VeldridSurface
{
	public struct VertexPositionColor
	{
		public static uint SizeInBytes = (uint)Marshal.SizeOf(typeof(VertexPositionColor));

		public Vector2 Position;
		public RgbaFloat Color;

		public VertexPositionColor(Vector2 position, RgbaFloat color)
		{
			Position = position;
			Color = color;
		}
	}

	public class VeldridDriver
	{
		public VeldridSurface Surface;

		public UITimer Clock = new UITimer();

		public CommandList CommandList;
		public DeviceBuffer VertexBuffer;
		public DeviceBuffer IndexBuffer;
		public Shader VertexShader;
		public Shader FragmentShader;
		public Pipeline Pipeline;

		private bool Ready = false;

		public VeldridDriver()
		{
			Clock.Interval = 1.0f / 60.0f;
			Clock.Elapsed += Clock_Elapsed;
		}

		private void Clock_Elapsed(object sender, EventArgs e)
		{
			Draw();
		}

		public void Draw()
		{
			if (!Ready)
			{
				return;
			}

			CommandList.Begin();
			CommandList.SetFramebuffer(Surface.Swapchain.Framebuffer);
			CommandList.ClearColorTarget(0, RgbaFloat.Pink);
			CommandList.SetVertexBuffer(0, VertexBuffer);
			CommandList.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16);
			CommandList.SetPipeline(Pipeline);
			CommandList.DrawIndexed(
				indexCount: 4,
				instanceCount: 1,
				indexStart: 0,
				vertexOffset: 0,
				instanceStart: 0);
			CommandList.End();
			Surface.GraphicsDevice.SubmitCommands(CommandList);
			Surface.GraphicsDevice.SwapBuffers(Surface.Swapchain);
		}

		public void SetUpVeldrid()
		{
			CreateResources();

			Ready = true;
		}

		private void CreateResources()
		{
			ResourceFactory factory = Surface.GraphicsDevice.ResourceFactory;

			VertexPositionColor[] quadVertices =
			{
				new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Red),
				new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Green),
				new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Blue),
				new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Yellow)
			};

			ushort[] quadIndices = { 0, 1, 2, 3 };

			VertexBuffer = factory.CreateBuffer(new BufferDescription(4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
			IndexBuffer = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));

			Surface.GraphicsDevice.UpdateBuffer(VertexBuffer, 0, quadVertices);
			Surface.GraphicsDevice.UpdateBuffer(IndexBuffer, 0, quadIndices);

			var vertexLayout = new VertexLayoutDescription(
				new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
				new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));

			VertexShader = LoadShader(ShaderStages.Vertex);
			FragmentShader = LoadShader(ShaderStages.Fragment);

			Pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription
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
				PrimitiveTopology = PrimitiveTopology.TriangleStrip,
				ResourceLayouts = Array.Empty<ResourceLayout>(),
				ShaderSet = new ShaderSetDescription(
					vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
					shaders: new Shader[] { VertexShader, FragmentShader }),
				Outputs = Surface.Swapchain.Framebuffer.OutputDescription
			});

			CommandList = factory.CreateCommandList();
		}

		private Shader LoadShader(ShaderStages stage)
		{
			string extension = null;

			switch (Surface.GraphicsDevice.BackendType)
			{
				case GraphicsBackend.Metal:
					extension = "metallib";
					break;
				case GraphicsBackend.Vulkan:
					extension = "450.glsl.spv";
					break;
				case GraphicsBackend.Direct3D11:
					extension = "hlsl.bytes";
					break;
				case GraphicsBackend.OpenGL:
					extension = "330.glsl";
					break;
				case GraphicsBackend.OpenGLES:
					extension = "300.glsles";
					break;
				default:
					throw new InvalidOperationException();
			}

			string name = $"VertexColor-{stage.ToString().ToLower()}.{extension}";
			string path = Path.Combine(AppContext.BaseDirectory, "shaders", name);

			byte[] shaderBytes = File.ReadAllBytes(path);
			string entryPoint = stage == ShaderStages.Vertex ? "VS" : "FS";

			return Surface.GraphicsDevice.ResourceFactory.CreateShader(
				new ShaderDescription(stage, shaderBytes, entryPoint));
		}
	}
}
