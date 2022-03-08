using System;
using CoreBoy.Core.Utils;
using System.Collections.Generic;
using CoreBoy.Core.Processors.Interfaces;
using CoreBoy.Core.Processors.State;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors;

public sealed class Ppu : IPpu
{
    public event RenderFramebufferDelegate RenderFramebufferHandler;
        
    public event VBlankInterruptDelegate VBlankInterruptHandler;

    public event LcdStatusInterruptDelegate LcdStatusInterruptHandler;

    public PpuState State { get; set; }
        
    public Ppu(ILogger log)
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
                if (State.Io[GraphicsIo.LCDC][LcdControl.LcdPower] && ScreenMode == ScreenMode.TransferringData)
                {
                    log.LogWarning("Read from Graphics RAM while inaccessible. Address: {Address:X4}", address);
                    return 0xFF;
                }

                //log.LogDebug("Read from Graphics RAM. Address: {Address:X4}, Value {Value:X2}", address, State.Vram[address & 0x1FFF]);
                return State.Vram[address & 0x1FFF];
            }
            // [FE00-FE9F] Object Attribute Memory
            else if (address is >= 0xFE00 and < 0xFEA0)
            {
                if (State.Io[GraphicsIo.LCDC][LcdControl.LcdPower] && ScreenMode >= ScreenMode.AccessingOam)
                {
                    log.LogWarning("Read from OAM while inaccessible. Address: {Address:X4}", address);
                    return 0xFF;
                }
                    
                //log.LogDebug("Read from OAM. Address: {Address:X4}, Value {Value:X2}", address, State.Oam[address & 0xFF]);
                return State.Oam[address & 0xFF];
            }
            // [FF40-FF4B] Graphics IO
            else if (address is >= 0xFF40 and < 0xFF4C)
            {
                //log.LogDebug("Read from Graphics IO. Address: {Address:X4}, Value {Value:X2}", address, State.Io[address & 0xF]);
                return State.Io[address & 0xF].Value;
            }
            else
            {
                log.LogError("Read from non PPU memory space. Address: {Address:X4}", address);
                return 0x00;
            }
        }

        set
        {
            // [8000-9FFF] Graphics RAM
            if (address is >= 0x8000 and < 0xA000)
            {
                if (State.Io[GraphicsIo.LCDC][LcdControl.LcdPower] && ScreenMode == ScreenMode.TransferringData)
                {
                    log.LogWarning("Write to Graphics RAM while inaccessible. Address: {Address:X4}, Value: {Value:X2}", address, value);
                    return;
                }

                //log.LogDebug("Write to Graphics RAM. Address: {Address:X4}, Value: {Value:X2}", address, value);
                State.Vram[address & 0x1FFF] = value;
            }
            // [FE00-FE9F] Object Attribute Memory
            else if (address is >= 0xFE00 and < 0xFEA0)
            {
                if (State.Io[GraphicsIo.LCDC][LcdControl.LcdPower] && ScreenMode >= ScreenMode.AccessingOam)
                {
                    log.LogWarning("Write to OAM while inaccessible. Address: {Address:X4}, Value: {Value:X2}", address, value);
                    return;
                }

                //log.LogDebug("Write to OAM. Address: {Address:X4}, Value: {Value:X2}", address, value);
                State.Oam[address & 0xFF] = value;
            }
            // [FF40-FF4B] Graphics IO
            else if (address is >= 0xFF40 and < 0xFF4C)
            {
                //log.LogDebug("Write to Graphics IO. Address: {Address:X4}, Value: {Value:X2}", address, value);
                State.Io[address & 0xF].Value = value;
            }
            else
            {
                log.LogError("Write to non PPU memory space. Address: {Address:X4}, Value: {Value:X2}", address, value);
            }
        }
    }

    public void UpdateState(long cycles)
    {
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

        // Update LYC signal and trigger interrupt if applicable
        var currentLycSignal = State.Io[GraphicsIo.LY].Value == State.Io[GraphicsIo.LYC].Value;
        if (currentLycSignal != State.Io[GraphicsIo.STAT][LcdStatus.LycSignal])
        {
            State.Io[GraphicsIo.STAT][LcdStatus.LycSignal] = currentLycSignal;
            if (currentLycSignal) LcdStatusInterruptHandler?.Invoke();
        }
    }

    private void UpdateHBlank()
    {
        if (State.Clock >= 204)
        {
            ScreenMode = State.Io[GraphicsIo.LY].Value == 143 ? ScreenMode.VBlank : ScreenMode.AccessingOam;

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
        var x = State.Io[GraphicsIo.WX].Value;
        var y = (byte)((State.Io[GraphicsIo.LY].Value + State.Io[GraphicsIo.WY].Value) & 0xFF);

        var tileMapOffset = State.Io[GraphicsIo.LCDC][LcdControl.WindowTileMap] ? (ushort)0x9C00 : (ushort)0x9800;

        RenderToFramebuffer(x, y, tileMapOffset);
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
        colorValue |= (byte) (currentPalette.GetBit(low) ? 1 : 0);
        colorValue |= (byte) (currentPalette.GetBit(high) ? 2 : 0);

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
            if (value == ScreenMode.VBlank)
            {
                RenderFramebufferHandler?.Invoke(framebuffer);
                VBlankInterruptHandler?.Invoke();
            }

            if (State.Io[GraphicsIo.STAT][LcdStatus.HBlankCheckEnabled] && value == ScreenMode.HBlank
                || State.Io[GraphicsIo.STAT][LcdStatus.VBlankCheckEnabled] && value == ScreenMode.VBlank
                || State.Io[GraphicsIo.STAT][LcdStatus.OamCheckEnabled] && value == ScreenMode.AccessingOam)
            {
                LcdStatusInterruptHandler?.Invoke();
            }
                
            var state = (byte)value;
            State.Io[GraphicsIo.STAT].LockBit(LcdStatus.ScreenModeLow, state.GetBit(0));
            State.Io[GraphicsIo.STAT].LockBit(LcdStatus.ScreenModeHigh, state.GetBit(1));
        }
    }

    private readonly ILogger log;
        
    private readonly byte[] framebuffer = new byte[Graphics.ScreenWidth * Graphics.ScreenHeight * 4];

    private static readonly Dictionary<byte, (byte r, byte g, byte b)> Palette = new()
    {
        [3] = (0, 0, 0),
        [2] = (85, 85, 85),
        [1] = (170, 170, 170),
        [0] = (255, 255, 255)
    };
}