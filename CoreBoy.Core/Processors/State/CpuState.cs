using System.Runtime.Serialization;
using CoreBoy.Core.Utils.Memory;

namespace CoreBoy.Core.Processors.State;

[DataContract]
public class CpuState
{
    [DataMember]
    public RegisterWord Af = new();
    [DataMember]
    public RegisterWord Bc = new();
    [DataMember]
    public RegisterWord De = new();
    [DataMember]
    public RegisterWord Hl = new();
    [DataMember]
    public RegisterWord Sp = new();
    [DataMember]
    public RegisterWord Pc = new();

    [DataMember]
    public bool Halt;
    [DataMember]
    public bool Stop;
    [DataMember]
    public bool MasterInterruptEnable;

    [DataMember]
    public long Clock;

    public override string ToString()
    {
        return $"AF: {Af.Value:X4}, BC: {Bc.Value:X4}, DE: {De.Value:X4}, HL: {Hl.Value:X4}, SP: {Sp.Value:X4}, PC: {Pc.Value:X4}";
    }
}