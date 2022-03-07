using CoreBoy.Core.Utils;
using CoreBoy.Silk;

using var emulator = new Emulator(Graphics.ScreenWidth * 2, Graphics.ScreenHeight * 2, "CoreBoy");
emulator.Run();