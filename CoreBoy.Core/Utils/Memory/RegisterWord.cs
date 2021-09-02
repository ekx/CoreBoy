using System.Runtime.Serialization;

namespace CoreBoy.Core.Utils.Memory
{
    [DataContract]
    public class RegisterWord
    {
        [DataMember]
        public RegisterByte Low;
        [DataMember]
        public RegisterByte High;

        public ushort Value
        {
            get => (ushort)(Low + (High << 8));

            private init
            {
                Low = (byte)(value & 0x00FF);
                High = (byte)(value >> 8);
            }
        }

        public RegisterWord()
        {
            Low = 0x00;
            High = 0x00;
        }

        public RegisterWord(ushort value)
        {
            Value = value;
        }

        public static implicit operator RegisterWord(ushort value)
        {
            return new RegisterWord(value);
        }

        public static implicit operator ushort(RegisterWord register)
        {
            return register.Value;
        }

        public override string ToString()
        {
            return $"High: {High:X2}, Low: {Low:X2}";
        }
    }
}
