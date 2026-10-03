namespace SidePanelApp.V6.Controls
{
    /// <summary>
    /// Сторона попапа, с которой «клюв» указывает на кольцо панели.
    /// </summary>
    public enum CalloutTailSide
    {
        Right,
        Left
    }

    internal static class CalloutContentBounds
    {
        public const int TailMargin = 6;

        public static void GetHorizontal(int width, CalloutTailSide tailSide, out int left, out int right)
        {
            if (tailSide == CalloutTailSide.Right)
            {
                left = 0;
                right = width - TailMargin;
            }
            else
            {
                left = TailMargin;
                right = width;
            }
        }
    }
}
