using Eto.Forms;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Eto.VeldridSurface
{
    public class VeldridDriver
    {
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
            GraphicsDevice.MainSwapchain.Resize(width, height);
        }

        public void Draw()
        {
            if (!Ready)
            {
                return;
            }

            CommandList.Begin();
            CommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
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
            GraphicsDevice.SwapBuffers();
        }

        public void SetUpVeldrid()
        {
            //GraphicsDevice = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
            //GraphicsDevice = GraphicsDevice.CreateOpenGL(new GraphicsDeviceOptions(), new Veldrid.OpenGL.OpenGLPlatformInfo(), Width, Height);
            //Swapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(new SwapchainDescription(SwapchainSource, (uint)Width, (uint)Height, null, false));

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
                Outputs = GraphicsDevice.SwapchainFramebuffer.OutputDescription
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












    [Handler(typeof(IMyCustomControl))]
    public class MyCustomControl : Drawable
    {
        new IMyCustomControl Handler { get { return (IMyCustomControl)base.Handler; } }

        public string MyProperty
        {
            get { return Handler.MyProperty; }
            set { Handler.MyProperty = value; }
        }

        public interface IMyCustomControl : Control.IHandler
        {
            string MyProperty { get; set; }
        }
    }

    public class SchmeldridSurface : MyCustomControl
    {
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

        public SchmeldridSurface()
        {
            BackgroundColor = Eto.Drawing.Colors.Red;

            Clock.Interval = 1.0f / 60.0f;
            Clock.Elapsed += Clock_Elapsed;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            //base.OnSizeChanged(e);

            


            Draw();
        }

        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    //base.OnPaint(e);

        //    Draw();
        //}

        private void Clock_Elapsed(object sender, EventArgs e)
        {
            Draw();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            SetUpVeldrid();

            Clock.Start();
        }

        public void Draw()
        {
            if (!Ready)
            {
                return;
            }

            Swapchain.Resize((uint)Width, (uint)Height);

            CommandList.Begin();
            CommandList.SetFramebuffer(Swapchain.Framebuffer);
            CommandList.ClearColorTarget(0, RgbaFloat.Black);
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
            //var window = new Sdl2Window(NativeHandle, false);

            //Farbarbus = new Sdl2Window("FLAAAAAAHbeb", 0, 0, 640, 480, SDL_WindowFlags.OpenGL | SDL_WindowFlags.Borderless | SDL_WindowFlags.Hidden, false);
            //GraphicsDevice = VeldridStartup.CreateGraphicsDevice(Farbarbus, GraphicsBackend.OpenGL);

            //var fump = new Eto.Forms.Panel()


            //GraphicsDevice = VeldridStartup.CreateGraphicsDevice(Sdl2Window, GraphicsBackend.Direct3D11);
            //GraphicsDevice = VeldridStartup.CreateGraphicsDevice(window, GraphicsBackend.OpenGL);

            //            var flags = Sdl2Native.SDL_GetWindowFlags(window.SdlWindowHandle);

            //GraphicsDevice = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
            //GraphicsDevice = GraphicsDevice.CreateOpenGL(new GraphicsDeviceOptions(), new Veldrid.OpenGL.OpenGLPlatformInfo(), Width, Height);
            //Swapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(new SwapchainDescription(SwapchainSource, (uint)Width, (uint)Height, null, false));

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

            //Pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription
            //{
            //    BlendState = BlendStateDescription.SingleOverrideBlend,
            //    DepthStencilState = new DepthStencilStateDescription(
            //        depthTestEnabled: true,
            //        depthWriteEnabled: true,
            //        comparisonKind: ComparisonKind.LessEqual),
            //        RasterizerState = new RasterizerStateDescription(
            //        cullMode: FaceCullMode.Back,
            //        fillMode: PolygonFillMode.Solid,
            //        frontFace: FrontFace.Clockwise,
            //        depthClipEnabled: true,
            //        scissorTestEnabled: false),
            //    PrimitiveTopology = PrimitiveTopology.TriangleStrip,
            //    ResourceLayouts = Array.Empty<ResourceLayout>(),
            //    ShaderSet = new ShaderSetDescription(
            //        vertexLayouts: new VertexLayoutDescription[] { vertexLayout },
            //        shaders: new Shader[] { VertexShader, FragmentShader }),
            //    Outputs = Swapchain.Framebuffer.OutputDescription
            //});

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




    
    public class VeldridSurface : Drawable
	{
        public UITimer Clock = new UITimer();

        public SwapchainSource SwapchainSource { get; set; }
        public Swapchain Swapchain { get; set; }

        private GraphicsDevice GraphicsDevice;
        private CommandList CommandList;
		private DeviceBuffer VertexBuffer;
		private DeviceBuffer IndexBuffer;
		private Shader VertexShader;
		private Shader FragmentShader;
		private Pipeline Pipeline;

        private bool Ready = false;

        private Action<Form, VeldridSurface> Action;

		public VeldridSurface(Action<Form, VeldridSurface> action)
		{
            Action = action;

			BackgroundColor = Eto.Drawing.Colors.Red;

			Clock.Interval = 1.0f / 60.0f;
			Clock.Elapsed += Clock_Elapsed;
		}

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (Sdl2Window != null)
            {
                Sdl2Window.PumpEvents();
                Sdl2Window.X = (int)PointToScreen(Location).X;
                Sdl2Window.Y = (int)PointToScreen(Location).Y;
                Sdl2Window.Width = Width + 356;
                Sdl2Window.Height = Height + 128;
                

                //Sdl2Window.Visible = true;

                //Action.Invoke(Parent as Form, this);
                //Sdl2Native.SDL_SetWindowSize(Sdl2Window.SdlWindowHandle, Width, Height);
            }

            

            Draw();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Draw();
        }

        private void Clock_Elapsed(object sender, EventArgs e)
        {
            Draw();
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			SetUpVeldrid();

			Clock.Start();
		}

		public void Draw()
		{
            if (!Ready)
            {
                return;
            }

            GraphicsDevice.MainSwapchain.Resize((uint)Width, (uint)Height);

            CommandList.Begin();
			CommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
			CommandList.ClearColorTarget(0, RgbaFloat.Black);
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
			GraphicsDevice.SwapBuffers();
		}

        public Sdl2Window Farbarbus;

        public Sdl2Window Sdl2Window = new Sdl2Window("FLAAAAAAHbeb", 0, 0, 320, 240, SDL_WindowFlags.OpenGL | SDL_WindowFlags.Borderless, false);

        public void SetUpVeldrid()
		{
            var window = new Sdl2Window(NativeHandle, false);

            //Farbarbus = new Sdl2Window("FLAAAAAAHbeb", 0, 0, 640, 480, SDL_WindowFlags.OpenGL | SDL_WindowFlags.Borderless | SDL_WindowFlags.Hidden, false);
            //GraphicsDevice = VeldridStartup.CreateGraphicsDevice(Farbarbus, GraphicsBackend.OpenGL);



            //Action.Invoke(Parent as Form, this);

            //GraphicsDevice = VeldridStartup.CreateGraphicsDevice(Sdl2Window, GraphicsBackend.Direct3D11);
            GraphicsDevice = VeldridStartup.CreateGraphicsDevice(Sdl2Window, GraphicsBackend.OpenGL);

                        //var flags = Sdl2Native.SDL_GetWindowFlags(window.SdlWindowHandle);

            //GraphicsDevice = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
            //GraphicsDevice = GraphicsDevice.CreateOpenGL(new GraphicsDeviceOptions(), new Veldrid.OpenGL.OpenGLPlatformInfo(), Width, Height);
            //Swapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(new SwapchainDescription(SwapchainSource, (uint)Width, (uint)Height, null, false));

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
                Outputs = GraphicsDevice.SwapchainFramebuffer.OutputDescription
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
