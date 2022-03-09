using System.Linq;
using CoreBoy.Core.Cartridges.Interfaces;
using CoreBoy.Core.Processors.Interfaces;
using CoreBoy.Core.Processors.State;
using CoreBoy.Core.Utils;
using CoreBoy.Core.Utils.Memory;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors;

public sealed class Mmu : IMmu
{
    public MmuState State { get; set; }

    public ICartridgeState CartridgeState
    {
        get => cartridge.State;
        set => cartridge.State = value;
    }

    public Mmu(ILogger log, IPpu ppu, ISpu spu)
    {
        this.log = log;
        this.ppu = ppu;
        this.spu = spu;

        this.ppu.VBlankInterruptHandler += () =>
        {
            if (State != null)
                State.Io[MmuIo.IF][InterruptFlag.VBlank] = true;
        };

        this.ppu.LcdStatusInterruptHandler += () =>
        {
            if (State != null)
                State.Io[MmuIo.IF][InterruptFlag.LcdStatus] = true;
        };
    }

    public void Reset()
    {
        log.LogInformation("MMU reset");

        State = new MmuState();
    }

    public byte this[ushort address]
    {
        get
        {
            // [0000-00FF] Boot ROM
            if (address < 0x0100 && !State.Io[MmuIo.BOOT][Boot.BootOff])
            {
                //log.LogDebug("Read from Boot ROM. Address: {Address:X4}, Value: {Value:X2}", address, bootRom[address]);
                return bootRom[address];
            }
            // [0000-7FFF] Cartridge ROM
            else if (address < 0x8000)
            {
                return cartridge[address];
            }
            // [8000-9FFF] Graphics RAM
            else if (address < 0xA000)
            {
                return ppu[address];
            }
            // [A000-BFFF] Cartridge RAM
            else if (address < 0xC000)
            {
                return cartridge[address];
            }
            // [C000-FDFF] Working RAM + Shadow
            else if (address < 0xFE00)
            {
                //log.LogDebug("Read from Working RAM. Address: {Address:X4}, Value: {Value:X2}", address, State.Wram[address & 0x1FFF]);
                return State.Wram[address & 0x1FFF];
            }
            // [FE00-FE9F] Object Attribute Memory
            else if (address < 0xFEA0)
            {
                return ppu[address];
            }
            // [FEA0-FEFF] Unusable memory
            else if (address < 0xFF00)
            {
                log.LogWarning("Read from unusable memory. Address: {Address:X4}", address);
                return 0xFF;
            }
            // [FF00-FF7F] Memory-mapped I/O
            else if (address < 0xFF80)
            {
                if (address is >= 0xFF10 and <= 0xFF3F)
                {
                    log.LogError("SPU read not implemented. Address: {Address:X4}", address);
                    return 0xFF;
                }
                else if (address is >= 0xFF40 and <= 0xFF4B)
                {
                    return ppu[address];
                }
                else
                {
                    return State.Io[address & 0xFF].Value;
                }
            }
            // [FF80-FFFF] Zero-page RAM
            else
            {
                if (address == 0xFFFF)
                {
                    return State.Io.Last().Value;
                }
                    
                return State.Hram[address & 0x7F];
            }
        }

        set
        {
            // [0000-7FFF] Cartridge ROM
            if (address < 0x8000)
            {
                cartridge[address] = value;
            }
            // [8000-9FFF] Graphics RAM
            else if (address < 0xA000)
            {
                ppu[address] = value;
            }
            // [A000-BFFF] Cartridge RAM
            else if (address < 0xC000)
            {
                cartridge[address] = value;
            }
            // [C000-FDFF] Working RAM + Shadow
            else if (address < 0xFE00)
            {
                //log.LogDebug("Write to Working RAM. Address: {Address:X4}, Value: {Value:X2}", address, value);
                State.Wram[address & 0x1FFF] = value;
            }
            // [FE00-FE9F] Object Attribute Memory
            else if (address < 0xFEA0)
            {
                ppu[address] = value;
            }
            // [FEA0-FEFF] Unusable memory
            else if (address < 0xFF00)
            {
                log.LogWarning("Write to unusable memory. Address: {Address:X4}, Value: {Value:X2}", address, value);
            }
            // [FF00-FF7F] Memory-mapped I/O
            else if (address < 0xFF80)
            {
                if (address is >= 0xFF11 and <= 0xFF3F)
                {
                    log.LogError("SPU write not implemented. Address: {Address:X4}, Value: {Value:X2}", address, value);
                }
                else if (address is >= 0xFF40 and <= 0xFF4B)
                {
                    ppu[address] = value;
                }
                else
                {
                    if (address == 0xFF50 && value == 0x01 && !State.Io[MmuIo.BOOT][Boot.BootOff])
                    {
                        State.Io[address & 0xFF].LockBit(Boot.BootOff, true);
                    }

                    State.Io[address & 0xFF].Value = value;
                }
            }
            // [FF80-FFFF] Zero-page RAM
            else
            {
                if (address == 0xFFFF)
                {
                    State.Io.Last().Value = value;
                }
                    
                State.Hram[address & 0x7F] = value;
            }
        }
    }

    public void LoadBootRom(byte[] bootRomIn)
    {
        bootRom = bootRomIn;
    }

    public void LoadCartridge(ICartridge cartridgeIn)
    {
        cartridge = cartridgeIn;
    }

    public void UpdateState(long cycles)
    {
        ppu.UpdateState(cycles);
        spu.UpdateState(cycles);
    }

    public void SetInput(InputState inputState)
    {
        State.Io[MmuIo.IF][InterruptFlag.Input] = ((InputCell) State.Io[MmuIo.P1]).UpdateInputState(inputState);
    }

    private readonly ILogger log;
    private readonly IPpu ppu;
    private readonly ISpu spu;
        
    private ICartridge cartridge;
    private byte[] bootRom = new byte[256];
}