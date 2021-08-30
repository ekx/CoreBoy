using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Moq;
using CoreBoy.Core.Processors;
using CoreBoy.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Test
{
    [TestClass]
    public class CpuTest
    {
        public CpuTest()
        {
            mmu = new Mock<IMmu>();
            mmu.Setup(m => m[It.IsAny<ushort>()]).Returns((ushort address) => (byte)address);
            mmu.Setup(m => m.UpdateState(It.IsAny<long>()));

            cpu = new Cpu(Mock.Of<ILogger<Cpu>>(), mmu.Object);
        }

        [TestMethod]
        public void NoOperation()
        {
            cpu.Reset();
            cpu.State.Pc = 0x00;

            cpu.RunInstructionCycle();
            
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Stop()
        {
            cpu.Reset();
            cpu.State.Pc = 0x10;

            cpu.RunInstructionCycle();
            
            Assert.AreEqual(true, cpu.State.Stop);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Halt()
        {
            cpu.Reset();
            cpu.State.Pc = 0x76;

            cpu.RunInstructionCycle();
            
            Assert.AreEqual(true, cpu.State.Halt);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void DisableInterrupts()
        {
            cpu.Reset();
            cpu.State.MasterInterruptEnable = true;
            cpu.State.Pc = 0xF3;

            cpu.RunInstructionCycle();
            
            Assert.AreEqual(false, cpu.State.MasterInterruptEnable);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void EnableInterrupts()
        {
            cpu.Reset();
            cpu.State.Pc = 0xFB;

            var mmuIo = new MemoryCell[129];
            mmuIo[MmuIo.IF] = new MemoryCell { Value = 0x00 };
            mmuIo[MmuIo.IE] = new MemoryCell { Value = 0x00 };
            var mmuState = new MmuState
            {
                Io = mmuIo
            };
            mmu.Setup(m => m.State).Returns(mmuState);
            
            cpu.RunInstructionCycle();
            
            Assert.AreEqual(true, cpu.State.MasterInterruptEnable);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void TriggerInterrupt()
        {
            cpu.Reset();
            cpu.State.MasterInterruptEnable = true;
            cpu.State.Pc = 0x1234;
            cpu.State.Sp = 0x5678;
            cpu.State.Halt = true;
            
            var mmuIo = new MemoryCell[129];
            mmuIo[MmuIo.IF] = new MemoryCell { Value = 0x01 };
            mmuIo[MmuIo.IE] = new MemoryCell { Value = 0x01 };
            var mmuState = new MmuState
            {
                Io = mmuIo
            };
            mmu.Setup(m => m.State).Returns(mmuState);
            
            cpu.RunInstructionCycle();
            
            mmu.VerifySet(m => m[0x5677] = 0x12, Times.Once());
            mmu.VerifySet(m => m[0x5676] = 0x34, Times.Once());
            Assert.AreEqual(0x5676, cpu.State.Sp.Value);
            Assert.AreEqual(cpu.State.Pc.Value, (ushort)InterruptType.VBlank);
            Assert.AreEqual(false, cpu.State.MasterInterruptEnable);
            Assert.AreEqual(20, cpu.State.Clock);
        }
        
        #region Loads

        // 8-Bit

        [TestMethod]
        public void Load8BitImmediateIntoRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x06;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x07, cpu.State.Bc.High.Value);
            Assert.AreEqual(0x08, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitImmediateIntoMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x36;
            cpu.State.Hl = 0xFFFF;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0xFFFF] = 0x37, Times.Once());
            Assert.AreEqual(0x38, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitRegisterIntoMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x02;
            cpu.State.Af.High.Value = 0x12;
            cpu.State.Bc = 0xFFFF;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0xFFFF] = 0x12, Times.Once());
            Assert.AreEqual(0x03, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitMemoryIntoRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x0A;
            cpu.State.Bc = 0x12;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x12, cpu.State.Af.High.Value);
            Assert.AreEqual(0x0B, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitRegisterIntoRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x41;
            cpu.State.Bc = 0x00FF;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFFFF, cpu.State.Bc.Value);
            Assert.AreEqual(0x42, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitRegisterIntoOffsetAddress()
        {
            cpu.Reset();
            cpu.State.Pc = 0xE0;
            cpu.State.Af.High.Value = 0x12;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0xFFE1] = 0x12, Times.Once());
            Assert.AreEqual(0xE2, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitOffsetAddressIntoRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xF0;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xF1, cpu.State.Af.High.Value);
            Assert.AreEqual(0xF2, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitRegisterIntoImmediateAddress()
        {
            cpu.Reset();
            cpu.State.Pc = 0xEA;
            cpu.State.Af.High.Value = 0x12;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0xECEB] = 0x12, Times.Once());
            Assert.AreEqual(0xED, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitImmediateAddressIntoRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xFA;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFB, cpu.State.Af.High.Value);
            Assert.AreEqual(0xFD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        // 16-Bit

        [TestMethod]
        public void Load16BitImmediateIntoRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x01;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0302, cpu.State.Bc.Value);
            Assert.AreEqual(0x04, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadSpIntoMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x08;
            cpu.State.Sp = 0x1234;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x0A09] = 0x34, Times.Once());
            mmu.VerifySet(m => m[0x0A0A] = 0x12, Times.Once());
            Assert.AreEqual(0x0B, cpu.State.Pc.Value);
            Assert.AreEqual(20, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadSpIntoHl()
        {
            cpu.Reset();
            cpu.State.Pc = 0xF8;
            cpu.State.Sp = 0x01FF;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x30, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x01F8, cpu.State.Hl.Value);
            Assert.AreEqual(0xFA, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadHlIntoSp()
        {
            cpu.Reset();
            cpu.State.Pc = 0xF9;
            cpu.State.Hl = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x1234, cpu.State.Sp.Value);
            Assert.AreEqual(0xFA, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        // Special

        [TestMethod]
        public void LoadIncrementRegisterIntoMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x22;
            cpu.State.Hl = 0x1234;
            cpu.State.Af.High.Value = 0x56;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1234] = 0x56, Times.Once());
            Assert.AreEqual(0x1235, cpu.State.Hl.Value);
            Assert.AreEqual(0x23, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadIncrementMemoryIntoRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x2A;
            cpu.State.Hl = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x34, cpu.State.Af.High.Value);
            Assert.AreEqual(0x1235, cpu.State.Hl.Value);
            Assert.AreEqual(0x2B, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadDecrementRegisterIntoMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x32;
            cpu.State.Hl = 0x1234;
            cpu.State.Af.High.Value = 0x56;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1234] = 0x56, Times.Once());
            Assert.AreEqual(0x1233, cpu.State.Hl.Value);
            Assert.AreEqual(0x33, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadDecrementMemoryIntoRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x3A;
            cpu.State.Hl = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x34, cpu.State.Af.High.Value);
            Assert.AreEqual(0x1233, cpu.State.Hl.Value);
            Assert.AreEqual(0x3B, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Increments

        [TestMethod]
        public void Increment8BitRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x04;
            cpu.State.Bc.High.Value = 0xFF;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xA0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x00, cpu.State.Bc.High.Value);
            Assert.AreEqual(0x05, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Increment8BitMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x34;
            cpu.State.Hl = 0x123F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x20, cpu.State.Af.Low.Value);
            mmu.VerifySet(m => m[0x123F] = 0x40, Times.Once());
            Assert.AreEqual(0x35, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Increment16BitRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x03;
            cpu.State.Bc = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x1235, cpu.State.Bc.Value);
            Assert.AreEqual(0x04, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Decrements

        [TestMethod]
        public void Decrement8BitRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x05;
            cpu.State.Bc.High.Value = 0x01;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x80, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x00, cpu.State.Bc.High.Value);
            Assert.AreEqual(0x06, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Decrement8BitMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x35;
            cpu.State.Hl = 0x1230;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x20, cpu.State.Af.Low.Value);
            mmu.VerifySet(m => m[0x1230] = 0x2F, Times.Once());
            Assert.AreEqual(0x36, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Decrement16BitRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x0B;
            cpu.State.Bc = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x1233, cpu.State.Bc.Value);
            Assert.AreEqual(0x0C, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Jumps

        [TestMethod]
        public void JumpImmediate()
        {
            cpu.Reset();
            cpu.State.Pc = 0xC3;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xC5C4, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        [TestMethod]
        public void JumpRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xE9;
            cpu.State.Hl = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x1234, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void JumpRelativeImmediate()
        {
            cpu.Reset();
            cpu.State.Pc = 0x18;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x33, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        #endregion
        
        #region Pop Push

        [TestMethod]
        public void Pop()
        {
            cpu.Reset();
            cpu.State.Pc = 0xC1;
            cpu.State.Sp = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x3534, cpu.State.Bc.Value);
            Assert.AreEqual(0x1236, cpu.State.Sp.Value);
            Assert.AreEqual(0xC2, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Push()
        {
            cpu.Reset();
            cpu.State.Pc = 0xC5;
            cpu.State.Sp = 0x1234;
            cpu.State.Bc = 0xF1F2;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1233] = 0xF1, Times.Once());
            mmu.VerifySet(m => m[0x1232] = 0xF2, Times.Once());
            Assert.AreEqual(0x1232, cpu.State.Sp.Value);
            Assert.AreEqual(0xC6, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region Call Return Reset

        [TestMethod]
        public void Call()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCD;
            cpu.State.Sp = 0x1234;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1233] = 0x00, Times.Once());
            mmu.VerifySet(m => m[0x1232] = 0xD0, Times.Once());
            Assert.AreEqual(0x1232, cpu.State.Sp.Value);
            Assert.AreEqual(0xCFCE, cpu.State.Pc.Value);
            Assert.AreEqual(24, cpu.State.Clock);
        }

        [TestMethod]
        public void Return()
        {
            cpu.Reset();
            cpu.State.Pc = 0xC9;
            cpu.State.Sp = 0x12;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x14, cpu.State.Sp.Value);
            Assert.AreEqual(0x1312, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }
        
        [TestMethod]
        public void Reset()
        {
            cpu.Reset();
            cpu.State.Pc = 0xC7;
            cpu.State.Sp = 0x12;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x0011] = 0x00, Times.Once());
            mmu.VerifySet(m => m[0x0010] = 0xC8, Times.Once());
            Assert.AreEqual(0x10, cpu.State.Sp.Value);
            Assert.AreEqual(0x00, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region Adds

        [TestMethod]
        public void Add8BitFromRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x80;
            cpu.State.Af.High.Value = 0x05;
            cpu.State.Bc.High.Value = 0x0F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x14, cpu.State.Af.High.Value);
            Assert.AreEqual(0x20, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x81, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Add8BitFromMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x86;
            cpu.State.Af.High.Value = 0x01;
            cpu.State.Af.Low.Value = 0x40;
            cpu.State.Hl = 0x00FF;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x00, cpu.State.Af.High.Value);
            Assert.AreEqual(0xB0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x87, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Add8BitFromImmediate()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCE;
            cpu.State.Af.High.Value = 0x05;
            cpu.State.Af.Low.Value = 0x10;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xD5, cpu.State.Af.High.Value);
            Assert.AreEqual(0x20, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xD0, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Add16BitFromRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x09;
            cpu.State.Hl = 0x0204;
            cpu.State.Bc = 0x1030;
            cpu.State.Af.Low.Value = 0x80;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x80, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x1234, cpu.State.Hl.Value);
            Assert.AreEqual(0x0A, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Add16BitFromImmediate ()
        {
            cpu.Reset();
            cpu.State.Pc = 0xE8;
            cpu.State.Sp = 0x1200;
            cpu.State.Af.Low.Value = 0x80;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x12E9, cpu.State.Sp.Value);
            Assert.AreEqual(0xEA, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region Subtracts

        [TestMethod]
        public void Subtract8BitFromRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0x90;
            cpu.State.Af.High.Value = 0x23;
            cpu.State.Bc.High.Value = 0x0F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x14, cpu.State.Af.High.Value);
            Assert.AreEqual(0x60, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x91, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Subtract8BitFromMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0x96;
            cpu.State.Af.High.Value = 0x01;
            cpu.State.Af.Low.Value = 0x40;
            cpu.State.Hl = 0x0002;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFF, cpu.State.Af.High.Value);
            Assert.AreEqual(0x70, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x97, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Subtract8BitFromImmediate()
        {
            cpu.Reset();
            cpu.State.Pc = 0xDE;
            cpu.State.Af.High.Value = 0xE1;
            cpu.State.Af.Low.Value = 0x10;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x01, cpu.State.Af.High.Value);
            Assert.AreEqual(0x60, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xE0, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Ands

        [TestMethod]
        public void AndRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xA0;
            cpu.State.Af.High.Value = 0xF0;
            cpu.State.Bc.High.Value = 0xF0;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xF0, cpu.State.Af.High.Value);
            Assert.AreEqual(0x20, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xA1, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void AndMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xA6;
            cpu.State.Af.High.Value = 0xAA;
            cpu.State.Hl = 0x0055;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0, cpu.State.Af.High.Value);
            Assert.AreEqual(0xA0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xA7, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void AndImmediate()
        {
            cpu.Reset();
            cpu.State.Pc = 0xE6;
            cpu.State.Af.High.Value = 0x1;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x01, cpu.State.Af.High.Value);
            Assert.AreEqual(0x20, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xE8, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Exclusive Ors

        [TestMethod]
        public void ExclusiveOrRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xA8;
            cpu.State.Af.High.Value = 0xF0;
            cpu.State.Bc.High.Value = 0xF0;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0, cpu.State.Af.High.Value);
            Assert.AreEqual(0x80, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xA9, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void ExclusiveOrMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xAE;
            cpu.State.Af.High.Value = 0xF0;
            cpu.State.Hl = 0x000F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFF, cpu.State.Af.High.Value);
            Assert.AreEqual(0x0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xAF, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void ExclusiveOrImmediate()
        {
            cpu.Reset();
            cpu.State.Pc = 0xEE;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xEF, cpu.State.Af.High.Value);
            Assert.AreEqual(0x0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xF0, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Ors

        [TestMethod]
        public void OrRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xB0;
            cpu.State.Af.High.Value = 0x0;
            cpu.State.Bc.High.Value = 0x0;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0, cpu.State.Af.High.Value);
            Assert.AreEqual(0x80, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xB1, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void OrMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xB6;
            cpu.State.Af.High.Value = 0xAA;
            cpu.State.Hl = 0x0055;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFF, cpu.State.Af.High.Value);
            Assert.AreEqual(0x0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xB7, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void OrImmediate()
        {
            cpu.Reset();
            cpu.State.Pc = 0xF6;
            cpu.State.Af.High.Value = 0x8;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFF, cpu.State.Af.High.Value);
            Assert.AreEqual(0x0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xF8, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Compares

        [TestMethod]
        public void CompareRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xB8;
            cpu.State.Af.High.Value = 0x23;
            cpu.State.Bc.High.Value = 0x0F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x23, cpu.State.Af.High.Value);
            Assert.AreEqual(0x60, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xB9, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void CompareMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xBE;
            cpu.State.Af.High.Value = 0x01;
            cpu.State.Af.Low.Value = 0x40;
            cpu.State.Hl = 0x0002;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x01, cpu.State.Af.High.Value);
            Assert.AreEqual(0x70, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xBF, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void CompareImmediate()
        {
            cpu.Reset();
            cpu.State.Pc = 0xFE;
            cpu.State.Af.High.Value = 0xE1;
            cpu.State.Af.Low.Value = 0x10;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xE1, cpu.State.Af.High.Value);
            Assert.AreEqual(0x70, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x100, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Misc

        [TestMethod]
        public void DecimalAdjustA()
        {
            cpu.Reset();
            cpu.State.Pc = 0x27;
            cpu.State.Af.High.Value = 0b0000_1010;
            cpu.State.Af.Low.Value = 0b0010_0000;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b0001_0000, cpu.State.Af.High.Value);
            Assert.AreEqual(0x00, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x28, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }
        
        [TestMethod]
        public void ComplementA()
        {
            cpu.Reset();
            cpu.State.Pc = 0x2F;
            cpu.State.Af.High.Value = 0b0010_0110;
            cpu.State.Af.Low.Value = 0b0000_0000;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b1101_1001, cpu.State.Af.High.Value);
            Assert.AreEqual(0b0110_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x30, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }
        
        [TestMethod]
        public void ComplementCarryFlag()
        {
            cpu.Reset();
            cpu.State.Pc = 0x3F;
            cpu.State.Af.Low.Value = 0b0111_0000;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b0000_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x40, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void SetCarryFlag()
        {
            cpu.Reset();
            cpu.State.Pc = 0x37;
            cpu.State.Af.Low.Value = 0b0110_0000;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b0001_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0x38, cpu.State.Pc.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }
        
        #endregion
        
        #region RotateLeft

        [TestMethod]
        public void RotateLeftCarryRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x00);
            cpu.State.Bc.High.Value = 0xAA;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x55, cpu.State.Bc.High.Value);
            Assert.AreEqual(0x10, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateLeftCarryMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x06);
            cpu.State.Hl = 0x12AA;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x12AA] = 0x55, Times.Once());
            Assert.AreEqual(0x10, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateLeftRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x10);
            cpu.State.Bc.High.Value = 0xAA;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x54, cpu.State.Bc.High.Value);
            Assert.AreEqual(0x10, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateLeftMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x16);
            cpu.State.Hl = 0x12AA;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x12AA] = 0x54, Times.Once());
            Assert.AreEqual(0x10, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region RotateRight

        [TestMethod]
        public void RotateRightCarryRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x08);
            cpu.State.Bc.High.Value = 0x55;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xAA, cpu.State.Bc.High.Value);
            Assert.AreEqual(0x10, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateRightCarryMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x0E);
            cpu.State.Hl = 0x1255;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1255] = 0xAA, Times.Once());
            Assert.AreEqual(0x10, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateRightRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x18);
            cpu.State.Bc.High.Value = 0x55;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x2A, cpu.State.Bc.High.Value);
            Assert.AreEqual(0x10, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateRightMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x1E);
            cpu.State.Hl = 0x1255;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1255] = 0x2A, Times.Once());
            Assert.AreEqual(0x10, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region ShiftLeft
        
        [TestMethod]
        public void ShiftLeftRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x20);
            cpu.State.Bc.High.Value = 0b1000_0001;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b0000_0010, cpu.State.Bc.High.Value);
            Assert.AreEqual(0b0001_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void ShiftLeftMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x26);
            cpu.State.Hl = 0x1281;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1281] = 0b0000_0010, Times.Once());
            Assert.AreEqual(0b0001_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }
        
        #endregion
        
        #region ShiftRight
        
        [TestMethod]
        public void ShiftRightRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x38);
            cpu.State.Bc.High.Value = 0b1000_0001;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b0100_0000, cpu.State.Bc.High.Value);
            Assert.AreEqual(0b0001_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void ShiftRightMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x3E);
            cpu.State.Hl = 0x1281;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1281] = 0b0100_0000, Times.Once());
            Assert.AreEqual(0b0001_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }
        
        [TestMethod]
        public void ShiftRightKeepMsbRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x28);
            cpu.State.Bc.High.Value = 0b1000_0001;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b1100_0000, cpu.State.Bc.High.Value);
            Assert.AreEqual(0b0001_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void ShiftRightKeepMsbMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x2E);
            cpu.State.Hl = 0x1281;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1281] = 0b1100_0000, Times.Once());
            Assert.AreEqual(0b0001_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }
        
        #endregion
        
        #region Swap

        [TestMethod]
        public void TestSwapRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x30);
            cpu.State.Bc.High.Value = 0b1100_0011;
            cpu.State.Af.Low.Value = 0b0111_0000;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b0011_1100, cpu.State.Bc.High.Value);
            Assert.AreEqual(0b0000_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }
        
        [TestMethod]
        public void TestSwapMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x36);
            mmu.Setup(m => m[0x1255]).Returns(0b1100_0011);
            cpu.State.Af.Low = 0b0111_0000;
            cpu.State.Hl = 0x1255;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1255] = 0b0011_1100, Times.Once());
            Assert.AreEqual(0b0000_0000, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }
        
        #endregion
        
        #region Bit

        [TestMethod]
        public void TestBitRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x40);
            cpu.State.Bc.High.Value = 0x01;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x20, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void TestBitMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x46);
            cpu.State.Hl = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xA0, cpu.State.Af.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void ResetBitRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x80);
            cpu.State.Bc.High.Value = 0b1111_1111;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b1111_1110, cpu.State.Bc.High.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void ResetBitMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x86);
            cpu.State.Hl = 0x1234;
            mmu.Setup(m => m[0x1234]).Returns(0b1111_1111);

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1234] = 0b1111_1110, Times.Once());
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }
        
        [TestMethod]
        public void SetBitRegister()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0xC0);
            cpu.State.Bc.High.Value = 0b0000_0000;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0b0000_0001, cpu.State.Bc.High.Value);
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void SetBitMemory()
        {
            cpu.Reset();
            cpu.State.Pc = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0xC6);
            cpu.State.Hl = 0x1234;
            mmu.Setup(m => m[0x1234]).Returns(0b0000_0000);

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1234] = 0b0000_0001, Times.Once());
            Assert.AreEqual(0xCD, cpu.State.Pc.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        private readonly Mock<IMmu> mmu;
        private readonly Cpu cpu;
    }
}
