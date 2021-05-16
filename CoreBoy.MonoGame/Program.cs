using System;

namespace CoreBoy.MonoGame
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            using (var game = new Emulator(320, 288))
            {
                game.Run();
            }
        }
    }
}
