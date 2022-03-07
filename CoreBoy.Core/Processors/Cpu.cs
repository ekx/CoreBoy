using System.Collections.Generic;
using CoreBoy.Core.Utils;
using CoreBoy.Core.Processors.Interfaces;
using CoreBoy.Core.Processors.State;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors;

public sealed partial class Cpu : ICpu
{
    public CpuState State { get; set; }

    public Cpu(ILogger log, IMmu mmu)
    {
        InitOpcodes();

        this.log = log;
        this.mmu = mmu;
    }

    public void Reset()
    {
        log.LogInformation("CPU reset");

        State = new CpuState();
    }

    public void RunInstructionCycle()
    {
        try
        {
            if (HandleInterrupts() || State.Stop || State.Halt) return;
            
            // Fetch
            var opcode = ReadByte(State.Pc++);

            // Decode
            var instruction = opcodeTable[opcode];

            // Execute
            instruction();
        }
        catch (KeyNotFoundException e)
        {
            throw new MissingOpcodeException($"Unimplemented opcode encountered: {ReadByte(--State.Pc):X2}", e);
        }
    }

    private void UpdateClock(long cycles)
    {
        State.Clock += cycles;
        mmu.UpdateState(cycles);
    }

    private void Idle()
    {
        UpdateClock(4);
    }

    private byte ReadByte(ushort address)
    {
        UpdateClock(4);
        return mmu[address];
    }

    private void WriteByte(ushort address, byte value)
    {
        UpdateClock(4);
        mmu[address] = value;
    }

    private ushort ReadWord(ushort address)
    {
        return (ushort) (ReadByte(address) + (ReadByte(++address) << 8));
    }

    private void WriteWord(ushort address, ushort value)
    {
        WriteByte(address, (byte) (value & 0x00FF));
        WriteByte(++address, (byte) (value >> 8));
    }

    private void SetFlag(RegisterFlag flag, bool value)
    {
        State.Af.Low[(int) flag] = value;
    }

    private bool GetFlag(RegisterFlag flag)
    {
        return State.Af.Low[(int) flag];
    }

    private bool HandleInterrupts()
    {
        if (!State.MasterInterruptEnable)
        {
            // TODO: Check if all IF flags are discarded each cycle
            // mmu.State.Io[MmuIo.IF].Value = 0x00;
            return false;
        }

        if (mmu.State.Io[MmuIo.IF][InterruptFlag.VBlank] && 
            mmu.State.Io[MmuIo.IE][InterruptEnable.VBlank])
        {
            mmu.State.Io[MmuIo.IF][InterruptFlag.VBlank] = false;
            return HandleInterrupt(InterruptType.VBlank);
        }
        else if (mmu.State.Io[MmuIo.IF][InterruptFlag.LcdStatus] && 
                 mmu.State.Io[MmuIo.IE][InterruptEnable.LcdStatus])
        {
            mmu.State.Io[MmuIo.IF][InterruptFlag.LcdStatus] = false;
            return HandleInterrupt(InterruptType.LcdStatus);
        }
        else if (mmu.State.Io[MmuIo.IF][InterruptFlag.Timer] && 
                 mmu.State.Io[MmuIo.IE][InterruptEnable.Timer])
        {
            mmu.State.Io[MmuIo.IF][InterruptFlag.Timer] = false;
            return HandleInterrupt(InterruptType.Timer);
        }
        else if (mmu.State.Io[MmuIo.IF][InterruptFlag.SerialTransfer] &&
                 mmu.State.Io[MmuIo.IE][InterruptEnable.SerialTransfer])
        {
            mmu.State.Io[MmuIo.IF][InterruptFlag.SerialTransfer] = false;
            return HandleInterrupt(InterruptType.SerialTransfer);
        }
        else if (mmu.State.Io[MmuIo.IF][InterruptFlag.Input] &&
                 mmu.State.Io[MmuIo.IE][InterruptEnable.Input])
        {
            mmu.State.Io[MmuIo.IF][InterruptFlag.Input] = false;
            return HandleInterrupt(InterruptType.Input);
        }

        // TODO: Check if all IF flags are discarded each cycle
        // mmu.State.Io[MmuIo.IF].Value = 0x00;
        return false;
    }

    private bool HandleInterrupt(InterruptType interruptType)
    {
        State.MasterInterruptEnable = false;
        State.Halt = false;
        State.Stop = false;

        Idle();
        Idle();

        Push(State.Pc);
        State.Pc = (ushort) interruptType;

        return true;
    }

    private readonly ILogger log;
    private readonly IMmu mmu;
}