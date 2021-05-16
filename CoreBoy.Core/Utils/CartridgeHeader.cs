using System.Text;
using Microsoft.Extensions.Logging;

namespace CoreBoy.Core.Cartridges
{
    public class CartridgeHeader
    {
        public string Title;                    // Title-- Max 16 chars trailed by zeroes.
        public bool CGBFlag;                    // Set if game is a Game Boy Color game.
        public string NewLicenseeCode;          // 2 char ASCII code only found in games released after the Super Game Boy
        public bool SGBFlag;                    // Set if game utilizes Super Game Boy features-- Features won't work if not set
        public CartridgeType CartType;          // Specifies which (if any) Memory Bank Controller is used by the cartridge
        public RomSize RomSize;                 // Specifies size and amount of ROM banks
        public RamSize RamSize;                 // Specifies size and amount of RAM banks
        public bool NonJapaneseRom;             // If set Cartridge wasn't sold in Japan
        public byte OldLicenseeCode;            // Set to 0x33 if new licensee code is used-- 0x79 = Accolade, 0xA4 == Konami
        public byte MaskROMVersion;             // Version of Game-- Usually 0x00
        public byte HeaderChecksum;             // Cartridges with faulty Header Checksum won't boot on original hardware
        public int GlobalChecksum;              // Produced by adding all bytes of the cartridge, except for the two checksum bytes-- Ignored by Game Boy

        public bool NoVerticalBlankInterruptHandler;
        public bool NoLCDCStatusInterruptHandler;
        public bool NoTimerOverflowInterruptHandler;
        public bool NoSerialTransferCompletionInterruptHandler;
        public bool NoHighToLowOfP10ToP13InterruptHandler;

        public CartridgeHeader(ILogger log, byte[] data)
        {
            Title = ExtractTitle(data);
            CGBFlag = data[0x0143] == 0x80;
            NewLicenseeCode = ExtractLicenseeCode(data);
            SGBFlag = data[0x0146] == 0x03;
            CartType = (CartridgeType)data[0x0147];

            switch (data[0x0148])
            {
                case 0x00:
                    RomSize = new RomSize(2);
                    break;
                case 0x01:
                    RomSize = new RomSize(4);
                    break;
                case 0x02:
                    RomSize = new RomSize(8);
                    break;
                case 0x03:
                    RomSize = new RomSize(16);
                    break;
                case 0x04:
                    RomSize = new RomSize(32);
                    break;
                case 0x05:
                    RomSize = new RomSize(64);
                    break;
                case 0x06:
                    RomSize = new RomSize(128);
                    break;
                case 0x07:
                    RomSize = new RomSize(256);
                    break;

                case 0x52:
                    RomSize = new RomSize(72);
                    break;
                case 0x53:
                    RomSize = new RomSize(80);
                    break;
                case 0x54:
                    RomSize = new RomSize(96);
                    break;
            }

            switch (data[0x0149])
            {
                case 0x00:
                    RamSize = new RamSize(0, 0);
                    break;
                case 0x01:
                    RamSize = new RamSize(2 * 1024, 1);
                    break;
                case 0x02:
                    RamSize = new RamSize(8 * 1024, 1);
                    break;
                case 0x03:
                    RamSize = new RamSize(8 * 1024, 4);
                    break;
            }

            NonJapaneseRom = data[0x014A] == 0x01;
            OldLicenseeCode = data[0x014B];
            MaskROMVersion = data[0x014C];

            HeaderChecksum = data[0x014D];

            int temp = 0;
            for (int i = 0x0134; i <= 0x014C; i++)
            {
                temp = temp - data[i] - 1;
            }
            temp &= 0xFF;

            if (temp != HeaderChecksum)
            {
                log.LogWarning($"HeaderChecksum invalid. Cartridge: {Title}");
            }

            GlobalChecksum = (((int)data[0x014E]) << 8) | data[0x014F];

            NoVerticalBlankInterruptHandler = data[0x0040] == 0xD9;
            NoLCDCStatusInterruptHandler = data[0x0048] == 0xD9;
            NoTimerOverflowInterruptHandler = data[0x0050] == 0xD9;
            NoSerialTransferCompletionInterruptHandler = data[0x0058] == 0xD9;
            NoHighToLowOfP10ToP13InterruptHandler = data[0x0060] == 0xD9;
        }

        private string ExtractTitle(byte[] data)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0x0134; i <= 0x0142; i++)
            {
                if (data[i] == 0x00)
                    break;
                sb.Append((char)data[i]);
            }

            return sb.ToString();
        }

        private string ExtractLicenseeCode(byte[] data)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0x0144; i <= 0x0145; i++)
            {
                sb.Append((char)data[i]);
            }

            return sb.ToString();
        }
    }

    public struct RomSize
    {
        public int BankSize { get; private set; }
        public int BankCount { get; private set; }
        public int Total { get { return BankSize * BankCount; } }

        public RomSize(int bankCount)
        {
            this.BankSize = 16 * 1024;
            this.BankCount = bankCount;
        }
    }
    
    public struct RamSize
    {
        public int BankSize { get; private set; }
        public int BankCount { get; private set; }
        public int Total { get { return BankSize * BankCount; } }

        public RamSize(int bankSize, int bankCount)
        {
            this.BankSize = bankSize;
            this.BankCount = bankCount;
        }
    }
}
