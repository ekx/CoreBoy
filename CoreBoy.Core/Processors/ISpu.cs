using System;
using System.Collections.Generic;
using System.Text;

namespace CoreBoy.Core.Processors
{
    public interface ISpu
    {
        SpuState State { get; set; }

        void Reset();
        void UpdateState(long cycles);
        byte this[ushort address] { get; set; }
    }
}
