namespace AuthenticatorDesk.UI;

internal static class InteractionTiming
{
    // AntdUI closes layered context menus asynchronously over roughly 100 ms.
    public const int ContextMenuCloseDelayMilliseconds = 150;
}
