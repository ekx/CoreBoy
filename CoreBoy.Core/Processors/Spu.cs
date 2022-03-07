using Microsoft.Extensions.Logging;
using System;
using CoreBoy.Core.Processors.Interfaces;
using CoreBoy.Core.Processors.State;

namespace CoreBoy.Core.Processors;

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

        State = new SpuState();
    }

    public byte this[ushort address]
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public void UpdateState(long cycles)
    {
            
    }

    private readonly ILogger log;
}