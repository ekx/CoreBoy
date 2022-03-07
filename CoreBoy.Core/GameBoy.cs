using System.IO;
using CoreBoy.Core.Cartridges;
using CoreBoy.Core.Processors;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using CoreBoy.Core.Cartridges.Interfaces;
using CoreBoy.Core.Processors.Interfaces;
using CoreBoy.Core.Utils;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core;

public class GameBoy
{
    public event RenderFramebufferDelegate RenderFramebufferHandler
    {
        add => ppu.RenderFramebufferHandler += value;
        remove => ppu.RenderFramebufferHandler -= value;
    }

    public GameBoy(ILoggerFactory loggerFactory)
    {
        log = loggerFactory.CreateLogger<GameBoy>();
        this.loggerFactory = loggerFactory;

        ppu = new Ppu(loggerFactory.CreateLogger<Ppu>());
        spu = new Spu(loggerFactory.CreateLogger<Spu>());
        mmu = new Mmu(loggerFactory.CreateLogger<Mmu>(), ppu, spu);
        cpu = new Cpu(loggerFactory.CreateLogger<Cpu>(), mmu);
    }

    public void Reset()
    {
        shouldStop = false;

        cpu.Reset();
        ppu.Reset();
        mmu.Reset();
    }

    public void Power()
    {
        Reset();

        while (!shouldStop)
        {
            try
            {
                cpu.RunInstructionCycle();
            }
            catch (MissingOpcodeException e)
            {
                log.LogError(e.Message);
                shouldStop = true;
            }
        }
    }

    public void PowerOff()
    {
        shouldStop = true;
    }

    public void LoadBootRom(string path)
    {
        mmu.LoadBootRom(File.ReadAllBytes(path));
    }

    public void LoadCartridge(string path)
    {
        var data = File.ReadAllBytes(path);

        ICartridge cartridge = null;
            
        switch ((CartridgeType)data[0x0147])
        {
            case CartridgeType.ROM:
                cartridge = new RomCartridge(loggerFactory.CreateLogger<RomCartridge>(), data);
                break;
            /*case RomType.ROM_MBC1:
            case RomType.ROM_MBC1_RAM:
            case RomType.ROM_MBC1_RAM_BATT:
                cartridge = new MBC1(fileData, romType, romSize, romBanks);
                break;
            case RomType.ROM_MBC2:
            case RomType.ROM_MBC2_BATTERY:
                cartridge = new MBC2(fileData, romType, romSize, romBanks);
                break;
            case RomType.ROM_MBC3:
            case RomType.ROM_MBC3_RAM:
            case RomType.ROM_MBC3_RAM_BATT:
                cartridge = new MBC3(fileData, romType, romSize, romBanks);
                break*/
            default:
                log.LogError($"Cartridge type not emulated: {(CartridgeType)data[0x0147]}");
                break;
        }

        mmu.LoadCartridge(cartridge);
    }

    public void SetInput(InputState inputState)
    {
        mmu.SetInput(inputState);
    }
        
    public void SaveState()
    {
        using var stream = new FileStream("SaveState.xml", FileMode.Create);
            
        var serializer = new DataContractSerializer(typeof(SaveState), new List<Type> { typeof(RomCartridge) });
        serializer.WriteObject(stream, new SaveState()
        {
            CpuState = cpu.State,
            MmuState = mmu.State,
            PpuState = ppu.State,
            SpuState = spu.State,
            CartridgeState = mmu.CartridgeState
        });
    }

    public void LoadState()
    {
        using var stream = new FileStream("SaveState.xml", FileMode.Open);
            
        var serializer = new DataContractSerializer(typeof(SaveState), new List<Type> { typeof(RomCartridge) });
        var saveState = (SaveState)serializer.ReadObject(stream);

        if (saveState == null) return;
            
        cpu.State = saveState.CpuState;
        mmu.State = saveState.MmuState;
        ppu.State = saveState.PpuState;
        spu.State = saveState.SpuState;
        mmu.CartridgeState = saveState.CartridgeState;
    }

    private bool shouldStop;

    private readonly ICpu cpu;
    private readonly IPpu ppu;
    private readonly ISpu spu;
    private readonly IMmu mmu;

    private readonly ILogger log;
    private readonly ILoggerFactory loggerFactory;
}