using System.Runtime.Serialization;
using CoreBoy.Core.Cartridges.Interfaces;
using CoreBoy.Core.Processors.State;

namespace CoreBoy.Core.Utils;

[DataContract]
public class SaveState
{
    [DataMember]
    public CpuState CpuState;
    [DataMember]
    public MmuState MmuState;
    [DataMember]
    public PpuState PpuState;
    [DataMember]
    public SpuState SpuState;
    [DataMember]
    public ICartridgeState CartridgeState;
}