namespace CoreBoy.Core.Utils
{
    public enum CartridgeType : byte
    {
        // ReSharper disable InconsistentNaming
        ROM = 0x00,
        ROM_MBC1 = 0x01,
        ROM_MBC1_RAM = 0x02,
        ROM_MBC1_RAM_BATT = 0x03,
        ROM_MBC2 = 0x05,
        ROM_MBC2_BATTERY = 0x06,
        ROM_RAM = 0x08,
        ROM_RAM_BATTERY = 0x09,
        ROM_MMM01 = 0x0B,
        ROM_MMM01_SRAM = 0x0C,
        ROM_MMM01_SRAM_BATT = 0x0D,
        ROM_MBC3_TIMER_BATT = 0x0F,
        ROM_MBC3_TIMER_RAM_BATT = 0x10,
        ROM_MBC3 = 0x11,
        ROM_MBC3_RAM = 0x12,
        ROM_MBC3_RAM_BATT = 0x13,
        ROM_MBC5 = 0x19,
        ROM_MBC5_RAM = 0x1A,
        ROM_MBC5_RAM_BATT = 0x1B,
        ROM_MBC5_RUMBLE = 0x1C,
        ROM_MBC5_RUMBLE_SRAM = 0x1D,
        ROM_MBC5_RUMBLE_SRAM_BATT = 0x1E,
        PocketCamera = 0x1F,
        BandaiTAMA5 = 0xFD,
        HudsonHuC3 = 0xFE,
        HudsonHuC1 = 0xFF,
        // ReSharper restore InconsistentNaming
    }

    public static class GraphicsIo
    {
        // ReSharper disable InconsistentNaming
        public const int LCDC = 0;
        public const int STAT = 1;
        public const int SCY = 2;
        public const int SCX = 3;
        public const int LY = 4;
        public const int LYC = 5;
        public const int DMA = 6;
        public const int BGP = 7;
        public const int OBP0 = 8;
        public const int OBP1 = 9;
        public const int WY = 10;
        public const int WX = 11;
        // ReSharper restore InconsistentNaming

    }

    public static class LcdControl
    {
        public const int BgEnabled = 0;
        public const int SpritesEnabled = 1;
        public const int SpriteSize = 2;
        public const int BgTileMap = 3;
        public const int TileSet = 4;
        public const int WindowEnabled = 5;
        public const int WindowTileMap = 6;
        public const int LcdPower = 7;
    }

    public static class LcdStatus
    {
        public const int ScreenModeHigh = 0;
        public const int ScreenModeLow = 1;
        public const int LycSignal = 2;
        public const int HBlankCheckEnabled = 3;
        public const int VBlankCheckEnabled = 4;
        public const int OamCheckEnabled = 5;
        public const int LycCheckEnabled = 6;
        public const int Unused = 7;
    }

    public enum ScreenMode
    {
        HBlank = 0,
        VBlank = 1,
        AccessingOam = 2,
        TransferringData = 3
    }

    public static class Graphics
    {
        public const byte ScreenWidth = 160;
        public const byte ScreenHeight = 144;
    }
}