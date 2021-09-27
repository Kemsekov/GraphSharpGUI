using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using AutoPipeline;
using SampleBase;
using Veldrid;

namespace GraphSharpGUI
{
    public class DrawNodesOnTexture : DrawUnit, IComputeTargetTextureView
    {
        private IComputeTargetTextureView _textureView;
        private byte[] _shader;
        private int[] _nodesId;
        private Vector4 _shiftImage = new(0,1,0,0);
        private NodeInfo[] _nodesInfo;
        public DeviceBuffer NodesInfoBuffer => _nodesInfoBuffer;
        public DeviceBuffer NodesIdBuffer => _nodesIdBuffer;

        private DeviceBuffer _nodesInfoBuffer;
        private DeviceBuffer _nodesIdBuffer;
        private DeviceBuffer _shiftImageBuffer;
        private ResourceLayout _layout;
        private Pipeline _pipeline;
        private CommandList _cl;
        private ResourceSet _resourceSet;

        public DrawNodesOnTexture(IComputeTargetTextureView textureView,byte[] shader, NodeInfo[] nodeInfos, int[] nodeIds) : base(textureView.Manager)
        {
            _textureView = textureView;
            _shader = shader;
            _nodesId = nodeIds;
            _nodesInfo = nodeInfos;
        }

        public DeviceBuffer ScreenSizeBuffer => _textureView.ScreenSizeBuffer;

        public TextureView ComputeTargetTextureView => _textureView.ComputeTargetTextureView;

        protected override void CreateResources(ResourceFactory factory)
        {
            _nodesInfoBuffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint)(_nodesInfo.Length*Unsafe.SizeOf<NodeInfo>()),
                    BufferUsage.StructuredBufferReadWrite,
                    (uint)(Unsafe.SizeOf<NodeInfo>())
                )
            );
            _nodesIdBuffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint)(_nodesId.Length*sizeof(int)),
                    BufferUsage.StructuredBufferReadOnly,
                    sizeof(int)
                )
            );
            
            _shiftImageBuffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint)sizeof(float)*4,
                    BufferUsage.UniformBuffer
                )
            );

            var shader = factory.CreateShader(
                new ShaderDescription(
                    ShaderStages.Compute,
                    _shader,
                    "main"
                )
            );
            _layout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription(
                        "Tex", 
                        ResourceKind.TextureReadWrite, 
                        ShaderStages.Compute),
                    new ResourceLayoutElementDescription(
                        "ScreenSizeBuffer", 
                        ResourceKind.UniformBuffer, 
                        ShaderStages.Compute),
                    new ResourceLayoutElementDescription(
                        "NodesInfo",
                        ResourceKind.StructuredBufferReadWrite,
                        ShaderStages.Compute
                    ),
                    new ResourceLayoutElementDescription(
                        "NodesId",
                        ResourceKind.StructuredBufferReadOnly,
                        ShaderStages.Compute
                    ),
                    new ResourceLayoutElementDescription(
                        "ShiftImage",
                        ResourceKind.UniformBuffer,
                        ShaderStages.Compute
                    )
                )
            );
            var pipelineDescription = new ComputePipelineDescription(
                shader,
                _layout,
                1,1,1
            );
            _pipeline = factory.CreateComputePipeline(ref pipelineDescription);
            _cl = factory.CreateCommandList();

            UpdateResources(factory);
        }

        private void UpdateResources(ResourceFactory factory)
        {
            lock(_cl){
            _resourceSet?.Dispose();
            _cl.Begin();
            _cl.UpdateBuffer(_nodesIdBuffer,0,_nodesId);
            _cl.UpdateBuffer(_nodesInfoBuffer,0,_nodesInfo);
            _cl.UpdateBuffer(_shiftImageBuffer,0,_shiftImage);
            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.WaitForIdle();
            _resourceSet = factory.CreateResourceSet(
                new ResourceSetDescription(
                    _layout,
                    this.ComputeTargetTextureView,
                    this.ScreenSizeBuffer,
                    this._nodesInfoBuffer,
                    this._nodesIdBuffer,
                    this._shiftImageBuffer
                )
            );
            }
        }
        protected override void HandleWindowResize()
        {
            base.HandleWindowResize();
            UpdateResources(ResourceFactory);
        }
        public void UpdateResources(NodeInfo[] nodesInfo, int[] nodesId){
            _nodesId = nodesId;
            _nodesInfo = nodesInfo;
            UpdateResources(ResourceFactory);
        }
        public void UpdateResources(){
            UpdateResources(ResourceFactory);
        }
        protected override void IssueDraw(float deltaSeconds, CommandList cl)
        {
            lock(_cl){
            cl.SetPipeline(_pipeline);
            cl.SetComputeResourceSet(0,_resourceSet);
            cl.Dispatch((uint)_nodesInfo.Length,1,1);
            }
        }
        protected override void OnKeyDown(KeyEvent key)
        {
            base.OnKeyDown(key);
            bool need_to_update = true;
            switch(key.Key){
                case Key.Left:
                    _shiftImage.Z+=0.1f;
                break;
                case Key.Right:
                    _shiftImage.Z-=0.1f;
                break;
                case Key.Down:
                    _shiftImage.W-=0.1f;
                break;
                case Key.Up:
                    _shiftImage.W+=0.1f;
                break;
                case Key.Plus:
                    //if you mult by k, then you move by k/2
                    _shiftImage.Y+=0.5f;
                    //_shiftImage.Z-=MathF.Log(_shiftImage.Y,81f/16f);;
                    //_shiftImage.W-=MathF.Log(_shiftImage.Y,81f/16f);;
                
                    //shiftImage.W*=_shiftImage.Y/2;
                    //shiftImage.Z*=_shiftImage.Y/2;
                break;
                case Key.Minus:
                    _shiftImage.Y-=0.5f;
                    //_shiftImage.W/=_shiftImage.Y/2;
                    //_shiftImage.Z/=_shiftImage.Y/2;
                break;
                case Key.R:
                    _shiftImage = new Vector4(0,1,0,0);    
                break;
                default :
                    need_to_update = false;
                break;
            }
            if(need_to_update)
                UpdateResources(ResourceFactory);
        }
    }
}