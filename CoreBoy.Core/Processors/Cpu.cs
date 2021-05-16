using System;
using System.Collections.Generic;
using CoreBoy.Core.Utils;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Processors
{
    public partial class Cpu : ICpu
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

            this.State = new CpuState();
        }

        public void RunInstructionCycle()
        {
            if (State.Halt || State.Stop)
            {
                return;
            }

            try
            {
                // Fetch
                var opcode = ReadByte(State.PC++);

                // Decode
                var instruction = opcodeTable[opcode];

                // Execute
                instruction();       
            }
            catch (KeyNotFoundException e)
            {
                throw new MissingOpcodeException($"Unimplemented opcode encountered: {ReadByte(--State.PC):X2}", e);
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
            State.AF.Low[(int)flag] = value;
        }

        private bool GetFlag(RegisterFlag flag)
        {
            return State.AF.Low[(int)flag];
        }

        private IMmu mmu;

        private readonly ILogger log;
    }

    [DataContract]
    public class CpuState
    {
        [DataMember]
        public RegisterWord AF = new RegisterWord();
        [DataMember]
        public RegisterWord BC = new RegisterWord();
        [DataMember]
        public RegisterWord DE = new RegisterWord();
        [DataMember]
        public RegisterWord HL = new RegisterWord();
        [DataMember]
        public RegisterWord SP = new RegisterWord();
        [DataMember]
        public RegisterWord PC = new RegisterWord();

        [DataMember]
        public bool Halt = false;
        [DataMember]
        public bool Stop = false;

        [DataMember]
        public long Clock = 0;

        public override string ToString()
        {
            return $"AF: {AF.Value:X4}, BC: {BC.Value:X4}, DE: {DE.Value:X4}, HL: {HL.Value:X4}, SP: {SP.Value:X4}, PC: {PC.Value:X4}";
        }
    }
}
