using System;
using System.Runtime.Serialization;
using CoreBoy.Core.Utils;
using CoreBoy.Core.Utils.Memory;

namespace CoreBoy.Core.Processors.State;

[DataContract]
public class MmuState
{
    public MmuState()
    {
        Io.Populate(() => new UndefinedCell());

        Io[MmuIo.P1] = new InputCell();
        Io[MmuIo.SB] = new MemoryCell();
        Io[MmuIo.SC] = new MemoryCell();
        Io[MmuIo.DIV] = new MemoryCell();
        Io[MmuIo.TIMA] = new MemoryCell();
        Io[MmuIo.TMA] = new MemoryCell();
        Io[MmuIo.TAC] = new MemoryCell();
        Io[MmuIo.IF] = new MemoryCell();
        Io[MmuIo.BOOT] = new MemoryCell();
        Io[MmuIo.HDMA1] = new MemoryCell();
        Io[MmuIo.HDMA2] = new MemoryCell();
        Io[MmuIo.HDMA3] = new MemoryCell();
        Io[MmuIo.HDMA4] = new MemoryCell();
        Io[MmuIo.HDMA5] = new MemoryCell();
        Io[MmuIo.IE] = new MemoryCell();
            
        Io[MmuIo.P1].LockBits(6, 2, true);
        Io[MmuIo.SC].LockBits(2, 5, true);
        Io[MmuIo.TAC].LockBits(3, 5, true);
        Io[MmuIo.IF].LockBits(5, 3, true);
        Io[MmuIo.BOOT].LockBits(1, 7, true);
        Io[MmuIo.IE].LockBits(5, 3, true);
            
        var random = new Random();
        random.NextBytes(Wram);
        random.NextBytes(Hram);
    }

    [DataMember]
    public IMemoryCell[] Io = new IMemoryCell[129];

    [DataMember]
    public byte[] Wram = new byte[8192];
    [DataMember]
    public byte[] Hram = new byte[128];
}