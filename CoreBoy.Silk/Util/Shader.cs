using System;
using System.IO;
using Silk.NET.OpenGL;

namespace CoreBoy.Silk.Util;

public class Shader : IDisposable
{
    public Shader(GL glIn, string vertexPath, string fragmentPath)
    {
        gl = glIn;

        var vertex = LoadShader(ShaderType.VertexShader, vertexPath);
        var fragment = LoadShader(ShaderType.FragmentShader, fragmentPath);
        handle = gl.CreateProgram();
            
        gl.AttachShader(handle, vertex);
        gl.AttachShader(handle, fragment);
            
        gl.LinkProgram(handle);
        gl.GetProgram(handle, GLEnum.LinkStatus, out var status);
            
        if (status == 0)
        {
            throw new Exception($"Program failed to link with error: {gl.GetProgramInfoLog(handle)}");
        }
            
        gl.DetachShader(handle, vertex);
        gl.DetachShader(handle, fragment);
            
        gl.DeleteShader(vertex);
        gl.DeleteShader(fragment);
    }

    public void Use()
    {
        gl.UseProgram(handle);
    }

    public void SetUniform(string name, int value)
    {
        int location = gl.GetUniformLocation(handle, name);
            
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
            
        gl.Uniform1(location, value);
    }

    public void SetUniform(string name, float value)
    {
        int location = gl.GetUniformLocation(handle, name);
            
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
            
        gl.Uniform1(location, value);
    }

    public void Dispose()
    {
        gl.DeleteProgram(handle);
    }

    private uint LoadShader(ShaderType type, string path)
    {
        var src = File.ReadAllText(path);
        var shaderHandle = gl.CreateShader(type);
            
        gl.ShaderSource(shaderHandle, src);
        gl.CompileShader(shaderHandle);
            
        string infoLog = gl.GetShaderInfoLog(shaderHandle);
            
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
        }

        return shaderHandle;
    }
        
    private readonly uint handle;
    private readonly GL gl;
}