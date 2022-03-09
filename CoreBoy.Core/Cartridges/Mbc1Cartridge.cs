using System;
using CoreBoy.Core.Cartridges.Interfaces;
using CoreBoy.Core.Cartridges.State;
using CoreBoy.Core.Cartridges.Utils;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Cartridges;

public class Mbc1Cartridge : ICartridge
{
    public ICartridgeState State
    {
        get => state;
        set => state = (Mbc1CartridgeState) value;
    }

    public Mbc1Cartridge(ILogger log, byte[] data)
    {
        this.log = log;

        header = new CartridgeHeader(data);
        
        if (data.Length != header.RomSize.Total)
            throw new ArgumentOutOfRangeException(nameof(data), "Cartridge data has invalid length");
        if (header.HeaderChecksum != header.CalculatedHeaderChecksum)
            throw new ArgumentOutOfRangeException(nameof(header.HeaderChecksum), "Cartridge header has invalid checksum");

        rom = data;

        State = new Mbc1CartridgeState(header.RamSize);
    }

    public byte this[ushort address]
    {
        get
        {
            // [0000-3FFF] Static ROM Bank
            if (address < 0x4000)
            {
                return rom[address];
            }
            // [4000-7FFF] Switchable ROM Bank
            else if (address < 0x8000)
            {
                var bankNumber = state.RomBank.Value + state.ModeSelect.Value == 0x00 ? state.RamBank.Value << 5 : 0;
                if (bankNumber is 0x00 or 0x20 or 0x40 or 0x60) bankNumber++; // TODO: This is not quite right -> Fix
                return rom[(address & 0x1FFF) + (0x1FFF * bankNumber)];
            }
            // [A000-BFFF] Cartridge RAM
            else if (address is >= 0xA000 and < 0xC000)
            {
                if ((state.RamEnable.Value & 0xF) != 0xA)
                {
                    log.LogWarning("Read from disabled cartridge RAM. Address: {Address:X4}", address);
                    return 0xFF;
                }

                if ((address & 0x1FFF) > header.RamSize.Total)
                {
                    log.LogWarning("Read from nonexistent cartridge RAM. Address: {Address:X4}", address);
                    return 0xFF;
                }

                var bankOffset = 0x1FFF * (state.ModeSelect.Value == 0x01 ? state.RamBank.Value : 0);
                return state.CartridgeRam[(address & 0x1FFF) + bankOffset];
            }
            else
            {
                log.LogError("Read from non cartridge memory space. Address: {Address:X4}", address);
                return 0x00;
            }
        }

        set
        {
            // [0000-1FFF] RAM Enable
            if (address < 0x2000)
            {
                state.RamEnable.Value = value;
            }
            // [2000-3FFF] ROM Bank Number
            else if (address < 0x4000)
            {
                state.RomBank.Value = value;
            }
            // [4000-5FFF] RAM Bank Number
            else if (address < 0x6000)
            {
                state.RamBank.Value = value;
            }
            // [6000-7FFF] Mode Select
            else if (address < 0x8000)
            {
                state.ModeSelect.Value = value;
            }
            // [A000-BFFF] Cartridge RAM
            else if (address is >= 0xA000 and < 0xC000)
            {
                if ((state.RamEnable.Value & 0xF) != 0xA)
                {
                    log.LogWarning("Write to disabled cartridge RAM. Address: {Address:X4}, Value: {Value:X2}", address, value);
                    return;
                }

                if ((address & 0x1FFF) > header.RamSize.Total)
                {
                    log.LogWarning("Write to nonexistent cartridge RAM. Address: {Address:X4}, Value: {Value:X2}", address, value);
                    return;
                }

                var bankOffset = 0x1FFF * (state.ModeSelect.Value == 0x01 ? state.RamBank.Value : 0);
                state.CartridgeRam[(address & 0x1FFF) + bankOffset] = value;
            }
            else
            {
                log.LogError("Write to non cartridge memory space. Address: {Address:X4}, Value: {Value:X2}", address, value);
            }
        }
    }
    
    private readonly byte[] rom;
    private Mbc1CartridgeState state;
    
    private readonly CartridgeHeader header;
    private readonly ILogger log;
}