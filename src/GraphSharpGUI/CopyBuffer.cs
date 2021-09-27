using System;
using System.Drawing;
using AutoPipeline;
using Veldrid;

namespace GraphSharpGUI
{
    public class CopyBuffer : DrawUnit
    {
        private DeviceBuffer _buffer;

        public DeviceBuffer CopyFrom => _copyFromFunc();

        private Func<DeviceBuffer> _copyFromFunc;

        public int SizeInBytes { get; }
        public CopyBuffer(DrawUnitManager manager,Func<DeviceBuffer> copyFrom,int sizeInBytes) : base(manager)
        {
            _copyFromFunc = copyFrom;
            SizeInBytes = sizeInBytes;
        }

        protected override void CreateResources(ResourceFactory factory)
        {
            _buffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint)(SizeInBytes),
                    BufferUsage.Staging
                )
            );
        }

        private void ThrowIfResourceNull(){
            if(CopyFrom is null || _buffer is null) 
                throw new ArgumentException("Copy buffer or destination buffer is null!");
        }

        protected override void IssueDraw(float deltaSeconds, CommandList cl)
        {
            ThrowIfResourceNull();
            cl.CopyBuffer(CopyFrom,0,_buffer,0,(uint)(SizeInBytes));
        }
        public MappedResourceView<T> Map<T>() where T : unmanaged{
            ThrowIfResourceNull();
            return GraphicsDevice.Map<T>(_buffer,MapMode.ReadWrite);
        }
        public void Unmap(){
            ThrowIfResourceNull();
            GraphicsDevice.Unmap(_buffer);
        }
    }
}