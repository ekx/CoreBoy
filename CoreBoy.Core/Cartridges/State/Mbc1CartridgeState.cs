using System;
using System.Runtime.Serialization;
using CoreBoy.Core.Cartridges.Interfaces;
using CoreBoy.Core.Cartridges.Utils;
using CoreBoy.Core.Utils.Memory;

namespace CoreBoy.Core.Cartridges.State;

[DataContract]
public class Mbc1CartridgeState : ICartridgeState
{
    public Mbc1CartridgeState(RamSize ramSize)
    {
        RamEnable = new MemoryCell();
        RomBank = new MemoryCell();
        RamBank = new MemoryCell();
        ModeSelect = new MemoryCell();
        
        RamEnable.LockBits(4, 4, true);
        RomBank.LockBits(5, 3, true);
        RamBank.LockBits(2, 6, true);
        ModeSelect.LockBits(1, 7, true);

        CartridgeRam = new byte[ramSize.Total];
        var random = new Random();
        random.NextBytes(CartridgeRam);
    }
    
    [DataMember]
    public MemoryCell RamEnable { get; set; }
    
    [DataMember]
    public MemoryCell RomBank { get; set; }
    
    [DataMember]
    public MemoryCell RamBank { get; set; }
    
    [DataMember]
    public MemoryCell ModeSelect { get; set; }
    
    [DataMember]
    public byte[] CartridgeRam { get; set; }
}