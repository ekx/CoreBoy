using CoreBoy.Core.Utils;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public partial class Cpu
    {
        private void DisableInterrupts()
        {
            log.LogError("Disable interrupts not implemented.");
        }

        private void EnableInterrupts()
        {
            log.LogError("Enable interrupts not implemented.");
        }

        private void LoadSPIntoHL()
        {
            byte value = ReadByte(State.PC++); 

            SetFlag(RegisterFlag.C, (State.SP.Low + value) > 0xFF);
            SetFlag(RegisterFlag.H, ((State.SP & 0x0F) + (value & 0x0F)) > 0x0F);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.Z, false);

            State.HL = (ushort)(State.SP + (sbyte)value);

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
            var address = ReadWord(State.PC);
            State.PC += 2;

            if (condition)
            {
                State.PC = address;
                Idle();
            }
        }

        private void JumpRelative(bool condition)
        {
            var offset = (sbyte)ReadByte(State.PC++);

            if (condition)
            {
                State.PC = (ushort)(State.PC.Value + offset);
                Idle();
            }
        }

        private void Push(RegisterWord register)
        {
            WriteByte(--State.SP, register.High);
            WriteByte(--State.SP, register.Low);

            Idle();
        }

        private void Pop(ref RegisterWord register)
        {
            register.Low = ReadByte(State.SP++);
            register.High = ReadByte(State.SP++);
        }

        private void Call(bool condition)
        {
            var address = ReadWord(State.PC);
            State.PC += 2;

            if (condition)
            {
                Push(State.PC);
                State.PC = address;
            }
        }

        private void Return()
        {
            byte low = ReadByte(State.SP++);
            byte high = ReadByte(State.SP++);
            State.PC = (ushort)((high << 8) | low);
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
            int result = State.AF.High + value;
            int resultLow = (State.AF.High & 0x0F) + (value & 0x0F);

            if (carry && GetFlag(RegisterFlag.C))
            {
                result++;
                resultLow++;
            }

            SetFlag(RegisterFlag.C, result > 0xFF);
            SetFlag(RegisterFlag.H, resultLow > 0x0F);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.Z, (result & 0xFF) == 0);

            State.AF.High = (byte)result;
        }

        public void Add(ref RegisterWord target, ushort value)
        {
            int result = target.Value + value;
            int resultLow = (target.Value & 0x0FFF) + (value & 0x0FFF);

            SetFlag(RegisterFlag.C, result > 0xFFFF);
            SetFlag(RegisterFlag.H, resultLow > 0x0FFF);
            SetFlag(RegisterFlag.N, false);

            if (target == State.SP)
            {
                SetFlag(RegisterFlag.Z, false);
                Idle();
            }
            Idle();

            target = (ushort)result;
        }

        public void Subtract(byte value, bool carry, bool assign = true)
        {
            int result = State.AF.High - value;
            int resultLow = (State.AF.High & 0x0F) - (value & 0x0F);

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
                State.AF.High = (byte)result;
            }
        }

        private void And(byte value)
        {
            State.AF.High = (byte)(State.AF.High & value);

            SetFlag(RegisterFlag.Z, State.AF.High == 0x00);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.H, true);
            SetFlag(RegisterFlag.C, false);
        }

        private void ExclusiveOr(byte value)
        {
            State.AF.High = (byte)(State.AF.High ^ value);

            SetFlag(RegisterFlag.Z, State.AF.High == 0x00);
            SetFlag(RegisterFlag.N, false);
            SetFlag(RegisterFlag.H, false);
            SetFlag(RegisterFlag.C, false);
        }

        private void Or(byte value)
        {
            State.AF.High = (byte)(State.AF.High | value);

            SetFlag(RegisterFlag.Z, State.AF.High == 0x00);
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
            bool bit7 = (value >> 7) == 1;
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
            bool bit0 = (value & 1) == 1;
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
