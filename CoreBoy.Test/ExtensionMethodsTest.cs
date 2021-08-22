using CoreBoy.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreBoy.Test
{
    [TestClass]
    public class ExtensionMethodsTest
    {
        [DataTestMethod]
        [DataRow(0, (byte)1)]
        [DataRow(1, (byte)2)]
        [DataRow(2, (byte)4)]
        [DataRow(3, (byte)8)]
        [DataRow(4, (byte)16)]
        [DataRow(5, (byte)32)]
        [DataRow(6, (byte)64)]
        [DataRow(7, (byte)128)]
        public void SetBit(int bitIndex, byte expectedValue)
        {
            byte test = 0;
            test = test.SetBit(bitIndex, true);
            Assert.AreEqual(expectedValue, test);
        }

        [DataTestMethod]
        [DataRow(0, (byte)1)]
        [DataRow(1, (byte)2)]
        [DataRow(2, (byte)4)]
        [DataRow(3, (byte)8)]
        [DataRow(4, (byte)16)]
        [DataRow(5, (byte)32)]
        [DataRow(6, (byte)64)]
        [DataRow(7, (byte)128)]
        public void ClearBit(int bitIndex, byte initialValue)
        {
            var test = initialValue.SetBit(bitIndex, false);
            Assert.AreEqual(0, test);
        }

        [DataTestMethod]
        [DataRow((byte)0xAA, 0, false)]
        [DataRow((byte)0xAA, 1, true)]
        [DataRow((byte)0xAA, 2, false)]
        [DataRow((byte)0xAA, 3, true)]
        [DataRow((byte)0xAA, 4, false)]
        [DataRow((byte)0xAA, 5, true)]
        [DataRow((byte)0xAA, 6, false)]
        [DataRow((byte)0xAA, 7, true)]
        public void GetBitTest(byte value, int bitIndex, bool expectedValue)
        {
            Assert.AreEqual(expectedValue, value.GetBit(bitIndex));
        }
    }
}
