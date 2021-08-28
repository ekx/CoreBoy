namespace CoreBoy.Core.Utils
{
    public delegate void RenderFramebufferDelegate(byte[] framebuffer);

    public delegate void VBlankInterruptDelegate();

    public delegate void LcdStatusInterruptDelegate();

    public delegate void TimerInterruptDelegate();

    public delegate void SerialTransferInterruptDelegate();

    public delegate void InputInterruptDelegate();

    public delegate void InterruptTriggeredDelegate(InterruptType interruptType);
}
