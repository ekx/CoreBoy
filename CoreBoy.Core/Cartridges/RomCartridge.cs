using System;
using Microsoft.Extensions.Logging;
using CoreBoy.Core.Cartridges.Interfaces;
using CoreBoy.Core.Cartridges.State;
using CoreBoy.Core.Cartridges.Utils;

namespace CoreBoy.Core.Cartridges;

public class RomCartridge : ICartridge
{
    public ICartridgeState State
    {
        get => state;
        set => state = (RomCartridgeState) value;
    }

    public RomCartridge(ILogger log, byte[] data)
    {
        this.log = log;

        var header = new CartridgeHeader(data);
        
        if (data.Length != header.RomSize.Total)
            throw new ArgumentOutOfRangeException(nameof(data), "Cartridge data has invalid length");
        if (header.HeaderChecksum != header.CalculatedHeaderChecksum)
            throw new ArgumentOutOfRangeException(nameof(header.HeaderChecksum), "Cartridge header has invalid checksum");

        rom = data;

        State = new RomCartridgeState();
    }

    public byte this[ushort address]
    {
        get
        {
            // [0000-7FFF] Cartridge ROM
            if (address < 0x8000)
            {
                //log.LogDebug("Read from Cartridge ROM. Address: {Address:X4}, Value: {Value:X2}", address, rom[address]);
                return rom[address];
            }
            // [A000-BFFF] Cartridge RAM
            else if (address is >= 0xA000 and < 0xC000)
            {
                log.LogWarning("Read from nonexistent cartridge RAM. Address: {Address:X4}", address);
                return 0xFF;
            }
            else
            {
                log.LogError("Read from non cartridge memory space. Address: {Address:X4}", address);
                return 0x00;
            }
        }

        set
        {
            // [0000-7FFF] Cartridge ROM
            if (address < 0x8000)
            {
                log.LogWarning("Write to read-only cartridge ROM. Address: {Address:X4}, Value: {Value:X2}", address, value);
            }
            // [A000-BFFF] Cartridge RAM
            else if (address is >= 0xA000 and < 0xC000)
            {
                log.LogWarning("Write to nonexistent cartridge RAM. Address: {Address:X4}, Value: {Value:X2}", address, value);
            }
            else
            {
                log.LogError("Write to non cartridge memory space. Address: {Address:X4}, Value: {Value:X2}", address, value);
            }
        }
    }

    private readonly byte[] rom;
    private RomCartridgeState state;

    private readonly ILogger log;
}