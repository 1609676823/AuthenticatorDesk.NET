using System.ComponentModel;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;
using AuthenticatorDesk.Services;
using AuthenticatorDesk.UI;
using AntButton = AntdUI.Button;
using AntLabel = AntdUI.Label;
using AntPanel = AntdUI.Panel;
using AntProgress = AntdUI.Progress;

namespace AuthenticatorDesk.UI.Controls;

public enum AuthenticatorCardAction
{
    Copy,
    Reveal,
    Favorite,
    Edit,
    Delete,
    Export,
    ShowSecret,
    ShowRestoreCode,
    AdvanceCounter,
    MoveUp,
    MoveDown
}

public sealed class AuthenticatorCardActionEventArgs(
    AuthenticatorEntry entry,
    AuthenticatorCardAction action) : EventArgs
{
    public AuthenticatorEntry Entry { get; } = entry;

    public AuthenticatorCardAction Action { get; } = action;
}

public sealed class AuthenticatorCard : AntPanel
{
    private readonly ProviderBadge _providerBadge;
    private readonly AntLabel _issuerLabel;
    private readonly AntLabel _nameLabel;
    private readonly AntLabel _codeLabel;
    private readonly AntLabel _statusLabel;
    private readonly AntProgress _progress;
    private readonly AntButton _favoriteButton;
    private readonly AntButton _revealButton;
    private readonly AntButton _copyButton;
    private readonly AntButton _moreButton;
    private bool? _codeHiddenOverride;
    private string _currentCode = string.Empty;
    private bool _globalHideCodes;
    private bool _compact;

    public AuthenticatorCard(AuthenticatorEntry entry)
    {
        Entry = entry;
        DoubleBuffered = true;
        Radius = 16;
        Shadow = 10;
        ShadowOffsetY = 3;
        ShadowOpacity = 0.09F;
        ShadowOpacityHover = 0.17F;
        BorderWidth = 1F;
        Height = 184;
        MinimumSize = new Size(270, 154);
        Margin = new Padding(8);
        Cursor = Cursors.Default;

        _providerBadge = new ProviderBadge { Entry = entry };
        _nameLabel = new AntLabel
        {
            AutoEllipsis = true,
            Font = LocalizationFonts.Create(12F, FontStyle.Bold),
            Text = entry.DisplayName,
            BackColor = Color.Transparent
        };
        _issuerLabel = new AntLabel
        {
            AutoEllipsis = true,
            Font = LocalizationFonts.Create(9F, FontStyle.Regular),
            Text = entry.DisplayIssuer,
            BackColor = Color.Transparent
        };
        _codeLabel = new AntLabel
        {
            AutoEllipsis = true,
            Cursor = Cursors.Hand,
            Font = new Font("Cascadia Mono", 22F, FontStyle.Bold),
            Text = "••• •••",
            BackColor = Color.Transparent
        };
        _statusLabel = new AntLabel
        {
            AutoEllipsis = true,
            Font = LocalizationFonts.Create(8.5F, FontStyle.Regular),
            BackColor = Color.Transparent
        };
        _progress = new AntProgress
        {
            Shape = AntdUI.TShapeProgress.Circle,
            UseSystemText = true,
            Value = 1F,
            Animation = 120,
            BackColor = Color.Transparent
        };
        _favoriteButton = IconButton(
            entry.Favorite ? "StarFilled" : "StarOutlined",
            L.Get("AuthenticatorCard.Tooltip.Favorite"));
        _revealButton = IconButton(
            "EyeOutlined",
            L.Get("AuthenticatorCard.Tooltip.ShowCode"));
        _copyButton = IconButton(
            "CopyOutlined",
            L.Get("AuthenticatorCard.Tooltip.CopyCode"));
        _moreButton = IconButton(
            "MoreOutlined",
            L.Get("AuthenticatorCard.Tooltip.More"));
        Controls.AddRange(
        [
            _providerBadge,
            _nameLabel,
            _issuerLabel,
            _codeLabel,
            _statusLabel,
            _progress,
            _favoriteButton,
            _revealButton,
            _copyButton,
            _moreButton
        ]);

        _favoriteButton.Click += (_, _) => RaiseAction(AuthenticatorCardAction.Favorite);
        _revealButton.Click += (_, _) =>
        {
            ToggleCodeVisibility();
            RaiseAction(AuthenticatorCardAction.Reveal);
        };
        _copyButton.Click += (_, _) => RaiseAction(AuthenticatorCardAction.Copy);
        _moreButton.Click += (_, _) => ShowContextMenu();
        _codeLabel.Click += (_, _) => RaiseAction(AuthenticatorCardAction.Copy);

        ApplyTheme();
        RefreshCode(DateTimeOffset.UtcNow);
    }

