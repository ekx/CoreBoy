namespace CoreBoy.Core.Cartridges
{
    public interface ICartridge
    {
        ICartridgeState State { get; set; }
        byte this[ushort address] { get; set; }
    }

    public interface ICartridgeState { }
}
