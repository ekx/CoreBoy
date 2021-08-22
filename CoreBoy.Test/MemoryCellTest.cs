using CoreBoy.Core.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreBoy.Test
{
    [TestClass]
    public class MemoryCellTest
    {
        [TestMethod]
        public void LockedBits()
        {
            var memoryCell = new MemoryCell
            {
                Value = 0xFF
            };

            Assert.AreEqual(0xFF, memoryCell.Value);

            memoryCell.LockBit(2, true);
            memoryCell.Value = 0x00;

            Assert.AreEqual(0x04, memoryCell.Value);
        }
    }
}
