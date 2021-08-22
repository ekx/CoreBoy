using System;
using CoreBoy.Core;

namespace CoreBoy.Silk
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using var emulator = new Emulator((int) (GameBoy.ScreenWidth * 2), (int) (GameBoy.ScreenHeight * 2), "CoreBoy");
            emulator.Run();
        }
    }
}