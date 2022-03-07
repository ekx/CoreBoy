using System;
using Silk.NET.OpenGL;

namespace CoreBoy.Silk.Util;

public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
    where TVertexType : unmanaged
    where TIndexType : unmanaged
{
    public VertexArrayObject(GL glIn, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
    {
        gl = glIn;

        handle = gl.GenVertexArray();
        Bind();
            
        vbo.Bind();
        ebo.Bind();
    }

    public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
    {
        gl.VertexAttribPointer(index, count, type, false, vertexSize * (uint) sizeof(TVertexType), (void*) (offSet * sizeof(TVertexType)));
        gl.EnableVertexAttribArray(index);
    }

    public void Bind()
    {
        gl.BindVertexArray(handle);
    }

    public void Dispose()
    {
        gl.DeleteVertexArray(handle);
    }
        
    private readonly uint handle;
    private readonly GL gl;
}