using System.Drawing;

namespace AuthenticatorDesk.Localization;

public static class LocalizationFonts
{
    public static Font Create(
        float emSize,
        FontStyle style = FontStyle.Regular)
    {
        if (!float.IsFinite(emSize) || emSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emSize),
                "Font size must be a positive finite number.");
        }

        var fontFamily =
            SystemFonts.MessageBoxFont?.FontFamily ??
            FontFamily.GenericSansSerif;
        return new Font(
            fontFamily,
            emSize,
            style,
            GraphicsUnit.Point);
    }
}
