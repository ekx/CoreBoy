using System.IO;
using CoreBoy.Core.Cartridges;
using CoreBoy.Core.Processors;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using CoreBoy.Core.Utils;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core
{
    public class GameBoy
    {
        public event RenderFramebufferDelegate RenderFramebufferHandler
        {
            add { ppu.RenderFramebufferHandler += value; }
            remove { ppu.RenderFramebufferHandler -= value; }
        }

        public GameBoy(ILoggerFactory loggerFactory)
        {
            this.log = loggerFactory.CreateLogger<GameBoy>();
            this.loggerFactory = loggerFactory;

            this.ppu = new Ppu(loggerFactory.CreateLogger<Ppu>());
            this.spu = new Spu(loggerFactory.CreateLogger<Spu>());
            this.mmu = new Mmu(loggerFactory.CreateLogger<Mmu>(), ppu, spu);
            this.cpu = new Cpu(loggerFactory.CreateLogger<Cpu>(), mmu);
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
            byte[] data = File.ReadAllBytes(path);

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

        public void SaveState()
        {
            using (var stream = new FileStream("SaveState.xml", FileMode.Create))
            {
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
        }

        public void LoadState()
        {
            using (var stream = new FileStream("SaveState.xml", FileMode.Open))
            {
                var serializer = new DataContractSerializer(typeof(SaveState), new List<Type> { typeof(RomCartridge) });
                var saveState = (SaveState)serializer.ReadObject(stream);

                cpu.State = saveState.CpuState;
                mmu.State = saveState.MmuState;
                ppu.State = saveState.PpuState;
                spu.State = saveState.SpuState;
                mmu.CartridgeState = saveState.CartridgeState;
            }
        }

        private bool shouldStop = false;

        private ICpu cpu;
        private IPpu ppu;
        private ISpu spu;
        private IMmu mmu;

        private readonly ILogger log;
        private readonly ILoggerFactory loggerFactory;
    }
}
