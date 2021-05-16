using CoreBoy.Core.Utils;

namespace CoreBoy.Core.Processors
{
    public interface IPpu
    {
        event RenderFramebufferDelegate RenderFramebufferHandler;

        PpuState State { get; set; }

        void Reset();
        void UpdateState(long cycles);
        byte this[ushort address] { get; set; }
    }
}
