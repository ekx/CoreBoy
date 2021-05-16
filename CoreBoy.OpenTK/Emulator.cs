using CoreBoy.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace CoreBoy.OpenTK
{
    public class Emulator : GameWindow
    {
        public Emulator(int width, int height, string title) : base(new GameWindowSettings(), new NativeWindowSettings { Size = new Vector2i(width, height), Title = title })
        {
            this.Title = title;
            this.WindowBorder = WindowBorder.Fixed;

            var serviceProvider = new ServiceCollection()
                .AddLogging(configure => configure.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .BuildServiceProvider();

            gameBoy = new GameBoy(serviceProvider.GetService<ILoggerFactory>());
            gameBoy.RenderFramebufferHandler += OnRenderFrameBuffer;

            string bootRomPath = Path.Combine("Resources", "boot.rom");
            string cartridgePath = Path.Combine("Resources", "opus5.gb");

            gameBoy.LoadBootRom(bootRomPath);
            gameBoy.LoadCartridge(cartridgePath);

            emulatorThread = new Thread(new ThreadStart(gameBoy.Power));
            emulatorThread.Start();

            GL.Viewport(0, 0, width, height);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, 160, 0, 144, -2, 2);
            GL.ClearColor(1f, 1f, 1f, 1f);
            GL.Enable(EnableCap.Texture2D);

            textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);
        }

        private void OnRenderFrameBuffer(byte[] framebuffer)
        {
            this.framebuffer = framebuffer;
            framebufferChanged = true;

            // FPS
            float currentFps = (1f / (float)(DateTime.UtcNow - lastFramebuffer).TotalSeconds);

            fpsAverage -= fpsHistory[fpsIndex];
            fpsAverage += currentFps;
            fpsHistory[fpsIndex] = currentFps;
            if (++fpsIndex == fpsHistory.Length) fpsIndex = 0;

            Title = $"FPS: {fpsAverage / fpsHistory.Length:00.00}";

            lastFramebuffer = DateTime.UtcNow;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            if (framebufferChanged)
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 160, 144, 0, PixelFormat.Bgra, PixelType.UnsignedByte, framebuffer);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

                framebufferChanged = false;
            }

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Begin(PrimitiveType.Quads);

            GL.TexCoord2(0f, 1f);
            GL.Vertex2(0f, 0f);

            GL.TexCoord2(1f, 1f);
            GL.Vertex2(160f, 0f);

            GL.TexCoord2(1f, 0f);
            GL.Vertex2(160f, 144f);

            GL.TexCoord2(0f, 0f); 
            GL.Vertex2(0f, 144f);

            GL.End();

            SwapBuffers();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            gameBoy.PowerOff();
            emulatorThread.Join();
        }

        private int textureId;
        private byte[] framebuffer;
        private bool framebufferChanged = false;

        private DateTime lastFramebuffer = DateTime.UtcNow;
        private float[] fpsHistory = new float[30];
        private float fpsAverage = 0f;
        private int fpsIndex = 0;

        private GameBoy gameBoy;
        private Thread emulatorThread;
    }
}