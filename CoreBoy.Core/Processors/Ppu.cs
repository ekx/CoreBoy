using System;
using CoreBoy.Core.Utils;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public sealed class Ppu : IPpu
    {
        public event RenderFramebufferDelegate RenderFramebufferHandler;

        public PpuState State { get; set; }
        
        public Ppu(ILogger<Ppu> log)
        {
            this.log = log;
        }

        public void Reset()
        {
            log.LogInformation("PPU reset");

            State = new PpuState();
        }

        public byte this[ushort address]
        {
            get
            {
                // [8000-9FFF] Graphics RAM
                if (address is >= 0x8000 and < 0xA000)
                {
                    if (State.Io[GraphicsIo.LCDC][LcdControl.LcdPower] && (int)ScreenMode == 3)
                    {
                        log.LogWarning($"Read from Graphics RAM while inaccessible. Address: {address:X4}");
                        return 0xFF;
                    }

                    //log.LogDebug($"Read from Graphics RAM. Address: {address:X4}, Value {State.Vram[address & 0x1FFF]:X2}");
                    return State.Vram[address & 0x1FFF];
                }
                // [FE00-FE9F] Object Attribute Memory
                else if (address is >= 0xFE00 and < 0xFEA0)
                {
                    if (State.Io[GraphicsIo.LCDC][LcdControl.LcdPower] && (int)ScreenMode >= 2)
                    {
                        log.LogWarning($"Read from OAM while inaccessible. Address: {address:X4}");
                        return 0xFF;
                    }
                    
                    //log.LogDebug($"Read from OAM. Address: {address:X4}, Value {State.Oam[address & 0xFF]:X2}");
                    return State.Oam[address & 0xFF];
                }
                // [FF40-FF4B] Graphics IO
                else if (address is >= 0xFF40 and < 0xFF4C)
                {
                    //log.LogDebug($"Read from Graphics IO. Address: {address:X4}, Value {State.IO[address & 0xF]:X2}");
                    return State.Io[address & 0xF].Value;
                }
                else
                {
                    log.LogError($"Read from non PPU memory space. Address: {address:X4}");
                    return 0x00;
                }
            }

            set
            {
                // [8000-9FFF] Graphics RAM
                if (address is >= 0x8000 and < 0xA000)
                {
                    if (State.Io[GraphicsIo.LCDC][LcdControl.LcdPower] && (int)ScreenMode == 3)
                    {
                        log.LogWarning($"Write to Graphics RAM while inaccessible. Address: {address:X4}, Value: {value:X2}");
                        return;
                    }

                    //log.LogDebug($"Write to Graphics RAM. Address: {address:X4}, Value: {value:X2}");
                    State.Vram[address & 0x1FFF] = value;
                }
                // [FE00-FE9F] Object Attribute Memory
                else if (address is >= 0xFE00 and < 0xFEA0)
                {
                    if (State.Io[GraphicsIo.LCDC][LcdControl.LcdPower] && (int)ScreenMode >= 2)
                    {
                        log.LogWarning($"Write to OAM while inaccessible. Address: {address:X4}, Value: {value:X2}");
                        return;
                    }

                    //log.LogDebug($"Write to OAM. Address: {address:X4}, Value: {value:X2}");
                    State.Oam[address & 0xFF] = value;
                }
                // [FF40-FF4B] Graphics IO
                else if (address is >= 0xFF40 and < 0xFF4C)
                {
                    //log.LogDebug($"Write to Graphics IO. Address: {address:X4}, Value: {value:X2}");
                    State.Io[address & 0xF].Value = value;
                }
                else
                {
                    log.LogError($"Write to non PPU memory space. Address: {address:X4}, Value: {value:X2}");
                }
            }
        }

        public void UpdateState(long cycles)
        {
            // TODO: Is this right? Also VRAM and OAM access?
            if (!State.Io[GraphicsIo.LCDC][LcdControl.LcdPower])
            {
                return;
            }

            State.Clock += cycles;

            switch (ScreenMode)
            {
                case ScreenMode.HBlank:
                    UpdateHBlank();
                    break;
                case ScreenMode.VBlank:
                    UpdateVBlank();
                    break;
                case ScreenMode.AccessingOam:
                    UpdateAccessingOam();
                    break;
                case ScreenMode.TransferringData:
                    UpdateTransferringData();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ScreenMode), "Invalid screen mode");
            }
        }

        private void UpdateHBlank()
        {
            if (State.Clock >= 204)
            {
                if (State.Io[GraphicsIo.LY].Value == 143)
                {
                    ScreenMode = ScreenMode.VBlank;
                    RenderFramebufferHandler?.Invoke(framebuffer);
                }
                else
                {
                    ScreenMode = ScreenMode.AccessingOam;
                }

                State.Clock = 0;
                State.Io[GraphicsIo.LY].Value++;
            }
        }

        private void UpdateVBlank()
        {
            if (State.Clock >= 456)
            {
                State.Clock = 0;
                State.Io[GraphicsIo.LY].Value++;

                if (State.Io[GraphicsIo.LY].Value > 153)
                {
                    ScreenMode = ScreenMode.AccessingOam;
                    State.Io[GraphicsIo.LY].Value = 0;
                }
            }
        }

        private void UpdateAccessingOam()
        {
            if (State.Clock >= 80)
            {
                State.Clock = 0;
                ScreenMode = ScreenMode.TransferringData;
            }
        }

        private void UpdateTransferringData()
        {
            if (State.Clock >= 172)
            {
                State.Clock = 0;
                ScreenMode = ScreenMode.HBlank;

                RenderScanline();
            }
        }

        private void RenderScanline()
        {
            if (State.Io[GraphicsIo.LCDC][LcdControl.BgEnabled])
            {
                RenderBackground();
            }

            if (State.Io[GraphicsIo.LCDC][LcdControl.WindowEnabled])
            {
                RenderWindow();
            }

            if (State.Io[GraphicsIo.LCDC][LcdControl.SpritesEnabled])
            {
                RenderSprites();
            }
        }

        private void RenderBackground()
        {
            var x = State.Io[GraphicsIo.SCX].Value;
            var y = (byte)((State.Io[GraphicsIo.LY].Value + State.Io[GraphicsIo.SCY].Value) & 0xFF);

            var tileMapOffset = State.Io[GraphicsIo.LCDC][LcdControl.BgTileMap] ? (ushort)0x9C00 : (ushort)0x9800;

            RenderToFramebuffer(x, y, tileMapOffset);
        }

        private void RenderToFramebuffer(byte x, byte y, ushort tileMapOffset)
        {
            // TODO: Should be cleaned up & performance improved
            var tileDataOffset = State.Io[GraphicsIo.LCDC][LcdControl.TileSet] ? (ushort)0x8000 : (ushort)0x8800;
            var mapOffset = tileDataOffset == 0x8800 ? 128 : 0;

            for (var i = 0; i < Graphics.ScreenWidth; i++)
            {
                var tileMapIndex = ((y / 8) * 32) + (x / 8);

                int tileIndex = this[(ushort)(tileMapOffset + tileMapIndex)];
                if (mapOffset == 128)
                {
                    tileIndex = (sbyte)tileIndex;
                }

                var tileStartAddress = tileDataOffset + ((tileIndex + mapOffset) * 16);

                var pixelX = x % 8;
                var pixelY = (y % 8) * 2;

                var byte1 = this[(ushort)(tileStartAddress + pixelY)];
                var byte2 = this[(ushort)(tileStartAddress + pixelY + 1)];
               
                var colorValue = CalculatePixelValue(byte1, byte2, pixelX);

                var framebufferIndex = ((State.Io[GraphicsIo.LY].Value * Graphics.ScreenWidth) + i) * 4;

                framebuffer[framebufferIndex] = Palette[colorValue].r;
                framebuffer[framebufferIndex + 1] = Palette[colorValue].g;
                framebuffer[framebufferIndex + 2] = Palette[colorValue].b;
                framebuffer[framebufferIndex + 3] = 255;

                x++;
            }
        }

        private void RenderWindow()
        {
            
        }

        private void RenderSprites()
        {
            
        }

        private byte CalculatePixelValue(byte byte1, byte byte2, int x)
        {
            byte colorIndex = 0;
            colorIndex |= (byte)((byte1 >> (7 - x)) & 1);
            colorIndex |= (byte)(((byte2 >> (7 - x)) & 1) << 1);

            byte currentPalette = State.Io[GraphicsIo.BGP].Value;
            byte high = 0, low = 0;

            switch (colorIndex)
            {
                case 0:
                    high = 1;
                    low = 0;
                    break;
                case 1:
                    high = 3;
                    low = 2;
                    break;
                case 2:
                    high = 5;
                    low = 4;
                    break;
                case 3:
                    high = 7;
                    low = 6;
                    break;
            }

            byte colorValue = 0;
            colorValue |= currentPalette.GetBit(low) ? (byte)1 : (byte)0;
            colorValue |= currentPalette.GetBit(high) ? (byte)2 : (byte)0;

            return colorValue;
        }

        private ScreenMode ScreenMode
        {
            get
            {
                byte state = 0x0;
                state = state.SetBit(0, State.Io[GraphicsIo.STAT][LcdStatus.ScreenModeLow]);
                state = state.SetBit(1, State.Io[GraphicsIo.STAT][LcdStatus.ScreenModeHigh]);
                return (ScreenMode)state;
            }
            set
            {
                var state = (byte)value;
                State.Io[GraphicsIo.STAT].LockBit(LcdStatus.ScreenModeLow, state.GetBit(0));
                State.Io[GraphicsIo.STAT].LockBit(LcdStatus.ScreenModeHigh, state.GetBit(1));
            }
        }

        private readonly byte[] framebuffer = new byte[Graphics.ScreenWidth * Graphics.ScreenHeight * 4];

        private readonly ILogger log;

        private static readonly Dictionary<byte, (byte r, byte g, byte b)> Palette = new()
        {
            [3] = (0, 0, 0),
            [2] = (85, 85, 85),
            [1] = (170, 170, 170),
            [0] = (255, 255, 255)
        };
}

    [DataContract]
    public class PpuState
    {
        public PpuState()
        {
            Io.Populate(() => new MemoryCell());
            Io[GraphicsIo.STAT].LockBit(LcdStatus.Unused, true);

            var random = new Random();
            random.NextBytes(Vram);
            random.NextBytes(Oam);
        }

        [DataMember]
        public MemoryCell[] Io = new MemoryCell[12];
        [DataMember]
        public byte[] Vram = new byte[8192];
        [DataMember]
        public byte[] Oam = new byte[160];

        [DataMember]
        public long Clock;
    }
}
