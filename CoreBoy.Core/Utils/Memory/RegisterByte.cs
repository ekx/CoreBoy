using System;
using System.Runtime.Serialization;

namespace CoreBoy.Core.Utils.Memory
{
    [DataContract]
    public class RegisterByte
    {
        [DataMember]
        public byte Value;

        public RegisterByte()
        {
            Value = 0x00;
        }

        public RegisterByte(byte value)
        {
            Value = value;
        }

        public static implicit operator RegisterByte(byte value)
        {
            return new RegisterByte(value);
        }

        public static implicit operator byte(RegisterByte register)
        {
            return register.Value;
        }

        public bool this[int bitIndex]
        {
            get => Value.GetBit(bitIndex);

            set => Value = Value.SetBit(bitIndex, value);
        }

        public override string ToString()
        {
            return $"Value: {Convert.ToString(Value, 2)}";
        }
    }
}
