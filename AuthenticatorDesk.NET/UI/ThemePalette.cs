using AuthenticatorDesk.Models;
using Microsoft.Win32;

namespace AuthenticatorDesk.UI;

public sealed record ThemePalette(
    bool Dark,
    Color Canvas,
    Color Surface,
    Color SurfaceElevated,
    Color SurfaceMuted,
    Color Border,
    Color Text,
    Color TextSecondary,
    Color Primary,
    Color PrimarySoft,
    Color Success,
    Color Warning,
    Color Danger,
    Color EditorCanvas,
    Color EditorSurface,
    Color EditorChrome,
    Color EditorBorder);

public static class ThemePaletteService
{
    public static ThemePalette Current { get; private set; } = Create(true);

    public static ThemePalette Apply(AppThemeMode mode)
    {
        var highContrast = SystemInformation.HighContrast;
        var dark = highContrast
            ? IsDark(SystemColors.Window)
            : mode switch
            {
                AppThemeMode.Dark => true,
                AppThemeMode.Light => false,
                _ => IsSystemDark()
            };

        Current = highContrast ? CreateHighContrast(dark) : Create(dark);
#pragma warning disable WFO5001
        Application.SetColorMode(highContrast
            ? SystemColorMode.System
            : mode switch
            {
                AppThemeMode.System => SystemColorMode.System,
                AppThemeMode.Dark => SystemColorMode.Dark,
                _ => SystemColorMode.Classic
            });
#pragma warning restore WFO5001
        AntdUI.Config.IsLight = !dark;
        AntdUI.Config.Animation = true;
        AntdUI.Config.ShowInWindow = true;
        if (highContrast)
        {
            ApplyHighContrastControlTokens(Current);
        }
        else
        {
            ApplyControlTokens(Current);
        }

        return Current;
    }

