using SampleBase;
using Veldrid;
using Vulkan.Xlib;

namespace AutoPipeline
{
    public interface IComputeTargetTextureView
    {
        DeviceBuffer ScreenSizeBuffer{get;}
        TextureView ComputeTargetTextureView{get;}
        DrawUnitManager Manager{get;}
    }
}