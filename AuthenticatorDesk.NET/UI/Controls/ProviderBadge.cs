using System.ComponentModel;
using System.Drawing.Drawing2D;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.UI.Controls;

public sealed class ProviderBadge : Control
{
    private AuthenticatorEntry? _entry;

    public ProviderBadge()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(46, 46);
        Font = LocalizationFonts.Create(11F, FontStyle.Bold);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AuthenticatorEntry? Entry
    {
        get => _entry;
        set
        {
            _entry = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (_entry is null)
        {
            return;
        }

        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
        if (SystemInformation.HighContrast)
        {
            using var highContrastBackground = new SolidBrush(SystemColors.Highlight);
            using var highContrastBorder = new Pen(SystemColors.WindowText, 1.5F);
            using var highContrastPath = RoundedRectangle(bounds, Math.Max(8, bounds.Width / 3F));
            eventArgs.Graphics.FillPath(highContrastBackground, highContrastPath);
            eventArgs.Graphics.DrawPath(highContrastBorder, highContrastPath);
            TextRenderer.DrawText(
                eventArgs.Graphics,
                ProviderInitials(_entry),
                Font,
                bounds,
                SystemColors.HighlightText,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding);
            return;
        }

        var accent = ThemePaletteService.AdaptAccent(
            ParseColor(_entry.AccentColor, ThemePaletteService.Current.Primary));
        using var background = new SolidBrush(Color.FromArgb(
            ThemePaletteService.Current.Dark ? 70 : 32,
            accent));
        using var border = new Pen(Color.FromArgb(
            ThemePaletteService.Current.Dark ? 135 : 80,
            accent), 1.5F);
        using var path = RoundedRectangle(bounds, Math.Max(8, bounds.Width / 3F));
        eventArgs.Graphics.FillPath(background, path);
        eventArgs.Graphics.DrawPath(border, path);

        var text = ProviderInitials(_entry);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            text,
            Font,
            bounds,
            accent,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding);
    }

    private static string ProviderInitials(AuthenticatorEntry entry)
    {
        return entry.Kind switch
        {
            AuthenticatorKind.Steam => "ST",
            AuthenticatorKind.BattleNet => "BN",
            AuthenticatorKind.Trion => "TR",
            AuthenticatorKind.Google => "G",
            AuthenticatorKind.Microsoft => "M",
            AuthenticatorKind.OktaVerify => "O",
            AuthenticatorKind.GuildWars => "GW",
            AuthenticatorKind.Hotp => "H",
            _ => Initials(entry.DisplayIssuer)
        };
    }

    private static string Initials(string value)
    {
        var words = value.Split(
            [' ', '-', '_', '/', '.'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
        {
            return "OTP";
        }

        return string.Concat(words.Take(2).Select(word => char.ToUpperInvariant(word[0])));
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : ColorTranslator.FromHtml(value);
        }
        catch
        {
            return fallback;
        }
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, float radius)
    {
        var diameter = Math.Min(radius * 2F, Math.Min(bounds.Width, bounds.Height));
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
