namespace CoreBoy.Core.Utils.Memory
{
    public class UndefinedCell : IMemoryCell
    {
        public byte Value
        {
            get => 0xFF;
            set { }
        }

        public bool this[int bitIndex]
        {
            get => true;
            set { }
        }

        public void LockBit(int index, bool valueIn) { }

        public void LockBits(int startIndex, int numberOfBits, bool value) { }
    }
}