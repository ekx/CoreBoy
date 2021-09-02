namespace CoreBoy.Core.Utils.Memory
{
    public interface IMemoryCell
    {
        byte Value { get; set; }
        
        bool this[int bitIndex] { get; set; }

        void LockBit(int index, bool valueIn);

        void LockBits(int startIndex, int numberOfBits, bool value);
    }
}