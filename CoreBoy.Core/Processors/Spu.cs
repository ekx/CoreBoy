using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CoreBoy.Core.Processors
{
    public class Spu : ISpu
    {
        public SpuState State { get; set; }

        public Spu(ILogger<Spu> log)
        {
            this.log = log;
        }

        public void Reset()
        {
            log.LogInformation("SPU reset");

            this.State = new SpuState();
        }

        public byte this[ushort address]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public void UpdateState(long cycles)
        {
            throw new NotImplementedException();
        }

        private readonly ILogger log;
    }

    [DataContract]
    public class SpuState
    {
        public SpuState()
        {
        }


    }
}
