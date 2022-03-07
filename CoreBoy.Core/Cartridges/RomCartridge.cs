using System;
using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;
using CoreBoy.Core.Cartridges.Interfaces;
using CoreBoy.Core.Cartridges.State;
using CoreBoy.Core.Utils;

namespace CoreBoy.Core.Cartridges;

[DataContract]
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

        if (data.Length != 32768)
            throw new ArgumentOutOfRangeException(nameof(data), "Cartridge data has invalid length");

        header = new CartridgeHeader(log, data);
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
                //log.LogDebug($"Read from Cartridge ROM. Address: {address:X4}, Value: {rom[address]:X2}");
                return rom[address];
            }
            // [A000-BFFF] Cartridge RAM
            else if (address is >= 0xA000 and < 0xC000)
            {
                log.LogWarning($"Read from nonexistent cartridge RAM. Address: {address:X4}");
                return 0x00;
            }
            else
            {
                log.LogError($"Read from non cartridge memory space. Address: {address:X4}");
                return 0x00;
            }
        }

        set
        {
            // [0000-7FFF] Cartridge ROM
            if (address < 0x8000)
            {
                log.LogWarning($"Write to read-only cartridge ROM. Address: {address:X4}, Value: {value:X2}");
            }
            // [A000-BFFF] Cartridge RAM
            else if (address is >= 0xA000 and < 0xC000)
            {
                log.LogWarning($"Write to nonexistent cartridge RAM. Address: {address:X4}, Value: {value:X2}");
            }
            else
            {
                log.LogError($"Write to non cartridge memory space. Address: {address:X4}, Value: {value:X2}");
            }
        }
    }

    private CartridgeHeader header;
    private readonly byte[] rom;
    private RomCartridgeState state;

    private readonly ILogger log;
}