    public static bool IsSystemDark()
    {
        if (SystemInformation.HighContrast)
        {
            return IsDark(SystemColors.Window);
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    public static Color AdaptAccent(Color accent)
    {
        if (SystemInformation.HighContrast)
        {
            return SystemColors.Highlight;
        }

        if (!Current.Dark)
        {
            return accent;
        }

        return Color.FromArgb(
            accent.A,
            (int)Math.Round((accent.R * 0.68D) + (255D * 0.32D)),
            (int)Math.Round((accent.G * 0.68D) + (255D * 0.32D)),
            (int)Math.Round((accent.B * 0.68D) + (255D * 0.32D)));
    }

    public static void StyleSecondaryButton(
        AntdUI.Button button,
        Color? background = null,
        Color? border = null)
    {
        ArgumentNullException.ThrowIfNull(button);
        var palette = Current;
        var highContrast = SystemInformation.HighContrast;
        var resolvedBackground = highContrast
            ? SystemColors.Control
            : background ?? palette.SurfaceMuted;
        var resolvedBorder = highContrast
            ? SystemColors.WindowText
            : border ?? palette.Border;
        button.Ghost = false;
        button.BackColor = resolvedBackground;
        button.DefaultBack = resolvedBackground;
        button.DefaultBorderColor = resolvedBorder;
        button.BorderWidth = 1F;
        button.ForeColor = highContrast ? SystemColors.ControlText : palette.Text;
        button.BackHover = highContrast
            ? SystemColors.Highlight
            : palette.PrimarySoft;
        button.BackActive = highContrast
            ? SystemColors.Highlight
            : palette.PrimarySoft;
        button.ForeHover = highContrast
            ? SystemColors.HighlightText
            : palette.Text;
        button.ForeActive = highContrast
            ? SystemColors.HighlightText
            : palette.Text;
    }

    private static ThemePalette Create(bool dark)
    {
        return dark
            ? new ThemePalette(
                true,
                Color.FromArgb(13, 19, 32),
                Color.FromArgb(20, 28, 45),
                Color.FromArgb(25, 35, 56),
                Color.FromArgb(31, 43, 67),
                Color.FromArgb(83, 100, 131),
                Color.FromArgb(242, 246, 255),
                Color.FromArgb(154, 168, 193),
                Color.FromArgb(134, 147, 255),
                Color.FromArgb(38, 47, 88),
                Color.FromArgb(47, 194, 139),
                Color.FromArgb(242, 180, 69),
                Color.FromArgb(255, 122, 135),
                Color.FromArgb(27, 37, 64),
                Color.FromArgb(37, 50, 82),
                Color.FromArgb(32, 44, 75),
                Color.FromArgb(90, 111, 168))
            : new ThemePalette(
                false,
                Color.FromArgb(244, 247, 252),
                Color.White,
                Color.FromArgb(248, 250, 255),
                Color.FromArgb(236, 241, 249),
                Color.FromArgb(218, 226, 238),
                Color.FromArgb(20, 29, 47),
                Color.FromArgb(98, 111, 133),
                Color.FromArgb(73, 91, 232),
                Color.FromArgb(231, 235, 255),
                Color.FromArgb(20, 159, 111),
                Color.FromArgb(211, 140, 28),
                Color.FromArgb(217, 67, 80),
                Color.FromArgb(230, 234, 248),
                Color.FromArgb(237, 241, 255),
                Color.FromArgb(220, 227, 250),
                Color.FromArgb(171, 184, 230));
    }

    private static ThemePalette CreateHighContrast(bool dark)
    {
        return new ThemePalette(
            dark,
            SystemColors.Window,
            SystemColors.Window,
            SystemColors.Window,
            SystemColors.Window,
            SystemColors.WindowText,
            SystemColors.WindowText,
            SystemColors.WindowText,
            SystemColors.Highlight,
            SystemColors.Window,
            SystemColors.WindowText,
            SystemColors.WindowText,
            SystemColors.WindowText,
            SystemColors.Window,
            SystemColors.Window,
            SystemColors.Window,
            SystemColors.WindowText);
    }

    private static void ApplyHighContrastControlTokens(ThemePalette palette)
    {
        AntdUI.Style.Clear();
        AntdUI.Style.Set(AntdUI.Colour.Primary, SystemColors.Highlight);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryColor, SystemColors.HighlightText);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryHover, SystemColors.Highlight);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryActive, SystemColors.Highlight);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBg, SystemColors.Window);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBgHover, SystemColors.Highlight);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBorder, SystemColors.WindowText);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBorderHover, SystemColors.Highlight);
        SetHighContrastSemanticTokens(
            AntdUI.Colour.Success,
            AntdUI.Colour.SuccessColor,
            AntdUI.Colour.SuccessBg,
            AntdUI.Colour.SuccessBorder,
            AntdUI.Colour.SuccessHover,
            AntdUI.Colour.SuccessActive);
        SetHighContrastSemanticTokens(
            AntdUI.Colour.Warning,
            AntdUI.Colour.WarningColor,
            AntdUI.Colour.WarningBg,
            AntdUI.Colour.WarningBorder,
            AntdUI.Colour.WarningHover,
            AntdUI.Colour.WarningActive);
        SetHighContrastSemanticTokens(
            AntdUI.Colour.Error,
            AntdUI.Colour.ErrorColor,
            AntdUI.Colour.ErrorBg,
            AntdUI.Colour.ErrorBorder,
            AntdUI.Colour.ErrorHover,
            AntdUI.Colour.ErrorActive);
        SetHighContrastSemanticTokens(
            AntdUI.Colour.Info,
            AntdUI.Colour.InfoColor,
            AntdUI.Colour.InfoBg,
            AntdUI.Colour.InfoBorder,
            AntdUI.Colour.InfoHover,
            AntdUI.Colour.InfoActive);
        AntdUI.Style.Set(AntdUI.Colour.TextBase, palette.Text);
        AntdUI.Style.Set(AntdUI.Colour.Text, palette.Text);
        AntdUI.Style.Set(AntdUI.Colour.TextSecondary, palette.Text);
        AntdUI.Style.Set(AntdUI.Colour.TextTertiary, palette.Text);
        AntdUI.Style.Set(AntdUI.Colour.TextQuaternary, SystemColors.GrayText);
        AntdUI.Style.Set(AntdUI.Colour.BgBase, palette.Canvas);
        AntdUI.Style.Set(AntdUI.Colour.BgLayout, palette.Canvas);
        AntdUI.Style.Set(AntdUI.Colour.BgContainer, palette.Surface);
        AntdUI.Style.Set(AntdUI.Colour.BgElevated, palette.SurfaceElevated);
        AntdUI.Style.Set(AntdUI.Colour.Fill, SystemColors.Window);
        AntdUI.Style.Set(AntdUI.Colour.FillSecondary, SystemColors.Window);
        AntdUI.Style.Set(AntdUI.Colour.FillTertiary, SystemColors.Window);
        AntdUI.Style.Set(AntdUI.Colour.FillQuaternary, SystemColors.Window);
        AntdUI.Style.Set(AntdUI.Colour.BorderColor, palette.Border);
        AntdUI.Style.Set(AntdUI.Colour.BorderColorDisable, SystemColors.GrayText);
        AntdUI.Style.Set(AntdUI.Colour.Split, palette.Border);
        AntdUI.Style.Set(AntdUI.Colour.HoverBg, SystemColors.Highlight);
        AntdUI.Style.Set(AntdUI.Colour.HoverColor, SystemColors.HighlightText);
        AntdUI.Style.Set(AntdUI.Colour.TextSpotlight, SystemColors.HighlightText);
        AntdUI.Style.Set(AntdUI.Colour.BgSpotlight, SystemColors.Highlight);
    }

    private static void SetHighContrastSemanticTokens(
        AntdUI.Colour color,
        AntdUI.Colour text,
        AntdUI.Colour background,
        AntdUI.Colour border,
        AntdUI.Colour hover,
        AntdUI.Colour active)
    {
        AntdUI.Style.Set(color, SystemColors.Highlight);
        AntdUI.Style.Set(text, SystemColors.HighlightText);
        AntdUI.Style.Set(background, SystemColors.Window);
        AntdUI.Style.Set(border, SystemColors.WindowText);
        AntdUI.Style.Set(hover, SystemColors.Highlight);
        AntdUI.Style.Set(active, SystemColors.Highlight);
    }

    private static void ApplyControlTokens(ThemePalette palette)
    {
        var primaryHover = palette.Dark
            ? Color.FromArgb(156, 166, 255)
            : Color.FromArgb(94, 111, 239);
        var primaryActive = palette.Dark
            ? Color.FromArgb(111, 126, 255)
            : Color.FromArgb(57, 73, 204);
        var primarySoftHover = palette.Dark
            ? Color.FromArgb(48, 59, 105)
            : Color.FromArgb(216, 222, 255);
        var primaryBorder = palette.Dark
            ? Color.FromArgb(70, 82, 140)
            : Color.FromArgb(195, 203, 255);
        var textTertiary = palette.Dark
            ? Color.FromArgb(119, 133, 157)
            : Color.FromArgb(126, 138, 158);
        var textQuaternary = palette.Dark
            ? Color.FromArgb(82, 96, 119)
            : Color.FromArgb(166, 176, 193);
        var fill = palette.Dark
            ? Color.FromArgb(54, 66, 87)
            : Color.FromArgb(204, 212, 224);
        var fillSecondary = palette.Dark
            ? Color.FromArgb(43, 55, 77)
            : Color.FromArgb(219, 225, 235);
        var fillTertiary = palette.Dark
            ? Color.FromArgb(35, 46, 66)
            : Color.FromArgb(231, 236, 244);
        var fillQuaternary = palette.Dark
            ? Color.FromArgb(28, 38, 57)
            : Color.FromArgb(239, 242, 248);
        var disabledBorder = palette.Dark
            ? Color.FromArgb(39, 51, 73)
            : Color.FromArgb(226, 232, 241);
        var successBackground = palette.Dark
            ? Color.FromArgb(23, 57, 50)
            : Color.FromArgb(226, 247, 239);
        var successBorder = palette.Dark
            ? Color.FromArgb(40, 105, 82)
            : Color.FromArgb(153, 222, 197);
        var warningBackground = palette.Dark
            ? Color.FromArgb(62, 48, 26)
            : Color.FromArgb(255, 246, 222);
        var warningBorder = palette.Dark
            ? Color.FromArgb(123, 91, 35)
            : Color.FromArgb(241, 207, 132);
        var dangerBackground = palette.Dark
            ? Color.FromArgb(64, 34, 43)
            : Color.FromArgb(255, 235, 238);
        var dangerBorder = palette.Dark
            ? Color.FromArgb(126, 56, 70)
            : Color.FromArgb(244, 169, 179);
        var primaryText = ContrastingText(palette.Primary, palette);
        var successText = ContrastingText(palette.Success, palette);
        var warningText = ContrastingText(palette.Warning, palette);
        var dangerText = ContrastingText(palette.Danger, palette);

        AntdUI.Style.Clear();
        AntdUI.Style.Set(AntdUI.Colour.Primary, palette.Primary);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryColor, primaryText);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryHover, primaryHover);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryActive, primaryActive);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBg, palette.PrimarySoft);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBgHover, primarySoftHover);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBorder, primaryBorder);
        AntdUI.Style.Set(AntdUI.Colour.PrimaryBorderHover, palette.Primary);
        AntdUI.Style.Set(AntdUI.Colour.Success, palette.Success);
        AntdUI.Style.Set(AntdUI.Colour.SuccessColor, successText);
        AntdUI.Style.Set(AntdUI.Colour.SuccessBg, successBackground);
        AntdUI.Style.Set(AntdUI.Colour.SuccessBorder, successBorder);
        AntdUI.Style.Set(AntdUI.Colour.SuccessHover, Lighten(palette.Success, palette.Dark ? 24 : 12));
        AntdUI.Style.Set(AntdUI.Colour.SuccessActive, Darken(palette.Success, palette.Dark ? 22 : 16));
        AntdUI.Style.Set(AntdUI.Colour.Warning, palette.Warning);
        AntdUI.Style.Set(AntdUI.Colour.WarningColor, warningText);
        AntdUI.Style.Set(AntdUI.Colour.WarningBg, warningBackground);
        AntdUI.Style.Set(AntdUI.Colour.WarningBorder, warningBorder);
        AntdUI.Style.Set(AntdUI.Colour.WarningHover, Lighten(palette.Warning, palette.Dark ? 18 : 10));
        AntdUI.Style.Set(AntdUI.Colour.WarningActive, Darken(palette.Warning, palette.Dark ? 24 : 18));
        AntdUI.Style.Set(AntdUI.Colour.Error, palette.Danger);
        AntdUI.Style.Set(AntdUI.Colour.ErrorColor, dangerText);
        AntdUI.Style.Set(AntdUI.Colour.ErrorBg, dangerBackground);
        AntdUI.Style.Set(AntdUI.Colour.ErrorBorder, dangerBorder);
        AntdUI.Style.Set(AntdUI.Colour.ErrorHover, Lighten(palette.Danger, palette.Dark ? 20 : 10));
        AntdUI.Style.Set(AntdUI.Colour.ErrorActive, Darken(palette.Danger, palette.Dark ? 22 : 16));
        AntdUI.Style.Set(AntdUI.Colour.Info, palette.Primary);
        AntdUI.Style.Set(AntdUI.Colour.InfoColor, primaryText);
        AntdUI.Style.Set(AntdUI.Colour.InfoBg, palette.PrimarySoft);
        AntdUI.Style.Set(AntdUI.Colour.InfoBorder, primaryBorder);
        AntdUI.Style.Set(AntdUI.Colour.InfoHover, primaryHover);
        AntdUI.Style.Set(AntdUI.Colour.InfoActive, primaryActive);
        AntdUI.Style.Set(AntdUI.Colour.TextBase, palette.Text);
        AntdUI.Style.Set(AntdUI.Colour.Text, palette.Text);
        AntdUI.Style.Set(AntdUI.Colour.TextSecondary, palette.TextSecondary);
        AntdUI.Style.Set(AntdUI.Colour.TextTertiary, textTertiary);
        AntdUI.Style.Set(AntdUI.Colour.TextQuaternary, textQuaternary);
        AntdUI.Style.Set(AntdUI.Colour.BgBase, palette.Canvas);
        AntdUI.Style.Set(AntdUI.Colour.BgLayout, palette.Canvas);
        AntdUI.Style.Set(AntdUI.Colour.BgContainer, palette.Surface);
        AntdUI.Style.Set(AntdUI.Colour.BgElevated, palette.SurfaceElevated);
        AntdUI.Style.Set(AntdUI.Colour.Fill, fill);
        AntdUI.Style.Set(AntdUI.Colour.FillSecondary, fillSecondary);
        AntdUI.Style.Set(AntdUI.Colour.FillTertiary, fillTertiary);
        AntdUI.Style.Set(AntdUI.Colour.FillQuaternary, fillQuaternary);
        AntdUI.Style.Set(AntdUI.Colour.BorderColor, palette.Border);
        AntdUI.Style.Set(AntdUI.Colour.BorderColorDisable, disabledBorder);
        AntdUI.Style.Set(AntdUI.Colour.Split, palette.Border);
        AntdUI.Style.Set(AntdUI.Colour.HoverBg, palette.SurfaceMuted);
        AntdUI.Style.Set(AntdUI.Colour.HoverColor, palette.Text);
        AntdUI.Style.Set(AntdUI.Colour.TextSpotlight, Color.White);
        AntdUI.Style.Set(AntdUI.Colour.BgSpotlight, Color.FromArgb(30, 36, 49));
    }

    private static Color Lighten(Color color, int amount)
    {
        return Color.FromArgb(
            color.A,
            Math.Min(byte.MaxValue, color.R + amount),
            Math.Min(byte.MaxValue, color.G + amount),
            Math.Min(byte.MaxValue, color.B + amount));
    }

    private static Color Darken(Color color, int amount)
    {
        return Color.FromArgb(
            color.A,
            Math.Max(byte.MinValue, color.R - amount),
            Math.Max(byte.MinValue, color.G - amount),
            Math.Max(byte.MinValue, color.B - amount));
    }

    private static Color ContrastingText(Color background, ThemePalette palette)
    {
        var darkText = palette.Dark ? palette.Canvas : palette.Text;
        return ContrastRatio(background, Color.White) >= ContrastRatio(background, darkText)
            ? Color.White
            : darkText;
    }

    private static double ContrastRatio(Color left, Color right)
    {
        var leftLuminance = RelativeLuminance(left);
        var rightLuminance = RelativeLuminance(right);
        return (Math.Max(leftLuminance, rightLuminance) + 0.05D) /
               (Math.Min(leftLuminance, rightLuminance) + 0.05D);
    }

    private static double RelativeLuminance(Color color)
    {
        static double Channel(byte value)
        {
            var component = value / 255D;
            return component <= 0.04045D
                ? component / 12.92D
                : Math.Pow((component + 0.055D) / 1.055D, 2.4D);
        }

        return (0.2126D * Channel(color.R)) +
               (0.7152D * Channel(color.G)) +
               (0.0722D * Channel(color.B));
    }

    private static bool IsDark(Color color)
    {
        return RelativeLuminance(color) < 0.5D;
    }
}
