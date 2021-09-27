using Veldrid;
using System;
using Veldrid.SPIRV;
using System.IO;
using System.Numerics;

namespace AutoPipeline
{
    public class DrawTexture : DrawUnit
    {
        private IComputeTargetTextureView _textureView;
        protected Pipeline _graphicsPipeline;
        protected ResourceSet _graphicsResourceSet;
        protected DeviceBuffer _vertexBuffer;
        protected DeviceBuffer _indexBuffer;
        private ResourceLayout _graphicsLayout;
        private CommandList _cl;
        private byte[] _vertex_shader;
        private byte[] _frag_shader;
        public DrawTexture(IComputeTargetTextureView textureView,byte[] vertex_shader, byte[] frag_shader) : base(textureView.Manager)
        {
            this._textureView = textureView;
            this._vertex_shader = vertex_shader;
            this._frag_shader = frag_shader;
        }

        protected override void CreateResources(ResourceFactory factory)
        {
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(16 * 4, BufferUsage.VertexBuffer));
            _indexBuffer = factory.CreateBuffer(new BufferDescription(2 * 6, BufferUsage.IndexBuffer));
            Shader[] shaders = factory.CreateFromSpirv(
                            new ShaderDescription(
                                ShaderStages.Vertex,
                                _vertex_shader,
                                "main"),
                            new ShaderDescription(
                                ShaderStages.Fragment,
                                _frag_shader,
                                "main"));

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new VertexLayoutDescription[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                shaders);

            _graphicsLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SS", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription fullScreenQuadDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { _graphicsLayout },
                MainSwapchain.Framebuffer.OutputDescription);

            _graphicsPipeline = factory.CreateGraphicsPipeline(ref fullScreenQuadDesc);
            _cl = factory.CreateCommandList();
            UpdateResources(factory);
            InitResources(factory);
        }
        private void UpdateResources(ResourceFactory factory){
            _graphicsResourceSet?.Dispose();
            _graphicsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _graphicsLayout,
                _textureView.ComputeTargetTextureView,
                GraphicsDevice.PointSampler));
        }
        private void InitResources(ResourceFactory factory)
        {
            _cl.Begin();
            Vector4[] quadVerts =
            {
                new Vector4(-1, 1, 0, 0),
                new Vector4(1, 1, 1, 0),
                new Vector4(1, -1, 1, 1),
                new Vector4(-1, -1, 0, 1),
            };

            ushort[] indices = { 0, 1, 2, 0, 2, 3 };

            _cl.UpdateBuffer(_vertexBuffer, 0, quadVerts);
            _cl.UpdateBuffer(_indexBuffer, 0, indices);

            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.WaitForIdle();
            
        }
        protected override void HandleWindowResize()
        {
            base.HandleWindowResize();
            UpdateResources(ResourceFactory);
        }
        protected override void IssueDraw(float deltaSeconds, CommandList cl)
        {
            cl.SetFramebuffer(MainSwapchain.Framebuffer);
            cl.SetFullViewports();
            cl.SetFullScissorRects();
            cl.ClearColorTarget(0, RgbaFloat.Black);
            cl.SetPipeline(_graphicsPipeline);
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetGraphicsResourceSet(0, _graphicsResourceSet);
        
            cl.DrawIndexed(6, 1, 0, 0, 0);
        }
    }
}