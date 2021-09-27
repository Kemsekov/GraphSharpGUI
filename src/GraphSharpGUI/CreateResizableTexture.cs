using System.IO;
using System.Numerics;
using SampleBase;
using Veldrid;
using Veldrid.SPIRV;

namespace AutoPipeline
{
    public class CreateResizableTexture : DrawUnit, IComputeTargetTextureView
    {
        protected DeviceBuffer _screenSizeBuffer;
        public DeviceBuffer ScreenSizeBuffer=>_screenSizeBuffer;
        protected Shader _fillImageShader;
        protected ResourceLayout _fillImageLayout;
        protected Pipeline _computeFillImagePipeline;
        protected ResourceSet _computeFillImageResourceSet;
        protected CommandList _cl;
        public TextureView ComputeTargetTextureView=>_computeTargetTextureView;
        private Texture _computeTargetTexture;
        private TextureView _computeTargetTextureView;
        //private ResourceLayout _graphicsLayout;
        private float _ticks;
        private uint _computeTexSize = 2048;
        private byte[] _compute_shader;

        public CreateResizableTexture(DrawUnitManager manager, byte[] shader)  : base(manager)
        {
            _compute_shader = shader;
        }
        protected override void CreateResources(ResourceFactory factory)
        {
            _screenSizeBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
        
            _fillImageShader = factory.CreateFromSpirv(
                new ShaderDescription(
                    ShaderStages.Compute,
                    _compute_shader,
                    "main"
                )
            );
            _fillImageLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                new ResourceLayoutElementDescription("ScreenSizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)
                )
            );
            ComputePipelineDescription computeFillImage = new ComputePipelineDescription(
                _fillImageShader,
                _fillImageLayout,
                16,16,1
            );
            
            _computeFillImagePipeline = factory.CreateComputePipeline(ref computeFillImage);

            _cl = factory.CreateCommandList();

            CreateWindowSizedResources(factory);
        }

        private void CreateWindowSizedResources(ResourceFactory factory)
        {
            _computeTargetTexture?.Dispose();
            _computeTargetTextureView?.Dispose();
            _computeFillImageResourceSet?.Dispose();
            if(_computeTargetTexture==null)
                _computeTargetTexture = factory.CreateTexture(TextureDescription.Texture2D(
                    _computeTexSize,
                    _computeTexSize,
                    1,
                    1,
                    PixelFormat.R32_G32_B32_A32_Float,
                    TextureUsage.Sampled | TextureUsage.Storage));
            else
                _computeTargetTexture = factory.CreateTexture(TextureDescription.Texture2D(
                    this.Manager.Window.Width,
                    this.Manager.Window.Height,
                    1,
                    1,
                    PixelFormat.R32_G32_B32_A32_Float,
                    TextureUsage.Sampled | TextureUsage.Storage));
            
            _computeTargetTextureView = factory.CreateTextureView(_computeTargetTexture);

            _computeFillImageResourceSet = factory.CreateResourceSet(
                new ResourceSetDescription(
                    _fillImageLayout,
                    _computeTargetTextureView,
                    _screenSizeBuffer
                )
            );

        }

        protected override void HandleWindowResize()
        {
            base.HandleWindowResize();
            GraphicsDevice.UpdateBuffer(_screenSizeBuffer, 0, new Vector4(Manager.Window.Width, Manager.Window.Height, 0, 0));
            CreateWindowSizedResources(ResourceFactory);
        }

        protected override void IssueDraw(float deltaSeconds, CommandList cl)
        {
            _ticks += deltaSeconds * 1000f;
            
            cl.SetPipeline(_computeFillImagePipeline);
            cl.SetComputeResourceSet(0, _computeFillImageResourceSet);
            cl.Dispatch(_computeTexSize/16, _computeTexSize/16, 1);

        }
    }
}