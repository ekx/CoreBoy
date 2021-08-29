namespace CoreBoy.Core.Utils
{
    public record InputState(
        bool UpPressed,
        bool LeftPressed,
        bool DownPressed,
        bool RightPressed,
        bool APressed,
        bool BPressed,
        bool StartPressed,
        bool SelectPressed
        );
}