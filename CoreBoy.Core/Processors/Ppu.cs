using System;
using CoreBoy.Core.Utils;
using System.Runtime.Serialization;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public class Ppu : IPpu
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

            this.State = new PpuState();
        }

        public byte this[ushort address]
        {
            get
            {
                // [8000-9FFF] Graphics RAM
                if (address >= 0x8000 && address < 0xA000)
                {
                    if (State.IO[GraphicsIO.LCDC][LCDControl.LCDPower] && (int)ScreenMode == 3)
                    {
                        log.LogWarning($"Read from Graphics RAM while inaccessible. Address: {address:X4}");
                        return 0xFF;
                    }

                    //log.LogDebug($"Read from Graphics RAM. Address: {address:X4}, Value {State.Vram[address & 0x1FFF]:X2}");
                    return State.Vram[address & 0x1FFF];
                }
                // [FE00-FE9F] Object Attribute Memory
                else if (address >= 0xFE00 && address < 0xFEA0)
                {
                    if (State.IO[GraphicsIO.LCDC][LCDControl.LCDPower] && (int)ScreenMode >= 2)
                    {
                        log.LogWarning($"Read from OAM while inaccessible. Address: {address:X4}");
                        return 0xFF;
                    }
                    
                    //log.LogDebug($"Read from OAM. Address: {address:X4}, Value {State.Oam[address & 0xFF]:X2}");
                    return State.Oam[address & 0xFF];
                }
                // [FF40-FF4B] Graphics IO
                else if (address >= 0xFF40 && address < 0xFF4C)
                {
                    //log.LogDebug($"Read from Graphics IO. Address: {address:X4}, Value {State.IO[address & 0xF]:X2}");
                    return State.IO[address & 0xF].Value;
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
                if (address >= 0x8000 && address < 0xA000)
                {
                    if (State.IO[GraphicsIO.LCDC][LCDControl.LCDPower] && (int)ScreenMode == 3)
                    {
                        log.LogWarning($"Write to Graphics RAM while inaccessible. Address: {address:X4}, Value: {value:X2}");
                        return;
                    }

                    //log.LogDebug($"Write to Graphics RAM. Address: {address:X4}, Value: {value:X2}");
                    State.Vram[address & 0x1FFF] = value;
                }
                // [FE00-FE9F] Object Attribute Memory
                else if (address >= 0xFE00 && address < 0xFEA0)
                {
                    if (State.IO[GraphicsIO.LCDC][LCDControl.LCDPower] && (int)ScreenMode >= 2)
                    {
                        log.LogWarning($"Write to OAM while inaccessible. Address: {address:X4}, Value: {value:X2}");
                        return;
                    }

                    //log.LogDebug($"Write to OAM. Address: {address:X4}, Value: {value:X2}");
                    State.Oam[address & 0xFF] = value;
                }
                // [FF40-FF4B] Graphics IO
                else if (address >= 0xFF40 && address < 0xFF4C)
                {
                    //log.LogDebug($"Write to Graphics IO. Address: {address:X4}, Value: {value:X2}");
                    State.IO[address & 0xF].Value = value;
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
            if (!State.IO[GraphicsIO.LCDC][LCDControl.LCDPower])
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
                case ScreenMode.AccessingOAM:
                    UpdateAccessingOAM();
                    break;
                case ScreenMode.TransferringData:
                    UpdateTransferringData();
                    break;
            }
        }

        private void UpdateHBlank()
        {
            if (State.Clock >= 204)
            {
                if (State.IO[GraphicsIO.LY].Value == 143)
                {
                    ScreenMode = ScreenMode.VBlank;
                    RenderFramebufferHandler?.Invoke(framebuffer);
                }
                else
                {
                    ScreenMode = ScreenMode.AccessingOAM;
                }

                State.Clock = 0;
                State.IO[GraphicsIO.LY].Value++;
            }
        }

        private void UpdateVBlank()
        {
            if (State.Clock >= 456)
            {
                State.Clock = 0;
                State.IO[GraphicsIO.LY].Value++;

                if (State.IO[GraphicsIO.LY].Value > 153)
                {
                    ScreenMode = ScreenMode.AccessingOAM;
                    State.IO[GraphicsIO.LY].Value = 0;
                }
            }
        }

        private void UpdateAccessingOAM()
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
            if (State.IO[GraphicsIO.LCDC][LCDControl.BGEnabled])
            {
                RenderBackground();
            }
        }

        private void RenderBackground()
        {
            byte x = State.IO[GraphicsIO.SCX].Value;
            byte y = (byte)((State.IO[GraphicsIO.LY].Value + State.IO[GraphicsIO.SCY].Value) & 0xFF);

            ushort tileMapOffset = State.IO[GraphicsIO.LCDC][LCDControl.BGTileMap] ? (ushort)0x9C00 : (ushort)0x9800;

            RenderToFramebuffer(x, y, tileMapOffset);
        }

        private void RenderToFramebuffer(byte x, byte y, ushort tileMapOffset)
        {
            // TODO: Should be cleaned up & performance improved
            ushort tileDataOffest = State.IO[GraphicsIO.LCDC][LCDControl.TileSet] ? (ushort)0x8000 : (ushort)0x8800;
            int mapOffset = (tileDataOffest == 0x8800) ? 128 : 0;

            for (int i = 0; i < Graphics.ScreenWidth; i++)
            {
                int tileMapIndex = ((y / 8) * 32) + (x / 8);

                int tileIndex = this[(ushort)(tileMapOffset + tileMapIndex)];
                if (mapOffset == 128)
                {
                    tileIndex = (sbyte)tileIndex;
                }

                int tileStartAddress = tileDataOffest + ((tileIndex + mapOffset) * 16);

                int pixelX = x % 8;
                int pixelY = (y % 8) * 2;

                byte byte1 = this[(ushort)(tileStartAddress + pixelY)];
                byte byte2 = this[(ushort)(tileStartAddress + pixelY + 1)];
               
                byte colorValue = CalculatePixelValue(byte1, byte2, pixelX);

                int framebufferIndex = ((State.IO[GraphicsIO.LY].Value * Graphics.ScreenWidth) + i) * 4;

                framebuffer[framebufferIndex] = palette[colorValue].r;
                framebuffer[framebufferIndex + 1] = palette[colorValue].g;
                framebuffer[framebufferIndex + 2] = palette[colorValue].b;
                framebuffer[framebufferIndex + 3] = 255;

                x++;
            }
        }

        private byte CalculatePixelValue(byte byte1, byte byte2, int x)
        {
            byte colorIndex = 0;
            colorIndex |= (byte)((byte1 >> (7 - x)) & 1);
            colorIndex |= (byte)(((byte2 >> (7 - x)) & 1) << 1);

            byte palette = State.IO[GraphicsIO.BGP].Value;
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
                default:
                    break;
            }

            byte colorValue = 0;
            colorValue |= palette.GetBit(low) ? (byte)1 : (byte)0;
            colorValue |= palette.GetBit(high) ? (byte)2 : (byte)0;

            return colorValue;
        }

        private ScreenMode ScreenMode
        {
            get
            {
                byte state = 0x0;
                state = state.SetBit(0, State.IO[GraphicsIO.STAT][LCDStatus.ScreenModeLow]);
                state = state.SetBit(1, State.IO[GraphicsIO.STAT][LCDStatus.ScreenModeHigh]);
                return (ScreenMode)state;
            }
            set
            {
                var state = (byte)value;
                State.IO[GraphicsIO.STAT].LockBit(LCDStatus.ScreenModeLow, state.GetBit(0));
                State.IO[GraphicsIO.STAT].LockBit(LCDStatus.ScreenModeHigh, state.GetBit(1));
            }
        }

        public byte[] framebuffer = new byte[Graphics.ScreenWidth * Graphics.ScreenHeight * 4];

        private readonly ILogger log;

        private static Dictionary<byte, (byte r, byte g, byte b)> palette = new Dictionary<byte, (byte r, byte g, byte b)>
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
            IO.Populate(() => new MemoryCell());
            IO[GraphicsIO.STAT].LockBit(LCDStatus.Unused, true);

            var random = new Random();
            random.NextBytes(Vram);
            random.NextBytes(Oam);
        }

        [DataMember]
        public MemoryCell[] IO = new MemoryCell[12];
        [DataMember]
        public byte[] Vram = new byte[8192];
        [DataMember]
        public byte[] Oam = new byte[160];

        [DataMember]
        public long Clock;
    }
}
