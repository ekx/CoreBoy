using System;

namespace CoreBoy.OpenTK
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            using (var emulator = new Emulator(320, 288, "CoreBoy"))
            {
                emulator.Run();
            }
        }
    }
}