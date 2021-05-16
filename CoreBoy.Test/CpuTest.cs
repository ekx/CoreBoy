using Moq;
using CoreBoy.Core.Processors;
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

        #region Loads

        // 8-Bit

        [TestMethod]
        public void Load8BitImmediateIntoRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x06;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x07, cpu.State.BC.High.Value);
            Assert.AreEqual(0x08, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitImmediateIntoMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x36;
            cpu.State.HL = 0xFFFF;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0xFFFF] = 0x37, Times.Once());
            Assert.AreEqual(0x38, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitRegisterIntoMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x02;
            cpu.State.AF.High.Value = 0x12;
            cpu.State.BC = 0xFFFF;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0xFFFF] = 0x12, Times.Once());
            Assert.AreEqual(0x03, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitMemoryIntoRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x0A;
            cpu.State.BC = 0x12;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x12, cpu.State.AF.High.Value);
            Assert.AreEqual(0x0B, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitRegisterIntoRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x41;
            cpu.State.BC = 0x00FF;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFFFF, cpu.State.BC.Value);
            Assert.AreEqual(0x42, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitRegisterIntoOffsetAddress()
        {
            cpu.Reset();
            cpu.State.PC = 0xE0;
            cpu.State.AF.High.Value = 0x12;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0xFFE1] = 0x12, Times.Once());
            Assert.AreEqual(0xE2, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitOffsetAddressIntoRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xF0;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xF1, cpu.State.AF.High.Value);
            Assert.AreEqual(0xF2, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitRegisterIntoImmediateAddress()
        {
            cpu.Reset();
            cpu.State.PC = 0xEA;
            cpu.State.AF.High.Value = 0x12;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0xECEB] = 0x12, Times.Once());
            Assert.AreEqual(0xED, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        [TestMethod]
        public void Load8BitImmediateAddressIntoRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xFA;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFB, cpu.State.AF.High.Value);
            Assert.AreEqual(0xFD, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        // 16-Bit

        [TestMethod]
        public void Load16BitImmediateIntoRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x01;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0302, cpu.State.BC.Value);
            Assert.AreEqual(0x04, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadSPIntoMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x08;
            cpu.State.SP = 0x1234;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x0A09] = 0x34, Times.Once());
            mmu.VerifySet(m => m[0x0A0A] = 0x12, Times.Once());
            Assert.AreEqual(0x0B, cpu.State.PC.Value);
            Assert.AreEqual(20, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadSPIntoHL()
        {
            cpu.Reset();
            cpu.State.PC = 0xF8;
            cpu.State.SP = 0x01FF;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x30, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x01F8, cpu.State.HL.Value);
            Assert.AreEqual(0xFA, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadHLIntoSP()
        {
            cpu.Reset();
            cpu.State.PC = 0xF9;
            cpu.State.HL = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x1234, cpu.State.SP.Value);
            Assert.AreEqual(0xFA, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        // Special

        [TestMethod]
        public void LoadIncrementRegisterIntoMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x22;
            cpu.State.HL = 0x1234;
            cpu.State.AF.High.Value = 0x56;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1234] = 0x56, Times.Once());
            Assert.AreEqual(0x1235, cpu.State.HL.Value);
            Assert.AreEqual(0x23, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadIncrementMemoryIntoRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x2A;
            cpu.State.HL = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x34, cpu.State.AF.High.Value);
            Assert.AreEqual(0x1235, cpu.State.HL.Value);
            Assert.AreEqual(0x2B, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadDecrementRegisterIntoMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x32;
            cpu.State.HL = 0x1234;
            cpu.State.AF.High.Value = 0x56;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1234] = 0x56, Times.Once());
            Assert.AreEqual(0x1233, cpu.State.HL.Value);
            Assert.AreEqual(0x33, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void LoadDecrementMemoryIntoRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x3A;
            cpu.State.HL = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x34, cpu.State.AF.High.Value);
            Assert.AreEqual(0x1233, cpu.State.HL.Value);
            Assert.AreEqual(0x3B, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Increments

        [TestMethod]
        public void Increment8BitRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x04;
            cpu.State.BC.High.Value = 0xFF;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xA0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x00, cpu.State.BC.High.Value);
            Assert.AreEqual(0x05, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Increment8BitMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x34;
            cpu.State.HL = 0x123F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x20, cpu.State.AF.Low.Value);
            mmu.VerifySet(m => m[0x123F] = 0x40, Times.Once());
            Assert.AreEqual(0x35, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Increment16BitRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x03;
            cpu.State.BC = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x1235, cpu.State.BC.Value);
            Assert.AreEqual(0x04, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Decrements

        [TestMethod]
        public void Decrement8BitRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x05;
            cpu.State.BC.High.Value = 0x01;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x80, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x00, cpu.State.BC.High.Value);
            Assert.AreEqual(0x06, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Decrement8BitMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x35;
            cpu.State.HL = 0x1230;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x20, cpu.State.AF.Low.Value);
            mmu.VerifySet(m => m[0x1230] = 0x2F, Times.Once());
            Assert.AreEqual(0x36, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Decrement16BitRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x0B;
            cpu.State.BC = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x1233, cpu.State.BC.Value);
            Assert.AreEqual(0x0C, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Jumps

        [TestMethod]
        public void JumpImmediate()
        {
            cpu.Reset();
            cpu.State.PC = 0xC3;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xC5C4, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        [TestMethod]
        public void JumpRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xE9;
            cpu.State.HL = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x1234, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void JumpRelativeImmediate()
        {
            cpu.Reset();
            cpu.State.PC = 0x18;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x33, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        #endregion
        
        #region Pop Push

        [TestMethod]
        public void Pop()
        {
            cpu.Reset();
            cpu.State.PC = 0xC1;
            cpu.State.SP = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x3534, cpu.State.BC.Value);
            Assert.AreEqual(0x1236, cpu.State.SP.Value);
            Assert.AreEqual(0xC2, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        [TestMethod]
        public void Push()
        {
            cpu.Reset();
            cpu.State.PC = 0xC5;
            cpu.State.SP = 0x1234;
            cpu.State.BC = 0xF1F2;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1233] = 0xF1, Times.Once());
            mmu.VerifySet(m => m[0x1232] = 0xF2, Times.Once());
            Assert.AreEqual(0x1232, cpu.State.SP.Value);
            Assert.AreEqual(0xC6, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region Call Return

        [TestMethod]
        public void Call()
        {
            cpu.Reset();
            cpu.State.PC = 0xCD;
            cpu.State.SP = 0x1234;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1233] = 0x00, Times.Once());
            mmu.VerifySet(m => m[0x1232] = 0xD0, Times.Once());
            Assert.AreEqual(0x1232, cpu.State.SP.Value);
            Assert.AreEqual(0xCFCE, cpu.State.PC.Value);
            Assert.AreEqual(24, cpu.State.Clock);
        }

        [TestMethod]
        public void Return()
        {
            cpu.Reset();
            cpu.State.PC = 0xC9;
            cpu.State.SP = 0x12;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x14, cpu.State.SP.Value);
            Assert.AreEqual(0x1312, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region Adds

        [TestMethod]
        public void Add8BitFromRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x80;
            cpu.State.AF.High.Value = 0x05;
            cpu.State.BC.High.Value = 0x0F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x14, cpu.State.AF.High.Value);
            Assert.AreEqual(0x20, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x81, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Add8BitFromMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x86;
            cpu.State.AF.High.Value = 0x01;
            cpu.State.AF.Low.Value = 0x40;
            cpu.State.HL = 0x00FF;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x00, cpu.State.AF.High.Value);
            Assert.AreEqual(0xB0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x87, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Add8BitFromImmediate()
        {
            cpu.Reset();
            cpu.State.PC = 0xCE;
            cpu.State.AF.High.Value = 0x05;
            cpu.State.AF.Low.Value = 0x10;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xD5, cpu.State.AF.High.Value);
            Assert.AreEqual(0x20, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xD0, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Add16BitFromRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x09;
            cpu.State.HL = 0x0204;
            cpu.State.BC = 0x1030;
            cpu.State.AF.Low.Value = 0x80;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x80, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x1234, cpu.State.HL.Value);
            Assert.AreEqual(0x0A, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Add16BitFromImmediate ()
        {
            cpu.Reset();
            cpu.State.PC = 0xE8;
            cpu.State.SP = 0x1200;
            cpu.State.AF.Low.Value = 0x80;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x12E9, cpu.State.SP.Value);
            Assert.AreEqual(0xEA, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region Subtracts

        [TestMethod]
        public void Subtract8BitFromRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0x90;
            cpu.State.AF.High.Value = 0x23;
            cpu.State.BC.High.Value = 0x0F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x14, cpu.State.AF.High.Value);
            Assert.AreEqual(0x60, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x91, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void Subtract8BitFromMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0x96;
            cpu.State.AF.High.Value = 0x01;
            cpu.State.AF.Low.Value = 0x40;
            cpu.State.HL = 0x0002;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFF, cpu.State.AF.High.Value);
            Assert.AreEqual(0x70, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x97, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void Subtract8BitFromImmediate()
        {
            cpu.Reset();
            cpu.State.PC = 0xDE;
            cpu.State.AF.High.Value = 0xE1;
            cpu.State.AF.Low.Value = 0x10;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x01, cpu.State.AF.High.Value);
            Assert.AreEqual(0x60, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xE0, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Ands

        [TestMethod]
        public void AndRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xA0;
            cpu.State.AF.High.Value = 0xF0;
            cpu.State.BC.High.Value = 0xF0;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xF0, cpu.State.AF.High.Value);
            Assert.AreEqual(0x20, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xA1, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void AndMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xA6;
            cpu.State.AF.High.Value = 0xAA;
            cpu.State.HL = 0x0055;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0, cpu.State.AF.High.Value);
            Assert.AreEqual(0xA0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xA7, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void AndImmediate()
        {
            cpu.Reset();
            cpu.State.PC = 0xE6;
            cpu.State.AF.High.Value = 0x1;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x01, cpu.State.AF.High.Value);
            Assert.AreEqual(0x20, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xE8, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Exclusive Ors

        [TestMethod]
        public void ExclusiveOrRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xA8;
            cpu.State.AF.High.Value = 0xF0;
            cpu.State.BC.High.Value = 0xF0;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0, cpu.State.AF.High.Value);
            Assert.AreEqual(0x80, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xA9, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void ExclusiveOrMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xAE;
            cpu.State.AF.High.Value = 0xF0;
            cpu.State.HL = 0x000F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFF, cpu.State.AF.High.Value);
            Assert.AreEqual(0x0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xAF, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void ExclusiveOrImmediate()
        {
            cpu.Reset();
            cpu.State.PC = 0xEE;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xEF, cpu.State.AF.High.Value);
            Assert.AreEqual(0x0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xF0, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Ors

        [TestMethod]
        public void OrRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xB0;
            cpu.State.AF.High.Value = 0x0;
            cpu.State.BC.High.Value = 0x0;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x0, cpu.State.AF.High.Value);
            Assert.AreEqual(0x80, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xB1, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void OrMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xB6;
            cpu.State.AF.High.Value = 0xAA;
            cpu.State.HL = 0x0055;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFF, cpu.State.AF.High.Value);
            Assert.AreEqual(0x0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xB7, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void OrImmediate()
        {
            cpu.Reset();
            cpu.State.PC = 0xF6;
            cpu.State.AF.High.Value = 0x8;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xFF, cpu.State.AF.High.Value);
            Assert.AreEqual(0x0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xF8, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region Compares

        [TestMethod]
        public void CompareRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xB8;
            cpu.State.AF.High.Value = 0x23;
            cpu.State.BC.High.Value = 0x0F;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x23, cpu.State.AF.High.Value);
            Assert.AreEqual(0x60, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xB9, cpu.State.PC.Value);
            Assert.AreEqual(4, cpu.State.Clock);
        }

        [TestMethod]
        public void CompareMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xBE;
            cpu.State.AF.High.Value = 0x01;
            cpu.State.AF.Low.Value = 0x40;
            cpu.State.HL = 0x0002;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x01, cpu.State.AF.High.Value);
            Assert.AreEqual(0x70, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xBF, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void CompareImmediate()
        {
            cpu.Reset();
            cpu.State.PC = 0xFE;
            cpu.State.AF.High.Value = 0xE1;
            cpu.State.AF.Low.Value = 0x10;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xE1, cpu.State.AF.High.Value);
            Assert.AreEqual(0x70, cpu.State.AF.Low.Value);
            Assert.AreEqual(0x100, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        #endregion

        #region RotateLeft

        [TestMethod]
        public void RotateLeftCarryRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x00);
            cpu.State.BC.High.Value = 0xAA;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x55, cpu.State.BC.High.Value);
            Assert.AreEqual(0x10, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateLeftCarryMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x06);
            cpu.State.HL = 0x12AA;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x12AA] = 0x55, Times.Once());
            Assert.AreEqual(0x10, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateLeftRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x10);
            cpu.State.BC.High.Value = 0xAA;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x54, cpu.State.BC.High.Value);
            Assert.AreEqual(0x10, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateLeftMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x16);
            cpu.State.HL = 0x12AA;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x12AA] = 0x54, Times.Once());
            Assert.AreEqual(0x10, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region RotateRight

        [TestMethod]
        public void RotateRightCarryRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x08);
            cpu.State.BC.High.Value = 0x55;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xAA, cpu.State.BC.High.Value);
            Assert.AreEqual(0x10, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateRightCarryMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x0E);
            cpu.State.HL = 0x1255;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1255] = 0xAA, Times.Once());
            Assert.AreEqual(0x10, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateRightRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x18);
            cpu.State.BC.High.Value = 0x55;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x2A, cpu.State.BC.High.Value);
            Assert.AreEqual(0x10, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void RotateRightMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x1E);
            cpu.State.HL = 0x1255;

            cpu.RunInstructionCycle();

            mmu.VerifySet(m => m[0x1255] = 0x2A, Times.Once());
            Assert.AreEqual(0x10, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(16, cpu.State.Clock);
        }

        #endregion

        #region Bit

        [TestMethod]
        public void TestBitRegister()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x40);
            cpu.State.BC.High.Value = 0x01;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0x20, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(8, cpu.State.Clock);
        }

        [TestMethod]
        public void TestBitMemory()
        {
            cpu.Reset();
            cpu.State.PC = 0xCB;
            mmu.Setup(m => m[0x00CC]).Returns(0x46);
            cpu.State.HL = 0x1234;

            cpu.RunInstructionCycle();

            Assert.AreEqual(0xA0, cpu.State.AF.Low.Value);
            Assert.AreEqual(0xCD, cpu.State.PC.Value);
            Assert.AreEqual(12, cpu.State.Clock);
        }

        #endregion

        private Mock<IMmu> mmu;
        private Cpu cpu;
    }
}
