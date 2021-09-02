namespace CoreBoy.Core.Processors.Interfaces
{
    public interface ISpu
    {
        SpuState State { get; set; }

        void Reset();
        void UpdateState(long cycles);
        byte this[ushort address] { get; set; }
    }
}
