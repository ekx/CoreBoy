using System;
using System.Runtime.Serialization;
using CoreBoy.Core.Utils;
using CoreBoy.Core.Utils.Memory;

namespace CoreBoy.Core.Processors.State;

[DataContract]
public class PpuState
{
    public PpuState()
    {
        Io.Populate(() => new MemoryCell());
        Io[GraphicsIo.STAT].LockBit(LcdStatus.Unused, true);

        var random = new Random();
        random.NextBytes(Vram);
        random.NextBytes(Oam);
    }

    [DataMember]
    public IMemoryCell[] Io = new IMemoryCell[12];
    [DataMember]
    public byte[] Vram = new byte[8192];
    [DataMember]
    public byte[] Oam = new byte[160];

    [DataMember]
    public long Clock;
}