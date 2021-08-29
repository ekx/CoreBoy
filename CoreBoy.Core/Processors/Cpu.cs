using System.Collections.Generic;
using CoreBoy.Core.Utils;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public sealed partial class Cpu : ICpu
    {
        public CpuState State { get; set; }

        public Cpu(ILogger<Cpu> log, IMmu mmu)
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
                if (!State.Stop && !State.Halt)
                {
                    // Fetch
                    var opcode = ReadByte(State.Pc++);

                    // Decode
                    var instruction = opcodeTable[opcode];

                    // Execute
                    instruction();
                }
                else
                {
                    //Idle();
                    UpdateClock(0);
                }
                
                HandleInterrupts();
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

        public ushort ReadWord(ushort address)
        {
            return (ushort)(ReadByte(address) + (ReadByte(++address) << 8));
        }

        public void WriteWord(ushort address, ushort value)
        {
            WriteByte(address, (byte)(value & 0x00FF));
            WriteByte(++address, (byte)(value >> 8));
        }

        private void SetFlag(RegisterFlag flag, bool value)
        {
            State.Af.Low[(int)flag] = value;
        }

        private bool GetFlag(RegisterFlag flag)
        {
            return State.Af.Low[(int)flag];
        }
        
        private void HandleInterrupts()
        {
            if (!State.MasterInterruptEnable)
            {
                // TODO: Check if all IF flags are discarded each cycle
                // mmu.State.Io[MmuIo.IF].Value = 0x00;
                return;
            }

            if (mmu.State.Io[MmuIo.IF][InterruptFlag.VBlank] && mmu.State.Io[MmuIo.IE][InterruptEnable.VBlank])
            {
                mmu.State.Io[MmuIo.IF][InterruptFlag.VBlank] = false;
                HandleInterrupt(InterruptType.VBlank);
            }
            else if (mmu.State.Io[MmuIo.IF][InterruptFlag.LcdStatus] && mmu.State.Io[MmuIo.IE][InterruptEnable.LcdStatus])
            {
                mmu.State.Io[MmuIo.IF][InterruptFlag.LcdStatus] = false;
                HandleInterrupt(InterruptType.LcdStatus);
            }
            else if (mmu.State.Io[MmuIo.IF][InterruptFlag.Timer] && mmu.State.Io[MmuIo.IE][InterruptEnable.Timer])
            {
                mmu.State.Io[MmuIo.IF][InterruptFlag.Timer] = false;
                HandleInterrupt(InterruptType.Timer);
            }
            else if (mmu.State.Io[MmuIo.IF][InterruptFlag.SerialTransfer] && mmu.State.Io[MmuIo.IE][InterruptEnable.SerialTransfer])
            {
                mmu.State.Io[MmuIo.IF][InterruptFlag.SerialTransfer] = false;
                HandleInterrupt(InterruptType.SerialTransfer);
            }
            else if (mmu.State.Io[MmuIo.IF][InterruptFlag.Input] && mmu.State.Io[MmuIo.IE][InterruptEnable.Input])
            {
                mmu.State.Io[MmuIo.IF][InterruptFlag.Input] = false;
                HandleInterrupt(InterruptType.Input);
            }
            
            // TODO: Check if all IF flags are discarded each cycle
            // mmu.State.Io[MmuIo.IF].Value = 0x00;
        }

        private void HandleInterrupt(InterruptType interruptType)
        {
            State.MasterInterruptEnable = false;

            Idle();
            Idle();
                
            Push(State.Pc);
            State.Pc = (ushort)interruptType;
        }

        private readonly ILogger log;
        private readonly IMmu mmu;
    }

    [DataContract]
    public class CpuState
    {
        [DataMember]
        public RegisterWord Af = new();
        [DataMember]
        public RegisterWord Bc = new();
        [DataMember]
        public RegisterWord De = new();
        [DataMember]
        public RegisterWord Hl = new();
        [DataMember]
        public RegisterWord Sp = new();
        [DataMember]
        public RegisterWord Pc = new();

        [DataMember]
        public bool Halt;
        [DataMember]
        public bool Stop;
        [DataMember]
        public bool MasterInterruptEnable;

        [DataMember]
        public long Clock;

        public override string ToString()
        {
            return $"AF: {Af.Value:X4}, BC: {Bc.Value:X4}, DE: {De.Value:X4}, HL: {Hl.Value:X4}, SP: {Sp.Value:X4}, PC: {Pc.Value:X4}";
        }
    }
}
