using CoreBoy.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using Ultraviolet;
using Ultraviolet.BASS;
using Ultraviolet.Graphics;
using Ultraviolet.Graphics.Graphics2D;
using Ultraviolet.OpenGL;
using Ultraviolet.Platform;
using Ultraviolet.SDL2;

namespace CoreBoy.Ultraviolet
{
    public class Emulator : UltravioletApplication
    {
        public Emulator() : base("Benjamin Dengler", "CoreBoy")
        {
           
        }

        protected override UltravioletContext OnCreatingUltravioletContext()
        {
            var configuration = new SDL2UltravioletConfiguration();
            configuration.Plugins.Add(new OpenGLGraphicsPlugin());
            configuration.Plugins.Add(new BASSAudioPlugin());

            return new SDL2UltravioletContext(this, configuration);
        }

        protected override void OnLoadingContent()
        {
            spriteBatch = SpriteBatch.Create();
            framebuffer = Texture2D.CreateTexture(160, 144);

            window = Ultraviolet.GetPlatform().Windows.GetPrimary();
            window.SetWindowedClientSize(screenSize);

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

            base.OnLoadingContent();
        }

        private void OnRenderFrameBuffer(byte[] framebuffer)
        {
            this.framebuffer.SetData(framebuffer);

            // FPS
            float currentFps = (1f / (float)(DateTime.UtcNow - lastFramebuffer).TotalSeconds);

            fpsAverage -= fpsHistory[fpsIndex];
            fpsAverage += currentFps;
            fpsHistory[fpsIndex] = currentFps;
            if (++fpsIndex == fpsHistory.Length) fpsIndex = 0;

            window.Caption = $"FPS: {fpsAverage / fpsHistory.Length:00.00}";

            lastFramebuffer = DateTime.UtcNow;
        }

        protected override void OnDrawing(UltravioletTime time)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            spriteBatch.Draw(framebuffer, new Rectangle(new Point2(0, 288), new Size2(320, -288)), Color.White);

            spriteBatch.End();

            base.OnDrawing(time);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                gameBoy.PowerOff();
                emulatorThread.Join();
            }

            base.Dispose(disposing);
        }

        private Texture2D framebuffer;
        private readonly Size2 screenSize = new Size2(320, 288);

        private DateTime lastFramebuffer = DateTime.UtcNow;
        private float[] fpsHistory = new float[30];
        private float fpsAverage = 0f;
        private int fpsIndex = 0;

        private SpriteBatch spriteBatch;
        private IUltravioletWindow window;

        private GameBoy gameBoy;
        private Thread emulatorThread;
    }
}
