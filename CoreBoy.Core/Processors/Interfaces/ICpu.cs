using CoreBoy.Core.Processors.State;

namespace CoreBoy.Core.Processors.Interfaces;

public interface ICpu
{
    CpuState State { get; set; }

    void Reset();
    void RunInstructionCycle();
}