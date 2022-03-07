using System;
using System.IO;
using System.Threading;
using CoreBoy.Core;
using CoreBoy.Core.Utils;
using CoreBoy.Silk.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Shader = CoreBoy.Silk.Util.Shader;
using Texture = CoreBoy.Silk.Util.Texture;

namespace CoreBoy.Silk;

public class Emulator : IDisposable
{
    public Emulator(int width, int height, string title)
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        options.WindowBorder = WindowBorder.Fixed;
        window = Window.Create(options);
            
        var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .BuildServiceProvider();

        gameBoy = new GameBoy(serviceProvider.GetService<ILoggerFactory>());
        gameBoy.RenderFramebufferHandler += OnRenderFrameBuffer;
    }

    public void Run()
    {
        var bootRomPath = Path.Combine("Resources", "boot.rom");
        var cartridgePath = Path.Combine("Resources", "opus5.gb");

        gameBoy.LoadBootRom(bootRomPath);
        gameBoy.LoadCartridge(cartridgePath);

        emulatorThread = new Thread(gameBoy.Power);
        emulatorThread.Start();
            
        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.Closing += OnClose;

        window.UpdatesPerSecond = 60;
            
        window.Run();
    }

    private void OnLoad()
    {
        inputContext = window.CreateInput();
            
        foreach (var keyboard in inputContext.Keyboards)
        {
            keyboard.KeyDown += KeyDown;
        }

        gl = GL.GetApi(window);

        ebo = new BufferObject<uint>(gl, indices, BufferTargetARB.ElementArrayBuffer);
        vbo = new BufferObject<float>(gl, vertices, BufferTargetARB.ArrayBuffer);
        vao = new VertexArrayObject<float, uint>(gl, vbo, ebo);

        vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);

        shader = new Shader(gl, Path.Combine("Shader", "shader.vert"), Path.Combine("Shader", "shader.frag"));
        texture = new Texture(gl, Span<byte>.Empty, default, default);
    }

    private void OnUpdate(double frameTime)
    {
        var keyboard = inputContext.Keyboards[0];
            
        gameBoy.SetInput(new InputState(
            keyboard.IsKeyPressed(Key.W),
            keyboard.IsKeyPressed(Key.A),
            keyboard.IsKeyPressed(Key.S),
            keyboard.IsKeyPressed(Key.D),
            keyboard.IsKeyPressed(Key.H),
            keyboard.IsKeyPressed(Key.J),
            keyboard.IsKeyPressed(Key.K),
            keyboard.IsKeyPressed(Key.L)
        ));
    }
        
    private unsafe void OnRender(double frameTime)
    {
        gl.Clear((uint) ClearBufferMask.ColorBufferBit);

        vao.Bind();
        shader.Use();

        if (framebufferChanged)
        {
            texture = new Texture(gl, framebuffer, Graphics.ScreenWidth, Graphics.ScreenHeight);
        }
            
        texture.Bind();
        shader.SetUniform("uTexture0", 0);

        gl.DrawElements(PrimitiveType.Triangles, (uint) indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private void OnClose()
    {
        Dispose();
    }
        
    public void Dispose()
    {
        gameBoy.PowerOff();
        emulatorThread.Join();
            
        vbo.Dispose();
        ebo.Dispose();
        vao.Dispose();
        shader.Dispose();
        texture.Dispose();
    }

    private void KeyDown(IKeyboard arg1, Key arg2, int arg3)
    {
        if (arg2 == Key.Escape)
        {
            window.Close();
        }

        if (arg2 == Key.R)
        {
            gameBoy.Reset();
        }
    }

    private void OnRenderFrameBuffer(byte[] framebufferIn)
    {
        framebuffer = framebufferIn;
        framebufferChanged = true;

        // FPS
        float currentFps = (1f / (float)(DateTime.UtcNow - lastFramebuffer).TotalSeconds);

        fpsAverage -= fpsHistory[fpsIndex];
        fpsAverage += currentFps;
        fpsHistory[fpsIndex] = currentFps;
        if (++fpsIndex == fpsHistory.Length) fpsIndex = 0;

        window.Title = $"FPS: {fpsAverage / fpsHistory.Length:00.00}";

        lastFramebuffer = DateTime.UtcNow;
    }
        
    private GL gl;
    private IInputContext inputContext;
    private readonly IWindow window;

    private BufferObject<float> vbo;
    private BufferObject<uint> ebo;
    private VertexArrayObject<float, uint> vao;
        
    private Texture texture;
    private Shader shader;
        
    private byte[] framebuffer;
    private bool framebufferChanged;
    private DateTime lastFramebuffer = DateTime.UtcNow;
    private readonly float[] fpsHistory = new float[30];
    private float fpsAverage;
    private int fpsIndex;
        
    private Thread emulatorThread;
    private readonly GameBoy gameBoy;

    private readonly float[] vertices =
    {
        //X    Y      Z    U  V
        -1.0f, -1.0f, 0.0f, 0f, 1f,
        1.0f, -1.0f, 0.0f, 1f, 1f,
        1.0f, 1.0f, 0.0f, 1f, 0f,
        -1.0f, 1.0f, 0.0f, 0f, 0f
    };

    private readonly uint[] indices =
    {
        0, 1, 3,
        1, 2, 3
    };
}