    public event EventHandler<AuthenticatorCardActionEventArgs>? ActionRequested;

    public AuthenticatorEntry Entry { get; }

    public string CurrentCode => _currentCode;

    internal bool CodeHidden =>
        _codeHiddenOverride ?? DefaultCodeHidden;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool GlobalHideCodes
    {
        get => _globalHideCodes;
        set
        {
            if (_globalHideCodes == value)
            {
                return;
            }

            _globalHideCodes = value;
            _codeHiddenOverride = null;
            UpdateCodePresentation();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Compact
    {
        get => _compact;
        set
        {
            if (_compact == value)
            {
                return;
            }

            _compact = value;
            var scale = DeviceDpi / 96F;
            Height = Math.Max(
                1,
                (int)Math.Round((value ? 154 : 184) * scale));
            PerformLayout();
        }
    }

    internal bool ToggleCodeVisibility()
    {
        SetCodeHidden(!CodeHidden);
        return !CodeHidden;
    }

    public void HideNow()
    {
        SetCodeHidden(true);
    }

    internal void SetCodeHidden(bool hidden)
    {
        _codeHiddenOverride = hidden;
        UpdateCodePresentation();
    }

    public void RefreshCode(DateTimeOffset instant)
    {
        try
        {
            if (Entry.AutoRefresh ||
                Entry.Kind == AuthenticatorKind.Hotp ||
                string.IsNullOrEmpty(_currentCode))
            {
                _currentCode = OtpService.GetCode(Entry, instant);
            }

            if (Entry.Kind == AuthenticatorKind.Hotp)
            {
                _progress.Value = 1F;
                _progress.Text = "#";
                _statusLabel.Text = L.Format(
                    "AuthenticatorCard.Status.Counter",
                    Entry.Counter);
            }
            else
            {
                var remaining = OtpService.GetRemainingSeconds(Entry, instant);
                _progress.Value = (float)OtpService.GetRemainingFraction(Entry, instant);
                _progress.Text = remaining.ToString();
                _statusLabel.Text = Entry.AutoRefresh
                    ? L.Format(
                        "AuthenticatorCard.Status.RefreshInSeconds",
                        remaining)
                    : L.Get("AuthenticatorCard.Status.AutoRefreshDisabled");
            }

            UpdateCodePresentation();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            _currentCode = string.Empty;
            _codeLabel.Text = L.Get("AuthenticatorCard.Error.InvalidSecret");
            _statusLabel.Text = exception.Message;
            _progress.Value = 0F;
        }
    }

    public void RefreshEntryPresentation()
    {
        _providerBadge.Entry = Entry;
        _issuerLabel.Text = Entry.DisplayIssuer;
        _nameLabel.Text = Entry.DisplayName;
        _favoriteButton.IconSvg = Entry.Favorite ? "StarFilled" : "StarOutlined";
        RefreshCode(DateTimeOffset.UtcNow);
    }

    public void ApplyTheme()
    {
        var palette = ThemePaletteService.Current;
        Back = palette.Surface;
        BorderColor = palette.Border;
        ShadowColor = palette.Dark ? Color.Black : Color.FromArgb(60, 78, 118);
        _nameLabel.ForeColor = palette.Text;
        _issuerLabel.ForeColor = palette.TextSecondary;
        _codeLabel.ForeColor = palette.Text;
        _statusLabel.ForeColor = palette.TextSecondary;
        _progress.Fill = ParseAccent();
        _progress.Back = palette.SurfaceMuted;
        _progress.ForeColor = palette.TextSecondary;
        _favoriteButton.ForeColor = Entry.Favorite ? palette.Warning : palette.TextSecondary;
        _revealButton.ForeColor = palette.TextSecondary;
        _copyButton.ForeColor = palette.Primary;
        _moreButton.ForeColor = palette.TextSecondary;
        Invalidate(true);
    }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        LayoutControls();
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        LayoutControls();
    }

    private void LayoutControls()
    {
        // AntdUI panel appearance setters can raise Resize while the constructor
        // is still assigning child controls.
        if (_providerBadge is null ||
            _issuerLabel is null ||
            _nameLabel is null ||
            _codeLabel is null ||
            _statusLabel is null ||
            _progress is null ||
            _favoriteButton is null ||
            _revealButton is null ||
            _copyButton is null ||
            _moreButton is null)
        {
            return;
        }

        var scale = DeviceDpi / 96F;
        int S(int value) => Math.Max(1, (int)Math.Round(value * scale));

        var left = S(18);
        var top = S(16);
        var badgeSize = S(46);
        var iconSize = S(34);
        var right = Width - S(18);

        _providerBadge.SetBounds(left, top, badgeSize, badgeSize);
        _favoriteButton.SetBounds(right - iconSize, top, iconSize, iconSize);
        var titleLeft = left + badgeSize + S(12);
        var titleWidth = Math.Max(S(70), _favoriteButton.Left - titleLeft - S(6));
        _nameLabel.SetBounds(titleLeft, top - S(1), titleWidth, S(27));
        _issuerLabel.SetBounds(titleLeft, top + S(27), titleWidth, S(19));

        var progressSize = _compact ? S(44) : S(50);
        var codeTop = _compact ? S(72) : S(78);
        _progress.SetBounds(right - progressSize, codeTop - S(2), progressSize, progressSize);
        _codeLabel.SetBounds(
            left,
            codeTop,
            Math.Max(S(100), _progress.Left - left - S(10)),
            _compact ? S(38) : S(44));
        _statusLabel.SetBounds(
            left,
            codeTop + (_compact ? S(37) : S(43)),
            Math.Max(S(100), _progress.Left - left - S(10)),
            S(21));

        var buttonTop = Height - S(42);
        if (_compact)
        {
            _revealButton.SetBounds(right - iconSize * 3 - S(12), buttonTop, iconSize, iconSize);
            _copyButton.SetBounds(right - iconSize * 2 - S(6), buttonTop, iconSize, iconSize);
            _moreButton.SetBounds(right - iconSize, buttonTop, iconSize, iconSize);
        }
        else
        {
            _revealButton.SetBounds(left, buttonTop, iconSize, iconSize);
            _copyButton.SetBounds(left + iconSize + S(6), buttonTop, S(92), iconSize);
            _copyButton.Text = Width >= S(340)
                ? L.Get("Common.Copy")
                : string.Empty;
            _moreButton.SetBounds(right - iconSize, buttonTop, iconSize, iconSize);
        }
    }

    private bool DefaultCodeHidden =>
        _globalHideCodes || Entry.RequireUnlock || !Entry.AutoRefresh;

    private void UpdateCodePresentation()
    {
        if (string.IsNullOrEmpty(_currentCode))
        {
            return;
        }

        var hidden = CodeHidden;
        _codeLabel.Text = hidden ? Mask(_currentCode) : OtpService.FormatCode(_currentCode);
        _revealButton.IconSvg = hidden ? "EyeOutlined" : "EyeInvisibleOutlined";
        _revealButton.Tag = hidden
            ? L.Get("AuthenticatorCard.Tooltip.ShowCode")
            : L.Get("Common.Hide");
        _revealButton.AccessibleName = _revealButton.Tag as string;
    }

    private void RaiseAction(AuthenticatorCardAction action)
    {
        ActionRequested?.Invoke(this, new AuthenticatorCardActionEventArgs(Entry, action));
    }

    private void ShowContextMenu()
    {
        var items = BuildContextMenuItems();
        AntdUI.ContextMenuStrip.open(
            _moreButton,
            item =>
            {
                if (item.Tag is AuthenticatorCardAction action)
                {
                    RaiseAction(action);
                }
            },
            [.. items],
            sleep: InteractionTiming.ContextMenuCloseDelayMilliseconds);
    }

    internal IReadOnlyList<AntdUI.IContextMenuStripItem> BuildContextMenuItems()
    {
        var items = new List<AntdUI.IContextMenuStripItem>();

        if (Entry.Kind == AuthenticatorKind.Hotp)
        {
            items.Add(new AntdUI.ContextMenuStripItem(
                L.Get("AuthenticatorCard.GenerateNext"))
            {
                IconSvg = "StepForwardOutlined",
                Tag = AuthenticatorCardAction.AdvanceCounter
            });
        }

        items.AddRange(
        [
            new AntdUI.ContextMenuStripItem(L.Get("AuthenticatorCard.Menu.Edit"))
            {
                IconSvg = "EditOutlined",
                Tag = AuthenticatorCardAction.Edit
            },
            new AntdUI.ContextMenuStripItem(L.Get("AuthenticatorCard.Menu.ExportQrOrUri"))
            {
                IconSvg = "QrcodeOutlined",
                Tag = AuthenticatorCardAction.Export
            },
            new AntdUI.ContextMenuStripItem(L.Get("AuthenticatorCard.Menu.ShowSecret"))
            {
                IconSvg = "KeyOutlined",
                Tag = AuthenticatorCardAction.ShowSecret
            }
        ]);

        if (Entry.Kind == AuthenticatorKind.BattleNet && !string.IsNullOrWhiteSpace(Entry.Serial))
        {
            items.Add(new AntdUI.ContextMenuStripItem(
                L.Get("AuthenticatorCard.Menu.ShowRecoveryCode"))
            {
                IconSvg = "SafetyCertificateOutlined",
                Tag = AuthenticatorCardAction.ShowRestoreCode
            });
        }

        items.Add(new AntdUI.ContextMenuStripItem(
            L.Get("AuthenticatorCard.Menu.MoveUp"))
        {
            IconSvg = "ArrowUpOutlined",
            Tag = AuthenticatorCardAction.MoveUp
        });
        items.Add(new AntdUI.ContextMenuStripItem(
            L.Get("AuthenticatorCard.Menu.MoveDown"))
        {
            IconSvg = "ArrowDownOutlined",
            Tag = AuthenticatorCardAction.MoveDown
        });
        items.Add(new AntdUI.ContextMenuStripItem(
            L.Get("AuthenticatorCard.Menu.Delete"))
        {
            IconSvg = "DeleteOutlined",
            Fore = ThemePaletteService.Current.Danger,
            Tag = AuthenticatorCardAction.Delete
        });

        return items;
    }

    private AntButton IconButton(string icon, string tooltip)
    {
        var button = new AntButton
        {
            IconSvg = icon,
            Ghost = true,
            Radius = 9,
            WaveSize = 1,
            Tag = tooltip
        };
        button.AccessibleName = tooltip;
        button.MouseEnter += (_, _) => AntdUI.Tooltip.open(
            button,
            button.Tag as string ?? tooltip,
            AntdUI.TAlign.Top);
        return button;
    }

    private Color ParseAccent()
    {
        try
        {
            return ThemePaletteService.AdaptAccent(
                ColorTranslator.FromHtml(Entry.AccentColor));
        }
        catch
        {
            return ThemePaletteService.Current.Primary;
        }
    }

    private static string Mask(string code)
    {
        if (code.All(char.IsDigit) && code.Length is 6 or 8 or 10)
        {
            return code.Length switch
            {
                6 => "••• •••",
                8 => "•••• ••••",
                _ => "••••• •••••"
            };
        }

        return new string('•', Math.Clamp(code.Length, 5, 10));
    }
}
