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

        State = new Mbc1CartridgeState();
    }

    public byte this[ushort address]
    {
        get => throw new System.NotImplementedException();
        set => throw new System.NotImplementedException();
    }
    
    private readonly byte[] rom;
    private Mbc1CartridgeState state;
    
    private readonly CartridgeHeader header;
    private readonly ILogger log;
}