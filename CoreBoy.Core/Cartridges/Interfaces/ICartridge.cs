namespace CoreBoy.Core.Cartridges.Interfaces;

public interface ICartridge
{
    ICartridgeState State { get; set; }
    byte this[ushort address] { get; set; }
}