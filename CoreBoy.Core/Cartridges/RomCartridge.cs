using Microsoft.Extensions.Logging;
using System.Runtime.Serialization;

namespace CoreBoy.Core.Cartridges
{
    [DataContract]
    public class RomCartridge : ICartridge
    {
        public CartridgeHeader CartInfo { get; set; }

        public ICartridgeState State
        {
            get { return state; }
            set { state = (RomCartridgeState)value; }
        }

        public RomCartridge(ILogger<RomCartridge> log, byte[] data)
        {
            this.log = log;

            CartInfo = new CartridgeHeader(log, data);
            this.rom = data;
        }

        public byte this[ushort address]
        {
            get
            {
                // [0000-7FFF] Cartridge ROM
                if (address < 0x8000)
                {
                    //log.LogDebug($"Read from Cartridge ROM. Address: {address:X4}, Value: {rom[address]:X2}");
                    return rom[address];
                }
                // [A000-BFFF] Cartridge RAM
                else if (address >= 0xA000 && address < 0xC000)
                {
                    log.LogWarning($"Read from nonexistent cartridge RAM. Address: {address:X4}");
                    return 0x00;
                }
                else
                {
                    log.LogError($"Read from non cartridge memory space. Address: {address:X4}");
                    return 0x00;
                }
            }

            set
            {
                // [0000-7FFF] Cartridge ROM
                if (address < 0x8000)
                {
                    log.LogWarning($"Write to read-only cartridge ROM. Address: {address:X4}, Value: {value:X2}");
                }
                // [A000-BFFF] Cartridge RAM
                else if (address >= 0xA000 && address < 0xC000)
                {
                    log.LogWarning($"Write to nonexistent cartridge RAM. Address: {address:X4}, Value: {value:X2}");
                }
                else
                {
                    log.LogError($"Write to non cartridge memory space. Address: {address:X4}, Value: {value:X2}");
                }
            }
        }

        private byte[] rom = new byte[32767];
        private RomCartridgeState state;

        private readonly ILogger log;
    }

    [DataContract]
    public class RomCartridgeState : ICartridgeState { }
}
