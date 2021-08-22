using System;
using Silk.NET.OpenGL;

namespace CoreBoy.Silk.Util
{
    public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged
    {
        public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
        {
            this.gl = gl;
            this.bufferType = bufferType;

            handle = this.gl.GenBuffer();
            Bind();
            
            fixed (void* d = data)
            {
                this.gl.BufferData(bufferType, (nuint) (data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
            }
        }

        public void Bind()
        {
            gl.BindBuffer(bufferType, handle);
        }

        public void Dispose()
        {
            gl.DeleteBuffer(handle);
        }
        
        private readonly uint handle;
        private readonly BufferTargetARB bufferType;
        private readonly GL gl;
    }
}