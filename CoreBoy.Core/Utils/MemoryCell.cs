using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CoreBoy.Core.Utils
{
    [DataContract]
    public class MemoryCell
    {
        public byte Value
        {
            get => value;
            set
            {
                this.value = value;

                foreach (var key in lockedBits.Keys)
                {
                    this.value = this.value.SetBit(key, lockedBits[key]);
                }
            }
        }

        public bool this[int bitIndex]
        {
            get => Value.GetBit(bitIndex);

            set => Value = Value.SetBit(bitIndex, value);
        }

        public MemoryCell()
        {
            value = 0x00;
            lockedBits = new Dictionary<int, bool>();
        }

        public void LockBit(int index, bool valueIn)
        {
            if (!lockedBits.ContainsKey(index))
            {
                lockedBits.Add(index, valueIn);
            }
            else
            {
                lockedBits[index] = valueIn;
            }

            value = value.SetBit(index, valueIn);
        }

        public override string ToString()
        {
            return $"Value: {Convert.ToString(Value, 2)}";
        }

        [DataMember]
        private byte value;
        [DataMember]
        private Dictionary<int, bool> lockedBits;
    }
}
