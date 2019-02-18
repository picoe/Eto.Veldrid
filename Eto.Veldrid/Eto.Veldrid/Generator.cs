using System;
using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.StartupUtilities;

namespace Eto.VeldridSurface
{
	public class Generator
	{
		public Framebuffer OffscreenBuffer;

		private GraphicsDevice _graphicsDevice;

		private CommandList _commandList;
		private DeviceBuffer _vertexBuffer;
		private DeviceBuffer _indexBuffer;
		private Shader _vertexShader;
		private Shader _fragmentShader;
		private Pipeline _pipeline;

		public Generator()
		{

			//CreateResources();
		}

		public void Draw()
		{
			_commandList.Begin();
			_commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
			_commandList.ClearColorTarget(0, RgbaFloat.Black);
			_commandList.SetVertexBuffer(0, _vertexBuffer);
			_commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
			_commandList.SetPipeline(_pipeline);
			_commandList.DrawIndexed(
				indexCount: 4,
				instanceCount: 1,
				indexStart: 0,
				vertexOffset: 0,
				instanceStart: 0);
			_commandList.End();
			_graphicsDevice.SubmitCommands(_commandList);
			_graphicsDevice.SwapBuffers();
		}

		//static void CreateResources()
		//{
		//	ResourceFactory factory = _graphicsDevice.ResourceFactory;

		//	VertexPositionColor[] quadVertices =
		//	{
		//		new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Red),
		//		new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Green),
		//		new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Blue),
		//		new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Yellow)
		//	};

		//	ushort[] quadIndices = { 0, 1, 2, 3 };

		//	_vertexBuffer = factory.CreateBuffer(new BufferDescription(4 * VertexPositionColor.SizeInBytes, BufferUsage.VertexBuffer));
		//	_indexBuffer = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));

		//	_graphicsDevice.UpdateBuffer(_vertexBuffer, 0, quadVertices);
		//	_graphicsDevice.UpdateBuffer(_indexBuffer, 0, quadIndices);

		//	var vertexLayout = new VertexLayoutDescription(
		//		new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
		//		new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));

		//	_vertexShader = LoadShader(ShaderStages.Vertex);
		//	_fragmentShader = LoadShader(ShaderStages.Fragment);

		//	var pipelineDescription = new GraphicsPipelineDescription();
		//	pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
		//	pipelineDescription.DepthStencilState = new DepthStencilStateDescription(
		//		depthTestEnabled: true,
		//		depthWriteEnabled: true,
		//		comparisonKind: ComparisonKind.LessEqual);
		//	pipelineDescription.RasterizerState = new RasterizerStateDescription(
		//		cullMode: FaceCullMode.Back,
		//		fillMode: PolygonFillMode.Solid,
		//		frontFace: FrontFace.Clockwise,
		//		depthClipEnabled: true,
		//		scissorTestEnabled: false);
		//	pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
		//	pipelineDescription.ResourceLayouts = System.Array.Empty<ResourceLayout>();
		//	pipelineDescription.ShaderSet = new ShaderSetDescription(
		//		vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
		//		shaders: new Shader[] { _vertexShader, _fragmentShader });
		//	pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;

		//	_pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

		//	_commandList = factory.CreateCommandList();
		//}

		//static void DisposeResources()
		//{
		//	_pipeline.Dispose();
		//	_vertexShader.Dispose();
		//	_fragmentShader.Dispose();
		//	_commandList.Dispose();
		//	_vertexBuffer.Dispose();
		//	_indexBuffer.Dispose();
		//	_graphicsDevice.Dispose();
		//}

		//private static Shader LoadShader(ShaderStages stage)
		//{
		//	string extension = null;

		//	switch (_graphicsDevice.BackendType)
		//	{
		//		case GraphicsBackend.Direct3D11:
		//			extension = "hlsl.bytes";
		//			break;
		//		case GraphicsBackend.Vulkan:
		//			extension = "spv";
		//			break;
		//		case GraphicsBackend.OpenGL:
		//			extension = "glsl";
		//			break;
		//		case GraphicsBackend.Metal:
		//			extension = "metallib";
		//			break;
		//		default:
		//			throw new System.InvalidOperationException();
		//	}

		//	string entryPoint = stage == ShaderStages.Vertex ? "VS" : "FS";
		//	string path = Path.Combine(System.AppContext.BaseDirectory, "Shaders", $"{stage.ToString()}.{extension}");
		//	byte[] shaderBytes = File.ReadAllBytes(path);

		//	return _graphicsDevice.ResourceFactory.CreateShader(new ShaderDescription(stage, shaderBytes, entryPoint));
		//}
	}

	struct VertexPositionColor
	{
		public Vector2 Position;
		public RgbaFloat Color;
		public VertexPositionColor(Vector2 position, RgbaFloat color)
		{
			Position = position;
			Color = color;
		}
		public const uint SizeInBytes = 24;
	}
}
