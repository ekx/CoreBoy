using System.Text;
using CoreBoy.Core.Utils;

namespace CoreBoy.Core.Cartridges.Utils;

public class CartridgeHeader
{
    public string Title { get; init; }                          // Title-- Max 16 chars trailed by zeroes.
    public bool CgbFlag { get; init; }                          // Set if game is a Game Boy Color game.
    public string NewLicenseeCode { get; init; }                // 2 char ASCII code only found in games released after the Super Game Boy
    public bool SgbFlag { get; init; }                          // Set if game utilizes Super Game Boy features-- Features won't work if not set
    public CartridgeType CartType { get; init; }                // Specifies which (if any) Memory Bank Controller is used by the cartridge
    public RomSize RomSize { get; init; }                       // Specifies size and amount of ROM banks
    public RamSize RamSize { get; init; }                       // Specifies size and amount of RAM banks
    public bool NonJapaneseRom { get; init; }                   // If set Cartridge wasn't sold in Japan
    public byte OldLicenseeCode { get; init; }                  // Set to 0x33 if new licensee code is used-- 0x79 = Accolade, 0xA4 == Konami
    public byte MaskRomVersion { get; init; }                   // Version of Game-- Usually 0x00
    public byte HeaderChecksum { get; init; }                   // Cartridges with faulty Header Checksum won't boot on original hardware
    public byte CalculatedHeaderChecksum { get; init; }         // Checksum calculated from actual header data
    public int GlobalChecksum { get; init; }                    // Produced by adding all bytes of the cartridge, except for the two checksum bytes-- Ignored by Game Boy

    public bool NoVerticalBlankInterruptHandler { get; init; }
    public bool NoLcdcStatusInterruptHandler { get; init; }
    public bool NoTimerOverflowInterruptHandler { get; init; }
    public bool NoSerialTransferCompletionInterruptHandler { get; init; }
    public bool NoHighToLowOfP10ToP13InterruptHandler { get; init; }

    public CartridgeHeader(byte[] data)
    {
        Title = ExtractTitle(data);
        CgbFlag = data[0x0143] == 0x80;
        NewLicenseeCode = ExtractLicenseeCode(data);
        SgbFlag = data[0x0146] == 0x03;
        CartType = (CartridgeType)data[0x0147];

        RomSize = data[0x0148] switch
        {
            0x00 => new RomSize(2),
            0x01 => new RomSize(4),
            0x02 => new RomSize(8),
            0x03 => new RomSize(16),
            0x04 => new RomSize(32),
            0x05 => new RomSize(64),
            0x06 => new RomSize(128),
            0x07 => new RomSize(256),
            0x52 => new RomSize(72),
            0x53 => new RomSize(80),
            0x54 => new RomSize(96),
            _ => RomSize
        };

        RamSize = data[0x0149] switch
        {
            0x00 => new RamSize(0, 0),
            0x01 => new RamSize(2 * 1024, 1),
            0x02 => new RamSize(8 * 1024, 1),
            0x03 => new RamSize(8 * 1024, 4),
            _ => RamSize
        };

        NonJapaneseRom = data[0x014A] == 0x01;
        OldLicenseeCode = data[0x014B];
        MaskRomVersion = data[0x014C];

        HeaderChecksum = data[0x014D];

        var temp = 0;
        for (var i = 0x0134; i <= 0x014C; i++)
        {
            temp = temp - data[i] - 1;
        }
        temp &= 0xFF;
        CalculatedHeaderChecksum = (byte) temp;

        GlobalChecksum = (data[0x014E] << 8) | data[0x014F];

        NoVerticalBlankInterruptHandler = data[0x0040] == 0xD9;
        NoLcdcStatusInterruptHandler = data[0x0048] == 0xD9;
        NoTimerOverflowInterruptHandler = data[0x0050] == 0xD9;
        NoSerialTransferCompletionInterruptHandler = data[0x0058] == 0xD9;
        NoHighToLowOfP10ToP13InterruptHandler = data[0x0060] == 0xD9;
    }

    private static string ExtractTitle(byte[] data)
    {
        var sb = new StringBuilder();

        for (var i = 0x0134; i <= 0x0142; i++)
        {
            if (data[i] == 0x00)
                break;
            sb.Append((char)data[i]);
        }

        return sb.ToString();
    }

    private static string ExtractLicenseeCode(byte[] data)
    {
        var sb = new StringBuilder();

        for (var i = 0x0144; i <= 0x0145; i++)
        {
            sb.Append((char)data[i]);
        }

        return sb.ToString();
    }
}