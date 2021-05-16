using CoreBoy.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Threading;

namespace CoreBoy.MonoGame
{
    public class Emulator : Game
    {
        public Emulator(int screenWidth, int screenHeight)
        {
            graphics = new GraphicsDeviceManager(this);
            this.screenBoundaries = new Rectangle(0, 0, screenWidth, screenHeight);
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = false;
            
            graphics.PreferredBackBufferWidth = screenBoundaries.Width;
            graphics.PreferredBackBufferHeight = screenBoundaries.Height;

            graphics.ApplyChanges();       

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            framebuffer = new Texture2D(GraphicsDevice, 160, 144);

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
        }

        private void OnRenderFrameBuffer(byte[] framebuffer)
        {
            this.framebuffer.SetData(framebuffer);
            framebufferChanged = true;

            // FPS
            float currentFps = (1f / (float)(DateTime.UtcNow - lastFramebuffer).TotalSeconds);

            fpsAverage -= fpsHistory[fpsIndex];
            fpsAverage += currentFps;
            fpsHistory[fpsIndex] = currentFps;
            if (++fpsIndex == fpsHistory.Length) fpsIndex = 0;

            Window.Title = $"FPS: {fpsAverage / fpsHistory.Length:00.00}";

            lastFramebuffer = DateTime.UtcNow;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (framebufferChanged)
            {
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.Draw(this.framebuffer, screenBoundaries, Color.White);
                spriteBatch.End();

                framebufferChanged = false;
            }

            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(framebuffer, screenBoundaries, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void OnExiting(Object sender, EventArgs args)
        {
            base.OnExiting(sender, args);

            gameBoy.PowerOff();
            emulatorThread.Join();
        }

        private Texture2D framebuffer;
        private bool framebufferChanged;
        private Rectangle screenBoundaries;

        private DateTime lastFramebuffer = DateTime.UtcNow;
        private float[] fpsHistory = new float[30];
        private float fpsAverage = 0f;
        private int fpsIndex = 0;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private GameBoy gameBoy;
        private Thread emulatorThread;  
    }
}
