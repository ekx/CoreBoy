using System;
using CoreBoy.Core.Cartridges;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public sealed class Mmu : IMmu
    {
        public MmuState State { get; set; }

        public ICartridgeState CartridgeState
        {
            get => cartridge.State;
            set => cartridge.State = value;
        }

        public Mmu(ILogger<Mmu> log, IPpu ppu, ISpu spu)
        {
            this.log = log;
            this.ppu = ppu;
            this.spu = spu;
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
                if (address < 0x0100 && State.BootRomLoaded)
                {
                    //log.LogDebug($"Read from Boot ROM. Address: {address:X4}, Value: {this.bootRom[address]:X2}");
                    return this.bootRom[address];
                }
                // [0000-7FFF] Cartridge ROM
                else if (address < 0x8000)
                {
                    return this.cartridge[address];
                }
                // [8000-9FFF] Graphics RAM
                else if (address < 0xA000)
                {
                    return ppu[address];
                }
                // [A000-BFFF] Cartridge RAM
                else if (address < 0xC000)
                {
                    return this.cartridge[address];
                }
                // [C000-FDFF] Working RAM + Shadow
                else if (address < 0xFE00)
                {
                    //log.LogDebug($"Read from Working RAM. Address: {address:X4}, Value: {State.Wram[address & 0x1FFF]:X2}");
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
                    log.LogWarning($"Read from unsuable memory. Address: {address:X4}");
                    return 0x00;
                }
                // [FF00-FF7F] Memory-mapped I/O
                else if (address < 0xFF80)
                {
                    if (address is >= 0xFF40 and <= 0xFF4B)
                    {
                        return ppu[address];
                    }
                    // TODO: Implement I/O
                    else
                    {
                        log.LogError($"I/O read not implemented. Address: {address:X4}");
                        return 0x00;
                    }
                }
                // [FF80-FFFF] Zero-page RAM
                else
                {
                    //log.LogDebug($"Read from Zero-page RAM. Address: {address:X4}, Value: {State.Hram[address & 0x7F]:X2}");
                    return State.Hram[address & 0x7F];
                }
            }

            set
            {
                // [0000-7FFF] Cartridge ROM
                if (address < 0x8000)
                {
                    this.cartridge[address] = value;
                }
                // [8000-9FFF] Graphics RAM
                else if (address < 0xA000)
                {
                    ppu[address] = value;
                }
                // [A000-BFFF] Cartridge RAM
                else if (address < 0xC000)
                {
                    this.cartridge[address] = value;
                }
                // [C000-FDFF] Working RAM + Shadow
                else if (address < 0xFE00)
                {
                    //log.LogDebug($"Write to Working RAM. Address: {address:X4}, Value: {value:X2}");
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
                    log.LogWarning($"Write to unsuable memory. Address: {address:X4}, Value: {value:X2}");
                }
                // [FF00-FF7F] Memory-mapped I/O
                else if (address < 0xFF80)
                {
                    if (address is >= 0xFF40 and <= 0xFF4B)
                    {
                        ppu[address] = value;
                        return;
                    }
                    else if (address == 0xFF50 && value == 0x01)
                    {
                        State.BootRomLoaded = false;
                    }
                    // TODO: Implement I/O
                    else
                    {
                        log.LogError($"I/O write not implemented. Address: {address:X4}, Value: {value:X2}");
                    }
                }
                // [FF80-FFFF] Zero-page RAM
                else
                {
                    //log.LogDebug($"Write to Zero-page RAM. Address: {address:X4}, Value: {value:X2}");
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
        }

        private readonly IPpu ppu;
        private ISpu spu;
        private ICartridge cartridge;
        private byte[] bootRom = new byte[256];
        
        private readonly ILogger log;
    }

    [DataContract]
    public class MmuState
    {
        public MmuState()
        {
            var random = new Random();
            random.NextBytes(Wram);
            random.NextBytes(Hram);
        }

        [DataMember]
        public bool BootRomLoaded = true;

        [DataMember]
        public byte[] Wram = new byte[8192];
        [DataMember]
        public byte[] Hram = new byte[128];
    }
}
