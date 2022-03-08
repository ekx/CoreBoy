namespace CoreBoy.Core.Cartridges.Utils;

public readonly struct RomSize
{
    public int BankSize { get; }
    public int BankCount { get; }
    public int Total => BankSize * BankCount;

    public RomSize(int bankCount)
    {
        BankSize = 16 * 1024;
        BankCount = bankCount;
    }
}