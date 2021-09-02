using CoreBoy.Core.Utils;
using CoreBoy.Core.Utils.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreBoy.Test
{
    [TestClass]
    public class RegisterTest
    {
        [TestMethod]
        public void ShortAssignment()
        {
            RegisterWord register = 0x1234;

            Assert.AreEqual(0x12, register.High.Value);
            Assert.AreEqual(0x34, register.Low.Value);
        }

        [TestMethod]
        public void ByteAssignment()
        {
            RegisterWord register = 0x0000;
            register.High = 0x12;
            register.Low = 0x34;

            Assert.AreEqual(0x1234, register.Value);
        }
    }
}
