using System;
using CoreBoy.Core.Utils;

namespace CoreBoy.Silk
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using var emulator = new Emulator(Graphics.ScreenWidth * 2, Graphics.ScreenHeight * 2, "CoreBoy");
            emulator.Run();
        }
    }
}