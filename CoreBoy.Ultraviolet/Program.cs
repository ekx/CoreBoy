using System;

namespace CoreBoy.Ultraviolet
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var emulator = new Emulator())
            {
                emulator.Run();
            }
        }
    }
}
