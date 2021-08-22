using CoreBoy.Core.Cartridges;

namespace CoreBoy.Core.Processors
{
    public interface IMmu
    {
        MmuState State { get; set; }
        ICartridgeState CartridgeState { get; set; }

        void Reset();
        void LoadBootRom(byte[] bootRomIn);
        void LoadCartridge(ICartridge cartridgeIn);
        void UpdateState(long cycles);
        byte this[ushort address] { get; set; }
    }
}
