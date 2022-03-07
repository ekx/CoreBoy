using CoreBoy.Core.Utils;
using CoreBoy.Core.Utils.Memory;

namespace CoreBoy.Core.Processors;

public sealed partial class Cpu
{
    private void DisableInterrupts()
    {
        State.MasterInterruptEnable = false;
    }

    private void EnableInterrupts()
    {
        // TODO: Delay enable for one tick
        State.MasterInterruptEnable = true;
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

        if (!condition)
            return;

        State.Pc = address;
        Idle();
    }

    private void JumpRelative(bool condition)
    {
        var offset = (sbyte)ReadByte(State.Pc++);

        if (!condition)
            return;

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

        if (!condition)
            return;

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

    private void Reset(ushort address)
    {
        Push(State.Pc);
        State.Pc = address;
    }

    private void Add(byte value, bool carry)
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

    private void Add(ref RegisterWord target, ushort value)
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

    private void Subtract(byte value, bool carry, bool assign = true)
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

    private void DecimalAdjustA()
    {
        byte correction = 0;

        if (GetFlag(RegisterFlag.H) || !GetFlag(RegisterFlag.N) && (State.Af.High & 0xf) > 0x9)
        {
            correction |= 0x6;
        }

        if (GetFlag(RegisterFlag.C) || !GetFlag(RegisterFlag.N) && State.Af.High > 0x99)
        {
            correction |= 0x60;
            SetFlag(RegisterFlag.C, true);
        }

        State.Af.High = (byte)(State.Af.High + (GetFlag(RegisterFlag.N) ? -correction : correction));

        SetFlag(RegisterFlag.Z, State.Af.High == 0x0);
        SetFlag(RegisterFlag.H, false);
    }

    private void ComplementA()
    {
        State.Af.High = (byte)~State.Af.High;

        SetFlag(RegisterFlag.N, true);
        SetFlag(RegisterFlag.H, true);
    }

    private void ComplementCarryFlag()
    {
        SetFlag(RegisterFlag.N, false);
        SetFlag(RegisterFlag.H, false);
        SetFlag(RegisterFlag.C, !GetFlag(RegisterFlag.C));
    }

    private void SetCarryFlag()
    {
        SetFlag(RegisterFlag.N, false);
        SetFlag(RegisterFlag.H, false);
        SetFlag(RegisterFlag.C, true);
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

        SetFlag(RegisterFlag.Z, value.Value == 0);
        SetFlag(RegisterFlag.N, false);
        SetFlag(RegisterFlag.H, false);
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

        SetFlag(RegisterFlag.Z, value.Value == 0);
        SetFlag(RegisterFlag.N, false);
        SetFlag(RegisterFlag.H, false);
        SetFlag(RegisterFlag.C, bit0);
    }

    private void RotateRight(ushort address, bool carry)
    {
        RegisterByte value = ReadByte(address);
        RotateRight(ref value, carry);
        WriteByte(address, value);
    }

    private void ShiftLeft(ref RegisterByte value)
    {
        var bit7 = (value >> 7) == 1;
            
        value = (byte)(value << 1);
            
        SetFlag(RegisterFlag.Z, value.Value == 0);
        SetFlag(RegisterFlag.N, false);
        SetFlag(RegisterFlag.H, false);
        SetFlag(RegisterFlag.C, bit7);
    }
        
    private void ShiftLeft(ushort address)
    {
        RegisterByte value = ReadByte(address);
        ShiftLeft(ref value);
        WriteByte(address, value);
    }

    private void ShiftRight(ref RegisterByte value, bool keepMsb)
    {
        var bit0 = (value & 1) == 1;
        var bit7 = (value & 7) == 1;

        value = (byte)(value >> 1);

        if (keepMsb)
        {
            value[7] = bit7;
        }

        SetFlag(RegisterFlag.Z, value.Value == 0);
        SetFlag(RegisterFlag.N, false);
        SetFlag(RegisterFlag.H, false);
        SetFlag(RegisterFlag.C, bit0);
    }
        
    private void ShiftRight(ushort address, bool keepMsb)
    {
        RegisterByte value = ReadByte(address);
        ShiftRight(ref value, keepMsb);
        WriteByte(address, value);
    }

    private void Swap(ref RegisterByte target)
    {
        target = (byte) ((target & 0x0F) << 4 | (target & 0xF0) >> 4);
            
        SetFlag(RegisterFlag.Z, target == 0);
        SetFlag(RegisterFlag.N, false);
        SetFlag(RegisterFlag.H, false);
        SetFlag(RegisterFlag.C, false);
    }

    private void Swap(ushort address)
    {
        RegisterByte value = ReadByte(address);
        Swap(ref value);
        WriteByte(address, value);
    }
        
    private void TestBit(byte value, int bitIndex)
    {
        SetFlag(RegisterFlag.Z, !value.GetBit(bitIndex));
        SetFlag(RegisterFlag.N, false);
        SetFlag(RegisterFlag.H, true);
    }

    private void ResetBit(ref RegisterByte target, int bitIndex)
    {
        target[bitIndex] = false;
    }
        
    private void ResetBit(ushort address, int bitIndex)
    {
        RegisterByte value = ReadByte(address);
        ResetBit(ref value, bitIndex);
        WriteByte(address, value);
    }
        
    private void SetBit(ref RegisterByte target, int bitIndex)
    {
        target[bitIndex] = true;
    }
        
    private void SetBit(ushort address, int bitIndex)
    {
        RegisterByte value = ReadByte(address);
        SetBit(ref value, bitIndex);
        WriteByte(address, value);
    }

    #endregion
}