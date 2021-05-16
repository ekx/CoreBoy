namespace CoreBoy.Core.Processors
{
    public interface ICpu
    {
        CpuState State { get; set; }

        void Reset();
        void RunInstructionCycle();
    }
}
