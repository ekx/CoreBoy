using CoreBoy.Core.Utils;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public sealed partial class Cpu
    {
        private void DisableInterrupts()
        {
            log.LogError("Disable interrupts not implemented");
        }

        private void EnableInterrupts()
        {
            log.LogError("Enable interrupts not implemented");
        }

        private void LoadSpIntoHl()
        {
            var value = ReadByte(State.Pc++); 

            SetFlag(RegisterFlag.C, (State.Sp.Low + value) > 0xFF);
            SetFlag(RegisterFlag.H, ((State.Sp & 0x0F) + (value & 0x0F)) > 0x0F);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.Z, false);

            State.Hl = (ushort)(State.Sp + (sbyte)value);

            Idle();
        }

        private byte Increment(byte value)
        {
            value++;

            SetFlag(RegisterFlag.H, (value & 0x0F) == 0);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.Z, value == 0);

            return value;
        }

        private byte Decrement(byte value)
        {
            value--;

            SetFlag(RegisterFlag.H, (value & 0x0F) == 0x0F);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.Z, value == 0);

            return value;
        }

        private void Jump(bool condition)
        {
            var address = ReadWord(State.Pc);
            State.Pc += 2;

            if (!condition) return;
            
            State.Pc = address;
            Idle();
        }

        private void JumpRelative(bool condition)
        {
            var offset = (sbyte)ReadByte(State.Pc++);

            if (!condition) return;
            
            State.Pc = (ushort)(State.Pc.Value + offset);
            Idle();
        }

        private void Push(RegisterWord register)
        {
            WriteByte(--State.Sp, register.High);
            WriteByte(--State.Sp, register.Low);

            Idle();
        }

        private void Pop(ref RegisterWord register)
        {
            register.Low = ReadByte(State.Sp++);
            register.High = ReadByte(State.Sp++);
        }

        private void Call(bool condition)
        {
            var address = ReadWord(State.Pc);
            State.Pc += 2;

            if (!condition) return;
            
            Push(State.Pc);
            State.Pc = address;
        }

        private void Return()
        {
            var low = ReadByte(State.Sp++);
            var high = ReadByte(State.Sp++);
            State.Pc = (ushort)((high << 8) | low);
            Idle();
        }

        private void Return(bool condition)
        {
            Idle();

            if (condition)
            {
                Return();
            }
        }

        public void Add(byte value, bool carry)
        {
            var result = State.Af.High + value;
            var resultLow = (State.Af.High & 0x0F) + (value & 0x0F);

            if (carry && GetFlag(RegisterFlag.C))
            {
                result++;
                resultLow++;
            }

            SetFlag(RegisterFlag.C, result > 0xFF);
            SetFlag(RegisterFlag.H, resultLow > 0x0F);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.Z, (result & 0xFF) == 0);

            State.Af.High = (byte)result;
        }

        public void Add(ref RegisterWord target, ushort value)
        {
            var result = target.Value + value;
            var resultLow = (target.Value & 0x0FFF) + (value & 0x0FFF);

            SetFlag(RegisterFlag.C, result > 0xFFFF);
            SetFlag(RegisterFlag.H, resultLow > 0x0FFF);
            SetFlag(RegisterFlag.N, false);

            if (target == State.Sp)
            {
                SetFlag(RegisterFlag.Z, false);
                Idle();
            }
            Idle();

            target = (ushort)result;
        }

        public void Subtract(byte value, bool carry, bool assign = true)
        {
            var result = State.Af.High - value;
            var resultLow = (State.Af.High & 0x0F) - (value & 0x0F);

            if (carry && GetFlag(RegisterFlag.C))
            {
                result--;
                resultLow--;
            }

            SetFlag(RegisterFlag.C, result < 0);
            SetFlag(RegisterFlag.H, resultLow < 0);
            SetFlag(RegisterFlag.N, true);
            SetFlag(RegisterFlag.Z, (result & 0xFF) == 0);

            if (assign)
            {
                State.Af.High = (byte)result;
            }
        }

        private void And(byte value)
        {
            State.Af.High = (byte)(State.Af.High & value);

            SetFlag(RegisterFlag.Z, State.Af.High == 0x00);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.H, true);
            SetFlag(RegisterFlag.C, false);
        }

        private void ExclusiveOr(byte value)
        {
            State.Af.High = (byte)(State.Af.High ^ value);

            SetFlag(RegisterFlag.Z, State.Af.High == 0x00);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.H, false);
            SetFlag(RegisterFlag.C, false);
        }

        private void Or(byte value)
        {
            State.Af.High = (byte)(State.Af.High | value);

            SetFlag(RegisterFlag.Z, State.Af.High == 0x00);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.H, false);
            SetFlag(RegisterFlag.C, false);
        }

        private void Compare(byte value)
        {
            Subtract(value, false, false);
        }

        #region CB instructions

        private void RotateLeft(ref RegisterByte value, bool carry)
        {
            var bit7 = (value >> 7) == 1;
            value = (byte)(value << 1);

            if (carry)
            {
                value[0] = bit7;
            }
            else
            {
                value[0] = GetFlag(RegisterFlag.C);
            }

            SetFlag(RegisterFlag.C, bit7);
        }

        private void RotateLeft(ushort address, bool carry)
        {
            RegisterByte value = ReadByte(address);
            RotateLeft(ref value, carry);
            WriteByte(address, value);
        }

        private void RotateRight(ref RegisterByte value, bool carry)
        {
            var bit0 = (value & 1) == 1;
            value = (byte)(value >> 1);

            if (carry)
            {
                value[7] = bit0;
            }
            else
            {
                value[7] = GetFlag(RegisterFlag.C);
            }

            SetFlag(RegisterFlag.C, bit0);
        }

        private void RotateRight(ushort address, bool carry)
        {
            RegisterByte value = ReadByte(address);
            RotateRight(ref value, carry);
            WriteByte(address, value);
        }

        private void TestBit(byte value, int bitIndex)
        {
            SetFlag(RegisterFlag.Z, !value.GetBit(bitIndex));
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.H, true);
        }

        #endregion
    }
}
