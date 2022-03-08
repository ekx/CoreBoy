namespace CoreBoy.Core.Cartridges.Utils;

public readonly struct RamSize
{
    public int BankSize { get; }
    public int BankCount { get; }
    public int Total => BankSize * BankCount;

    public RamSize(int bankSize, int bankCount)
    {
        BankSize = bankSize;
        BankCount = bankCount;
    }
}