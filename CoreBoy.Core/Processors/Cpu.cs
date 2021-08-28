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
            
            this.mmu.InterruptTriggeredHandler += OnInterruptTriggered;
        }

        public void Reset()
        {
            log.LogInformation("CPU reset");

            State = new CpuState();
        }

        public void RunInstructionCycle()
        {
            if (State.Halt || State.Stop)
            {
                // TODO: Check if this is correct
                mmu.UpdateState(0);
                return;
            }

            try
            {
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
        
        private void OnInterruptTriggered(InterruptType interruptType)
        {
            if (!State.MasterInterruptEnable) 
                return;
            
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
