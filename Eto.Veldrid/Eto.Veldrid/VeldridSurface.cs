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
        public static GraphicsBackend PreferredBackend
        {
            get
            {
                GraphicsBackend backend;
                if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal))
                {
                    backend = GraphicsBackend.Metal;
                }
                else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan))
                {
                    backend = GraphicsBackend.Vulkan;
                }
                else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11))
                {
                    backend = GraphicsBackend.Direct3D11;
                }
                else
                {
                    backend = GraphicsBackend.OpenGL;
                }

                return backend;
            }
        }

        public UITimer Clock = new UITimer();

        public SwapchainSource SwapchainSource { get; set; }
        public Swapchain Swapchain { get; set; }

        public GraphicsDevice GraphicsDevice;
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

        public void Resize(int width, int height)
        {
            if (width < 0 || height < 0)
            {
                return;
            }

            Resize((uint)width, (uint)height);
        }
        public void Resize(uint width, uint height)
        {
            Swapchain?.Resize(width, height);
        }

        public void Draw()
        {
            if (!Ready)
            {
                return;
            }

            CommandList.Begin();
            CommandList.SetFramebuffer(Swapchain.Framebuffer);
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
            GraphicsDevice.SubmitCommands(CommandList);
            GraphicsDevice.SwapBuffers(Swapchain);
        }

        public void SetUpVeldrid()
        {
            CreateResources();

            Ready = true;
        }

        private void CreateResources()
        {
            ResourceFactory factory = GraphicsDevice.ResourceFactory;

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

            GraphicsDevice.UpdateBuffer(VertexBuffer, 0, quadVertices);
            GraphicsDevice.UpdateBuffer(IndexBuffer, 0, quadIndices);

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
                Outputs = Swapchain.Framebuffer.OutputDescription
            });

            CommandList = factory.CreateCommandList();
        }

        private Shader LoadShader(ShaderStages stage)
        {
            string extension = null;

            switch (GraphicsDevice.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    extension = "hlsl.bytes";
                    break;
                case GraphicsBackend.Vulkan:
                    extension = "spv";
                    break;
                case GraphicsBackend.OpenGL:
                    extension = "glsl";
                    break;
                case GraphicsBackend.Metal:
                    extension = "metallib";
                    break;
                default:
                    throw new System.InvalidOperationException();
            }

            string entryPoint = stage == ShaderStages.Vertex ? "VS" : "FS";
            string path = Path.Combine(System.AppContext.BaseDirectory, "Shaders", $"{stage.ToString()}.{extension}");
            byte[] shaderBytes = File.ReadAllBytes(path);

            return GraphicsDevice.ResourceFactory.CreateShader(new ShaderDescription(stage, shaderBytes, entryPoint));
        }
    }
}
