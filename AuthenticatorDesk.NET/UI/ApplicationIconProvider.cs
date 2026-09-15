namespace AuthenticatorDesk.UI;

internal static class ApplicationIconProvider
{
    private const string ResourceName =
        "AuthenticatorDesk.Assets.AuthenticatorDesk.Icon.ico";

    private static readonly byte[] IconData = LoadIconData();

    public static Icon Create(Size size)
    {
        using var stream = new MemoryStream(IconData, writable: false);
        using var icon = new Icon(stream, size);
        return (Icon)icon.Clone();
    }

    private static byte[] LoadIconData()
    {
        using var stream = typeof(ApplicationIconProvider).Assembly
            .GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded application icon '{ResourceName}' was not found.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
