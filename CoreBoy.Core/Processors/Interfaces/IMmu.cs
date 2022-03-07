using CoreBoy.Core.Cartridges.Interfaces;
using CoreBoy.Core.Processors.State;
using CoreBoy.Core.Utils;

namespace CoreBoy.Core.Processors.Interfaces;

public interface IMmu
{
    MmuState State { get; set; }
    ICartridgeState CartridgeState { get; set; }

    void Reset();
    void LoadBootRom(byte[] bootRomIn);
    void LoadCartridge(ICartridge cartridgeIn);
    void UpdateState(long cycles);
    void SetInput(InputState inputState);
    byte this[ushort address] { get; set; }
}