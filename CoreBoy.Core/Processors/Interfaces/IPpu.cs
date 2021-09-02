using CoreBoy.Core.Utils;

namespace CoreBoy.Core.Processors.Interfaces
{
    public interface IPpu
    {
        event RenderFramebufferDelegate RenderFramebufferHandler;
        event VBlankInterruptDelegate VBlankInterruptHandler;
        event LcdStatusInterruptDelegate LcdStatusInterruptHandler;

        PpuState State { get; set; }

        void Reset();
        void UpdateState(long cycles);
        byte this[ushort address] { get; set; }
    }
}
