using System;
using Silk.NET.OpenGL;

namespace CoreBoy.Silk.Util
{
    public class Texture : IDisposable
    {
        public unsafe Texture(GL glIn, Span<byte> data, uint width, uint height)
        {
            gl = glIn;
            
            if (data == null || data.Length <= 0)
            {
                return;
            }
            
            fixed (void* d = &data[0])
            {
                Load(d, width, height);
            }
        }

        private unsafe void Load(void* data, uint width, uint height)
        {
            //Generating the opengl handle;
            handle = gl.GenTexture();
            Bind();

            //Setting the data of a texture.
            gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            
            //Setting some texture parameters so the texture behaves as expected.
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);

            //Generating mipmaps.
            gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            //When we bind a texture we can choose which texture slot we can bind it to.
            gl.ActiveTexture(textureSlot);
            gl.BindTexture(TextureTarget.Texture2D, handle);
        }

        public void Dispose()
        {
            //In order to dispose we need to delete the opengl handle for the texture.
            gl.DeleteTexture(handle);
        }
        
        private uint handle;
        private readonly GL gl;
    }
}