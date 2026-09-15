using System.Runtime.InteropServices;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;
using AuthenticatorDesk.Services;
using AuthenticatorDesk.UI.Controls;
using AntButton = AntdUI.Button;
using AntCheckbox = AntdUI.Checkbox;
using AntInput = AntdUI.Input;
using AntLabel = AntdUI.Label;
using AntProgress = AntdUI.Progress;
using AntSelect = AntdUI.Select;

namespace AuthenticatorDesk.UI.Dialogs;

/// <summary>
/// Compiled visual base used by the out-of-process inherited-form designer.
/// Runtime data is supplied by <see cref="EntryEditorForm"/>, while the
/// parameterless constructor provides a safe, representative preview.
/// </summary>
public class EntryEditorFormVisualBase : ResponsiveWindow
{
    private static readonly KindOption[] KindOptions =
    [
        new(L.Get("EntryEditor.Kind.GenericTotp"), AuthenticatorKind.Totp),
        new(L.Get("EntryEditor.Kind.HotpCounter"), AuthenticatorKind.Hotp),
        new("Google Authenticator", AuthenticatorKind.Google),
        new("Microsoft Authenticator", AuthenticatorKind.Microsoft),
        new("Okta Verify", AuthenticatorKind.OktaVerify),
        new("Guild Wars 2", AuthenticatorKind.GuildWars),
        new("Steam Guard", AuthenticatorKind.Steam),
        new("Battle.net", AuthenticatorKind.BattleNet),
        new("Trion / Glyph", AuthenticatorKind.Trion)
    ];

    private static readonly AlgorithmOption[] AlgorithmOptions =
    [
        new("SHA-1", OtpAlgorithm.Sha1),
        new("SHA-256", OtpAlgorithm.Sha256),
        new("SHA-512", OtpAlgorithm.Sha512)
    ];

    private static readonly ActionOption[] ActionOptions =
    [
        new(L.Get("EntryEditor.HotkeyAction.CopyCode"), EntryHotkeyAction.Copy),
        new(L.Get("EntryEditor.HotkeyAction.TypeCode"), EntryHotkeyAction.Type),
        new(L.Get("EntryEditor.HotkeyAction.ShowNotification"), EntryHotkeyAction.Show)
    ];

    private static readonly AccentOption[] AccentOptions =
    [
        new(L.Get("EntryEditor.Color.Indigo"), "#5B53FF"),
        new(L.Get("EntryEditor.Color.Blue"), "#3B82F6"),
        new(L.Get("EntryEditor.Color.Cyan"), "#06B6D4"),
        new(L.Get("EntryEditor.Color.Green"), "#10B981"),
        new(L.Get("EntryEditor.Color.Orange"), "#F59E0B"),
        new(L.Get("EntryEditor.Color.Rose"), "#EC4899"),
        new(L.Get("EntryEditor.Color.Purple"), "#8B5CF6")
    ];

    protected readonly AuthenticatorEntry _workingCopy;
    protected readonly FlowLayoutPanel _formFlow;
    protected readonly List<ResponsiveFieldRow> _fieldRows = [];
    protected readonly AntSelect _kindSelect;
    protected readonly AntInput _nameInput;
    protected readonly AntInput _issuerInput;
    protected readonly AntInput _secretInput;
    protected readonly AntSelect _algorithmSelect;
    protected readonly AntInput _digitsInput;
    protected readonly AntInput _periodInput;
    protected readonly AntInput _counterInput;
    protected readonly AntInput _serialInput;
    protected readonly AntInput _deviceIdInput;
    protected readonly AntInput _steamDataInput;
    protected readonly AntInput _notesInput;
    protected readonly AntInput _hotkeyInput;
    protected readonly AntSelect _hotkeyActionSelect;
    protected readonly AntSelect _accentSelect;
    protected readonly AntCheckbox _favoriteCheck;
    protected readonly AntCheckbox _requireUnlockCheck;
    protected readonly AntCheckbox _copyOnRevealCheck;
    protected readonly AntCheckbox _autoRefreshCheck;
    protected readonly AntdUI.Panel _verificationPanel;
    protected readonly AntButton _verifyButton;
    protected readonly AntLabel _verificationCodeLabel;
    protected readonly AntLabel _verificationStatusLabel;
    protected readonly AntProgress _verificationProgress;
    protected readonly System.Windows.Forms.Timer _verificationTimer = new() { Interval = 500 };
    protected readonly ResponsiveFieldRow _counterRow;
    protected readonly ResponsiveFieldRow _serialRow;
    protected readonly ResponsiveFieldRow _deviceIdRow;
    protected readonly ResponsiveFieldRow _steamDataRow;
    protected readonly AntdUI.Panel _providerSection;
    protected AuthenticatorEntry? _verificationEntry;
    protected bool _verificationAttempted;
    protected string? _automaticIssuer;
    protected bool _updatingIssuer;
    protected bool _updatingLayout;
    protected bool _layoutScheduled;

    protected EntryEditorFormVisualBase()
        : this(CreateDesignerPreviewEntry(), isNew: true)
    {
    }

    protected EntryEditorFormVisualBase(AuthenticatorEntry entry, bool isNew)
    {
        var palette = ThemePaletteService.Current;
        _workingCopy = entry.Clone();
        Name = "EntryEditorForm";
        Text = isNew
            ? L.Get("EntryEditor.Title.Add")
            : L.Get("EntryEditor.Title.Edit");
        ClientSize = new Size(720, 760);
        MinimumSize = new Size(340, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        ControlBox = false;
        Font = LocalizationFonts.Create(9.5F, FontStyle.Regular);
        BackColor = palette.EditorCanvas;

        var header = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 48,
            Text = Text,
            SubText = L.Get("EntryEditor.Subtitle"),
            ShowButton = true,
            ShowIcon = false,
            DragMove = true,
            EnableDoubleClickMaximize = false,
            MaximizeBox = false,
            MinimizeBox = false,
            FullBox = false,
            DividerShow = false,
            BackColor = palette.EditorChrome,
            ForeColor = palette.Text,
            DividerColor = palette.EditorBorder
        };

        var footer = new AntdUI.Panel
        {
            Dock = DockStyle.Bottom,
            Height = 66,
            Back = palette.EditorChrome,
            BorderWidth = 1,
            BorderColor = palette.EditorBorder,
            Padding = new Padding(24, 12, 24, 12),
            Radius = 0
        };
        var footerFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        var saveButton = new AntButton
        {
            Text = isNew ? L.Get("Common.Add") : L.Get("Common.Save"),
            IconSvg = "CheckOutlined",
            Type = AntdUI.TTypeMini.Primary,
            Radius = 10,
            Size = new Size(112, 40)
        };
        var cancelButton = new AntButton
        {
            Text = L.Get("Common.Cancel"),
            Radius = 10,
            DialogResult = DialogResult.Cancel,
            Size = new Size(90, 40)
        };
        ThemePaletteService.StyleSecondaryButton(
            cancelButton,
            palette.EditorChrome,
            palette.EditorBorder);
        saveButton.Click += (_, _) => SaveAndClose();
        footerFlow.Controls.Add(saveButton);
        footerFlow.Controls.Add(cancelButton);
        footer.Controls.Add(footerFlow);

        var content = new AntdUI.Panel
        {
            Dock = DockStyle.Fill,
            Back = palette.EditorCanvas,
            BorderWidth = 1,
            BorderColor = palette.EditorBorder,
            Radius = 0
        };
        _formFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = palette.EditorCanvas,
            Padding = new Padding(28, 20, 28, 26)
        };
        _formFlow.ClientSizeChanged += (_, _) => ScheduleResponsiveLayout();
        content.Controls.Add(_formFlow);

        _kindSelect = CreateSelectForRuntime(KindOptions.Cast<object>());
        _nameInput = CreateInput(L.Get("EntryEditor.Placeholder.Name"));
        _issuerInput = CreateInput(L.Get("EntryEditor.Placeholder.Issuer"));
        _secretInput = CreateInput(L.Get("EntryEditor.Placeholder.Secret"));
        _secretInput.PasswordCopy = false;
        _algorithmSelect = CreateSelectForRuntime(AlgorithmOptions.Cast<object>());
        _digitsInput = CreateInput("6");
        _periodInput = CreateInput("30");
        _counterInput = CreateInput("0");
        _serialInput = CreateInput(L.Get("EntryEditor.Placeholder.Serial"));
        _deviceIdInput = CreateInput(L.Get("EntryEditor.Placeholder.DeviceId"));
        _steamDataInput = CreateInput(L.Get("EntryEditor.Placeholder.SteamData"));
        _steamDataInput.Multiline = true;
        _steamDataInput.WordWrap = true;
        _notesInput = CreateInput(L.Get("EntryEditor.Placeholder.Notes"));
        _notesInput.Multiline = true;
        _notesInput.WordWrap = true;
        _hotkeyInput = CreateInput(L.Get("EntryEditor.Placeholder.Hotkey"));
        _hotkeyInput.ReadOnly = true;
        _hotkeyInput.KeyDown += CaptureHotkey;
        _hotkeyInput.Click += (_, _) => _hotkeyInput.Focus();
        _hotkeyActionSelect = CreateSelectForRuntime(ActionOptions.Cast<object>());
        _accentSelect = CreateSelectForRuntime(AccentOptions.Cast<object>());

        _favoriteCheck = CreateCheck(L.Get("EntryEditor.Option.Favorite"));
        _requireUnlockCheck = CreateCheck(L.Get("EntryEditor.Option.RequireUnlock"));
        _copyOnRevealCheck = CreateCheck(L.Get("EntryEditor.Option.CopyOnReveal"));
        _autoRefreshCheck = CreateCheck(L.Get("EntryEditor.Option.AutoRefresh"));

        _verificationPanel = new AntdUI.Panel
        {
            Height = 116,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(16),
            Back = palette.EditorSurface,
            BorderWidth = 1,
            BorderColor = palette.EditorBorder,
            Radius = 12,
            AccessibleName = L.Get("EntryEditor.Accessibility.VerificationResult")
        };
        _verifyButton = new AntButton
        {
            Text = L.Get("EntryEditor.Verification.Verify"),
            IconSvg = "SafetyCertificateOutlined",
            Type = AntdUI.TTypeMini.Primary,
            Radius = 10
        };
        _verificationCodeLabel = new AntLabel
        {
            Text = L.Get("EntryEditor.Verification.Waiting"),
            Font = new Font("Cascadia Mono", 20F, FontStyle.Bold),
            ForeColor = palette.Text,
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        _verificationStatusLabel = new AntLabel
        {
            Text = L.Get("EntryEditor.Verification.Instructions"),
            Font = LocalizationFonts.Create(8.5F, FontStyle.Regular),
            ForeColor = palette.TextSecondary,
            TextMultiLine = true,
            BackColor = Color.Transparent
        };
        _verificationProgress = new AntProgress
        {
            Shape = AntdUI.TShapeProgress.Circle,
            UseSystemText = true,
            Value = 0F,
            Text = "—",
            Fill = palette.Primary,
            Back = palette.EditorChrome,
            BackColor = Color.Transparent,
            ForeColor = palette.TextSecondary,
            Animation = 120
        };
        _verificationPanel.Controls.AddRange(
        [
            _verifyButton,
            _verificationCodeLabel,
            _verificationStatusLabel,
            _verificationProgress
        ]);
        _verificationPanel.Resize += (_, _) => LayoutVerificationPanel();

        AddSection(
            L.Get("EntryEditor.Section.Basic"),
            L.Get("EntryEditor.Section.BasicDescription"));
        AddField(L.Get("EntryEditor.Field.Kind"), _kindSelect);
        AddField(L.Get("EntryEditor.Field.AccountName"), _nameInput);
        AddField(L.Get("EntryEditor.Field.Issuer"), _issuerInput);
        AddSecretField();
        AddQuickImportActions();

        AddSection(
            L.Get("EntryEditor.Section.Verification"),
            L.Get("EntryEditor.Section.VerificationDescription"));
        _formFlow.Controls.Add(_verificationPanel);

        AddSection(
            L.Get("EntryEditor.Section.OtpParameters"),
            L.Get("EntryEditor.Section.OtpParametersDescription"));
        AddField(L.Get("EntryEditor.Field.Algorithm"), _algorithmSelect);
        AddField(L.Get("EntryEditor.Field.Digits"), _digitsInput);
        AddField(L.Get("EntryEditor.Field.RefreshPeriodSeconds"), _periodInput);
        _counterRow = AddField(L.Get("EntryEditor.Field.HotpCounter"), _counterInput);

        _providerSection = AddSection(
            L.Get("EntryEditor.Section.ProviderData"),
            L.Get("EntryEditor.Section.ProviderDataDescription"));
        _serialRow = AddField(L.Get("EntryEditor.Field.Serial"), _serialInput);
        _deviceIdRow = AddField(L.Get("EntryEditor.Field.DeviceId"), _deviceIdInput);
        _steamDataRow = AddField(L.Get("EntryEditor.Field.SteamData"), _steamDataInput, 86);

        AddSection(
            L.Get("EntryEditor.Section.Preferences"),
            L.Get("EntryEditor.Section.PreferencesDescription"));
        AddField(L.Get("EntryEditor.Field.GlobalHotkey"), CreateHotkeyEditor());
        AddField(L.Get("EntryEditor.Field.HotkeyAction"), _hotkeyActionSelect);
        AddField(L.Get("EntryEditor.Field.AccentColor"), _accentSelect);
        AddOptionsPanel();
        AddField(L.Get("EntryEditor.Field.Notes"), _notesInput, 82);

        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(header);
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        _verifyButton.Click += (_, _) => VerifyAuthenticator();
        _verificationTimer.Tick += (_, _) => RefreshVerificationPreview(DateTimeOffset.UtcNow);
        _kindSelect.SelectedIndexChanged += (_, _) => UpdateProviderFields();
        LoadEntry();
        InitializeIssuerTracking();
        UpdateProviderFields();
        WireVerificationInvalidation();
        Shown += (_, _) =>
        {
            UpdateResponsiveLayout();
            _nameInput.Focus();
        };
    }

    protected AuthenticatorEntry ResultValue => _workingCopy;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _verificationTimer.Stop();
            _verificationTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    protected void LoadEntry()
    {
        _kindSelect.SelectedIndex = Math.Max(
            0,
            Array.FindIndex(KindOptions, option => option.Kind == _workingCopy.Kind));
        _nameInput.Text = _workingCopy.Name;
        _issuerInput.Text = _workingCopy.Issuer;
        _secretInput.Text = _workingCopy.Secret;
        _algorithmSelect.SelectedIndex = Math.Max(
            0,
            Array.FindIndex(AlgorithmOptions, option => option.Algorithm == _workingCopy.Algorithm));
        _digitsInput.Text = _workingCopy.Digits.ToString();
        _periodInput.Text = _workingCopy.Period.ToString();
        _counterInput.Text = _workingCopy.Counter.ToString();
        _serialInput.Text = _workingCopy.Serial ?? string.Empty;
        _deviceIdInput.Text = _workingCopy.DeviceId ?? string.Empty;
        _steamDataInput.Text = _workingCopy.ProviderData.GetValueOrDefault("steamData") ?? string.Empty;
        _notesInput.Text = _workingCopy.Notes ?? string.Empty;
        _hotkeyInput.Text = _workingCopy.Hotkey ?? string.Empty;
        _hotkeyActionSelect.SelectedIndex = Math.Max(
            0,
            Array.FindIndex(ActionOptions, option => option.Action == _workingCopy.HotkeyAction));
        _accentSelect.SelectedIndex = Math.Max(
            0,
            Array.FindIndex(
                AccentOptions,
                option => option.Color.Equals(_workingCopy.AccentColor, StringComparison.OrdinalIgnoreCase)));
        _favoriteCheck.Checked = _workingCopy.Favorite;
        _requireUnlockCheck.Checked = _workingCopy.RequireUnlock;
        _copyOnRevealCheck.Checked = _workingCopy.CopyOnReveal;
        _autoRefreshCheck.Checked = _workingCopy.AutoRefresh;
    }

    protected void SaveAndClose()
    {
        try
        {
            ResolveStructuredSecretInput();
            PopulateEntryFromInputs(_workingCopy);
            OtpAuthUriService.Validate(_workingCopy);
            _workingCopy.ModifiedAtUtc = DateTime.UtcNow;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            AntdUI.Message.error(this, exception.Message, autoClose: 4);
        }
    }

    protected void VerifyAuthenticator()
    {
        _verificationTimer.Stop();
        _verificationEntry = null;
        _verificationAttempted = true;

        try
        {
            ResolveStructuredSecretInput();
            var candidate = _workingCopy.Clone();
            PopulateEntryFromInputs(candidate);
            OtpService.ValidateForCodeGeneration(candidate);

            _verificationEntry = candidate;
            _verifyButton.Text = L.Get("EntryEditor.Verification.Retry");
            RefreshVerificationPreview(DateTimeOffset.UtcNow);
            _verificationTimer.Enabled = candidate.Kind != AuthenticatorKind.Hotp;
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            _verificationCodeLabel.Text = L.Get("EntryEditor.Verification.Failed");
            _verificationCodeLabel.ForeColor = ThemePaletteService.Current.Danger;
            _verificationStatusLabel.Text = exception.Message;
            _verificationStatusLabel.ForeColor = ThemePaletteService.Current.Danger;
            _verificationProgress.Value = 0F;
            _verificationProgress.Text = "!";
            _verificationProgress.Fill = ThemePaletteService.Current.Danger;
        }
    }

    protected void RefreshVerificationPreview(DateTimeOffset instant)
    {
        if (_verificationEntry is null)
        {
            return;
        }

        try
        {
            var code = OtpService.GetCode(_verificationEntry, instant);
            _verificationCodeLabel.Text = OtpService.FormatCode(code);
            _verificationCodeLabel.ForeColor = ThemePaletteService.Current.Text;
            _verificationStatusLabel.ForeColor = ThemePaletteService.Current.Success;
            _verificationProgress.Fill = ThemePaletteService.Current.Success;

            if (_verificationEntry.Kind == AuthenticatorKind.Hotp)
            {
                _verificationProgress.Value = 1F;
                _verificationProgress.Text = "#";
                _verificationStatusLabel.Text = L.Format(
                    "EntryEditor.Verification.CounterValid",
                    _verificationEntry.Counter);
            }
            else
            {
                var remaining = OtpService.GetRemainingSeconds(_verificationEntry, instant);
                _verificationProgress.Value =
                    (float)OtpService.GetRemainingFraction(_verificationEntry, instant);
                _verificationProgress.Text = remaining.ToString();
                _verificationStatusLabel.Text = L.Format(
                    "EntryEditor.Verification.TimeBasedValid",
                    remaining);
            }
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            _verificationTimer.Stop();
            _verificationEntry = null;
            _verificationCodeLabel.Text = L.Get("EntryEditor.Verification.Failed");
            _verificationCodeLabel.ForeColor = ThemePaletteService.Current.Danger;
            _verificationStatusLabel.Text = exception.Message;
            _verificationStatusLabel.ForeColor = ThemePaletteService.Current.Danger;
            _verificationProgress.Value = 0F;
            _verificationProgress.Text = "!";
            _verificationProgress.Fill = ThemePaletteService.Current.Danger;
        }
    }

    protected void PopulateEntryFromInputs(AuthenticatorEntry target)
    {
        var kind = KindOptions[
            Math.Clamp(_kindSelect.SelectedIndex, 0, KindOptions.Length - 1)].Kind;
        var algorithm = AlgorithmOptions[
            Math.Clamp(_algorithmSelect.SelectedIndex, 0, AlgorithmOptions.Length - 1)].Algorithm;
        var action = ActionOptions[
            Math.Clamp(_hotkeyActionSelect.SelectedIndex, 0, ActionOptions.Length - 1)].Action;
        var accent = AccentOptions[
            Math.Clamp(_accentSelect.SelectedIndex, 0, AccentOptions.Length - 1)].Color;

        target.Name = _nameInput.Text.Trim();
        target.Issuer = _issuerInput.Text.Trim();
        target.Secret = Base32Encoding.Normalize(_secretInput.Text.Trim());
        target.Kind = kind;
        target.Algorithm = algorithm;
        target.Digits = ParseInt(
            _digitsInput.Text,
            L.Get("EntryEditor.Field.Digits"),
            4,
            10);
        target.Period = kind == AuthenticatorKind.Hotp
            ? 30
            : ParseInt(
                _periodInput.Text,
                L.Get("EntryEditor.Field.RefreshPeriodSeconds"),
                1,
                3_600);
        target.Counter = kind == AuthenticatorKind.Hotp
            ? ParseLong(
                _counterInput.Text,
                L.Get("EntryEditor.Field.HotpCounter"),
                0)
            : TryParseNonNegativeLong(_counterInput.Text, target.Counter);
        target.Serial = kind is
            AuthenticatorKind.Steam or
            AuthenticatorKind.BattleNet or
            AuthenticatorKind.Trion
                ? NullIfEmpty(_serialInput.Text)
                : null;
        target.DeviceId = kind is AuthenticatorKind.Steam or AuthenticatorKind.Trion
            ? NullIfEmpty(_deviceIdInput.Text)
            : null;
        target.Notes = NullIfEmpty(_notesInput.Text);
        target.Hotkey = NullIfEmpty(_hotkeyInput.Text);
        target.HotkeyAction = action;
        target.AccentColor = accent;
        target.Favorite = _favoriteCheck.Checked;
        target.RequireUnlock = _requireUnlockCheck.Checked;
        target.CopyOnReveal = _copyOnRevealCheck.Checked;
        target.AutoRefresh = kind != AuthenticatorKind.Hotp && _autoRefreshCheck.Checked;

        if (kind != AuthenticatorKind.Steam)
        {
            target.ProviderData.Remove("steamData");
            target.ProviderData.Remove("steamSessionData");
        }
        else if (string.IsNullOrWhiteSpace(_steamDataInput.Text))
        {
            target.ProviderData.Remove("steamData");
        }
        else
        {
            target.ProviderData["steamData"] = _steamDataInput.Text.Trim();
        }

        target.ApplyProviderDefaults();
    }

    protected void ResolveStructuredSecretInput()
    {
        var secretText = _secretInput.Text.Trim();
        if (!secretText.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase) &&
            !secretText.StartsWith("otpauth-migration://", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var entries = OtpAuthUriService.Parse(secretText);
        ApplyImportedEntry(entries[0], showSuccess: false);
        WarnAboutAdditionalImportedEntries(entries.Count);
    }

    protected void WireVerificationInvalidation()
    {
        _secretInput.TextChanged += (_, _) => InvalidateVerificationPreview();
        _digitsInput.TextChanged += (_, _) => InvalidateVerificationPreview();
        _periodInput.TextChanged += (_, _) => InvalidateVerificationPreview();
        _counterInput.TextChanged += (_, _) => InvalidateVerificationPreview();
        _kindSelect.SelectedIndexChanged += (_, _) => InvalidateVerificationPreview();
        _algorithmSelect.SelectedIndexChanged += (_, _) => InvalidateVerificationPreview();
    }

    protected void InvalidateVerificationPreview()
    {
        if (_verificationEntry is null && !_verificationAttempted)
        {
            return;
        }

        _verificationTimer.Stop();
        _verificationEntry = null;
        _verificationAttempted = false;
        _verifyButton.Text = L.Get("EntryEditor.Verification.Verify");
        _verificationCodeLabel.Text = L.Get("EntryEditor.Verification.ParametersChanged");
        _verificationCodeLabel.ForeColor = ThemePaletteService.Current.Text;
        _verificationStatusLabel.Text =
            L.Get("EntryEditor.Verification.ParametersChangedDescription");
        _verificationStatusLabel.ForeColor = ThemePaletteService.Current.TextSecondary;
        _verificationProgress.Value = 0F;
        _verificationProgress.Text = "—";
        _verificationProgress.Fill = ThemePaletteService.Current.Primary;
    }

    protected void AddSecretField()
    {
        var panel = new Panel { BackColor = Color.Transparent };
        var toggleButton = new AntButton
        {
            Dock = DockStyle.Right,
            Width = 46,
            IconSvg = "EyeOutlined",
            Ghost = true,
            Radius = 9
        };
        _secretInput.Dock = DockStyle.Fill;
        _secretInput.UseSystemPasswordChar = true;
        toggleButton.Click += (_, _) =>
        {
            _secretInput.UseSystemPasswordChar = !_secretInput.UseSystemPasswordChar;
            toggleButton.IconSvg = _secretInput.UseSystemPasswordChar
                ? "EyeOutlined"
                : "EyeInvisibleOutlined";
        };
        panel.Controls.Add(_secretInput);
        panel.Controls.Add(toggleButton);
        AddField(L.Get("EntryEditor.Field.SharedSecret"), panel);
    }

    protected void AddQuickImportActions()
    {
        var actions = new ResponsiveWrapPanel
        {
            BackColor = Color.Transparent,
            MinimumItemWidth = 240,
            ItemHeight = 38,
            MaximumColumns = 2,
            HorizontalSpacing = 10,
            VerticalSpacing = 8,
            Margin = new Padding(0, 0, 0, 14)
        };
        var pasteButton = new AntButton
        {
            Text = L.Get("EntryEditor.Import.FromClipboard"),
            IconSvg = "SnippetsOutlined",
            Radius = 9
        };
        var imageButton = new AntButton
        {
            Text = L.Get("EntryEditor.Import.FromQrImage"),
            IconSvg = "QrcodeOutlined",
            Radius = 9
        };
        ThemePaletteService.StyleSecondaryButton(
            pasteButton,
            ThemePaletteService.Current.EditorChrome,
            ThemePaletteService.Current.EditorBorder);
        ThemePaletteService.StyleSecondaryButton(
            imageButton,
            ThemePaletteService.Current.EditorChrome,
            ThemePaletteService.Current.EditorBorder);
        pasteButton.Click += (_, _) => ImportFromClipboard();
        imageButton.Click += (_, _) => ImportFromQrImage();
        actions.Controls.Add(pasteButton);
        actions.Controls.Add(imageButton);
        _formFlow.Controls.Add(actions);
    }

    protected Control CreateHotkeyEditor()
    {
        var panel = new Panel { BackColor = Color.Transparent };
        var clearButton = new AntButton
        {
            Dock = DockStyle.Right,
            Width = 46,
            IconSvg = "CloseOutlined",
            Ghost = true,
            Radius = 9
        };
        clearButton.Click += (_, _) => _hotkeyInput.Text = string.Empty;
        _hotkeyInput.Dock = DockStyle.Fill;
        panel.Controls.Add(_hotkeyInput);
        panel.Controls.Add(clearButton);
        return panel;
    }

    protected void AddOptionsPanel()
    {
        var options = new ResponsiveWrapPanel
        {
            BackColor = ThemePaletteService.Current.EditorSurface,
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 4, 0, 14),
            MinimumItemWidth = 220,
            ItemHeight = 30,
            MaximumColumns = 2,
            HorizontalSpacing = 12,
            VerticalSpacing = 6
        };
        options.Controls.AddRange(
        [
            _favoriteCheck,
            _requireUnlockCheck,
            _copyOnRevealCheck,
            _autoRefreshCheck
        ]);
        _formFlow.Controls.Add(options);
    }

    protected ResponsiveFieldRow AddField(
        string label,
        Control editor,
        int editorHeight = 44)
    {
        var row = new ResponsiveFieldRow(label, editor, editorHeight);
        _fieldRows.Add(row);
        _formFlow.Controls.Add(row);
        return row;
    }

    protected AntdUI.Panel AddSection(string title, string subtitle)
    {
        var palette = ThemePaletteService.Current;
        var section = new AntdUI.Panel
        {
            Height = 66,
            Margin = new Padding(0, 14, 0, 10),
            Padding = new Padding(14, 8, 14, 8),
            Back = palette.EditorSurface,
            BorderWidth = 1,
            BorderColor = palette.EditorBorder,
            Radius = 12
        };
        var subtitleLabel = new AntLabel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 0, 0, 0),
            Text = subtitle,
            Font = LocalizationFonts.Create(8.5F, FontStyle.Regular),
            ForeColor = palette.TextSecondary,
            AutoEllipsis = true
        };
        var titleLabel = new AntLabel
        {
            Dock = DockStyle.Top,
            Height = 26,
            Text = title,
            PrefixSvg = "RightOutlined",
            PrefixColor = palette.Primary,
            Font = LocalizationFonts.Create(12F, FontStyle.Bold),
            ForeColor = palette.Text
        };
        section.Controls.Add(subtitleLabel);
        section.Controls.Add(titleLabel);
        _formFlow.Controls.Add(section);
        return section;
    }

    protected void UpdateProviderFields()
    {
        var kind = KindOptions[Math.Clamp(_kindSelect.SelectedIndex, 0, KindOptions.Length - 1)].Kind;
        var hasSerial = kind is
            AuthenticatorKind.Steam or
            AuthenticatorKind.BattleNet or
            AuthenticatorKind.Trion;
        var hasDeviceId = kind is AuthenticatorKind.Steam or AuthenticatorKind.Trion;
        var hasSteamData = kind == AuthenticatorKind.Steam;
        _providerSection.Visible = hasSerial;
        _counterRow.Visible = kind == AuthenticatorKind.Hotp;
        _serialRow.Visible = hasSerial;
        _deviceIdRow.Visible = hasDeviceId;
        _steamDataRow.Visible = hasSteamData;
        var hasFixedOtpParameters = kind is
            AuthenticatorKind.Steam or
            AuthenticatorKind.BattleNet or
            AuthenticatorKind.Trion;
        _algorithmSelect.Enabled = !hasFixedOtpParameters;
        _digitsInput.Enabled = !hasFixedOtpParameters;
        _periodInput.Enabled = kind != AuthenticatorKind.Hotp && !hasFixedOtpParameters;
        _autoRefreshCheck.Enabled = kind != AuthenticatorKind.Hotp;

        var defaults = new AuthenticatorEntry { Kind = kind };
        defaults.ApplyProviderDefaults();
        if (string.IsNullOrWhiteSpace(_issuerInput.Text) ||
            _automaticIssuer is not null &&
            _issuerInput.Text.Trim().Equals(_automaticIssuer, StringComparison.OrdinalIgnoreCase))
        {
            SetIssuer(defaults.Issuer, automatic: true);
        }

        if (kind is AuthenticatorKind.Steam or AuthenticatorKind.BattleNet or AuthenticatorKind.Trion)
        {
            _digitsInput.Text = defaults.Digits.ToString();
            _periodInput.Text = defaults.Period.ToString();
            _algorithmSelect.SelectedIndex = 0;
        }

        UpdateResponsiveLayout();
    }

    protected void InitializeIssuerTracking()
    {
        var kind = KindOptions[
            Math.Clamp(_kindSelect.SelectedIndex, 0, KindOptions.Length - 1)].Kind;
        _automaticIssuer = IsDefaultIssuer(kind, _issuerInput.Text)
            ? _issuerInput.Text.Trim()
            : null;
        _issuerInput.TextChanged += (_, _) =>
        {
            if (!_updatingIssuer)
            {
                _automaticIssuer = null;
            }
        };
    }

    protected void SetIssuer(string value, bool automatic)
    {
        _updatingIssuer = true;
        try
        {
            _issuerInput.Text = value;
            _automaticIssuer = automatic ? value.Trim() : null;
        }
        finally
        {
            _updatingIssuer = false;
        }
    }

    private static bool IsDefaultIssuer(AuthenticatorKind kind, string value)
    {
        var defaults = new AuthenticatorEntry { Kind = kind };
        defaults.ApplyProviderDefaults();
        return value.Trim().Equals(defaults.Issuer, StringComparison.OrdinalIgnoreCase);
    }

    protected void ImportFromClipboard()
    {
        try
        {
            if (Clipboard.ContainsImage() && Clipboard.GetImage() is Image image)
            {
                using var bitmap = new Bitmap(image);
                ApplyImportedText(QrCodeService.Decode(bitmap));
                return;
            }

            var text = Clipboard.GetText().Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException(
                    L.Get("EntryEditor.Import.Error.EmptyClipboard"));
            }

            ApplyImportedText(text);
        }
        catch (Exception exception) when (
            exception is FormatException or ExternalException or ArgumentException)
        {
            AntdUI.Message.error(this, exception.Message, autoClose: 4);
        }
    }

    protected void ImportFromQrImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = L.Get("EntryEditor.Import.FileDialog.Title"),
            Filter = L.Get("EntryEditor.Import.FileDialog.Filter")
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            ApplyImportedText(QrCodeService.DecodeFile(dialog.FileName));
        }
        catch (Exception exception) when (
            exception is FormatException or IOException or ArgumentException)
        {
            AntdUI.Message.error(this, exception.Message, autoClose: 4);
        }
    }

    protected void ApplyImportedText(string value)
    {
        if (value.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("otpauth-migration://", StringComparison.OrdinalIgnoreCase))
        {
            var entries = OtpAuthUriService.Parse(value);
            ApplyImportedEntry(entries[0]);
            WarnAboutAdditionalImportedEntries(entries.Count);
            return;
        }

        if (!Base32Encoding.TryDecode(value, out _))
        {
            throw new FormatException(
                L.Get("EntryEditor.Import.Error.InvalidContent"));
        }

        _secretInput.Text = Base32Encoding.Normalize(value);
        AntdUI.Message.success(
            this,
            L.Get("EntryEditor.Import.Notification.SecretLoaded"),
            autoClose: 2);
    }

    protected void ApplyImportedEntry(
        AuthenticatorEntry imported,
        bool showSuccess = true)
    {
        _workingCopy.Kind = imported.Kind;
        _workingCopy.Algorithm = imported.Algorithm;
        _workingCopy.Digits = imported.Digits;
        _workingCopy.Period = imported.Period;
        _workingCopy.Counter = imported.Counter;
        _workingCopy.TimeOffsetSeconds = imported.TimeOffsetSeconds;
        _workingCopy.Secret = imported.Secret;
        _workingCopy.Serial = imported.Serial;
        _workingCopy.DeviceId = imported.DeviceId;
        _workingCopy.ProviderData = new Dictionary<string, string>(
            imported.ProviderData,
            StringComparer.OrdinalIgnoreCase);

        _kindSelect.SelectedIndex = Math.Max(
            0,
            Array.FindIndex(KindOptions, option => option.Kind == imported.Kind));
        _nameInput.Text = imported.Name;
        SetIssuer(imported.Issuer, IsDefaultIssuer(imported.Kind, imported.Issuer));
        _secretInput.Text = imported.Secret;
        _algorithmSelect.SelectedIndex = Math.Max(
            0,
            Array.FindIndex(AlgorithmOptions, option => option.Algorithm == imported.Algorithm));
        _digitsInput.Text = imported.Digits.ToString();
        _periodInput.Text = imported.Period.ToString();
        _counterInput.Text = imported.Counter.ToString();
        _serialInput.Text = imported.Serial ?? string.Empty;
        _deviceIdInput.Text = imported.DeviceId ?? string.Empty;
        _steamDataInput.Text = imported.ProviderData.GetValueOrDefault("steamData") ??
                               imported.ProviderData.GetValueOrDefault("legacyData") ??
                               string.Empty;
        UpdateProviderFields();
        if (showSuccess)
        {
            AntdUI.Message.success(
                this,
                L.Get("EntryEditor.Import.Notification.ParametersLoaded"),
                autoClose: 3);
        }
    }

    protected void WarnAboutAdditionalImportedEntries(int count)
    {
        if (count <= 1)
        {
            return;
        }

        AntdUI.Message.warn(
            this,
            L.Format("EntryEditor.Import.Warning.MultipleAccounts", count),
            autoClose: 6);
    }

    protected void CaptureHotkey(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.KeyCode is Keys.Back or Keys.Delete or Keys.Escape)
        {
            _hotkeyInput.Text = string.Empty;
        }
        else
        {
            var modifiers = eventArgs.Modifiers;
            if (modifiers == Keys.None ||
                eventArgs.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            {
                return;
            }

            _hotkeyInput.Text = HotkeyService.Format(modifiers | eventArgs.KeyCode);
        }

        eventArgs.Handled = true;
        eventArgs.SuppressKeyPress = true;
    }

    protected void ScheduleResponsiveLayout()
    {
        if (_layoutScheduled || IsDisposed || !IsHandleCreated)
        {
            return;
        }

        _layoutScheduled = true;
        BeginInvoke((MethodInvoker)(() =>
        {
            _layoutScheduled = false;
            if (!IsDisposed)
            {
                UpdateResponsiveLayout();
            }
        }));
    }

    protected void UpdateResponsiveLayout()
    {
        if (_updatingLayout || _formFlow.IsDisposed)
        {
            return;
        }

        _updatingLayout = true;
        _formFlow.SuspendLayout();
        try
        {
            var availableWidth = Math.Max(
                1,
                _formFlow.ClientSize.Width -
                _formFlow.Padding.Horizontal -
                ScaleLogical(4));
            var stacked = ToLogical(availableWidth) < 520;
            foreach (Control control in _formFlow.Controls)
            {
                control.Width = availableWidth;
            }

            foreach (var row in _fieldRows)
            {
                row.Stacked = stacked;
            }

            LayoutVerificationPanel();
        }
        finally
        {
            _formFlow.ResumeLayout(performLayout: true);
            _updatingLayout = false;
        }
    }

    protected void LayoutVerificationPanel()
    {
        if (_verificationPanel is null ||
            _verifyButton is null ||
            _verificationCodeLabel is null ||
            _verificationStatusLabel is null ||
            _verificationProgress is null)
        {
            return;
        }

        var innerWidth = Math.Max(
            1,
            _verificationPanel.ClientSize.Width - ScaleLogical(32));
        var narrow = ToLogical(_verificationPanel.ClientSize.Width) < 500;
        var progressSize = ScaleLogical(52);

        if (narrow)
        {
            if (_verificationPanel.Height != ScaleLogical(174))
            {
                _verificationPanel.Height = ScaleLogical(174);
            }

            _verifyButton.SetBounds(
                ScaleLogical(16),
                ScaleLogical(14),
                innerWidth,
                ScaleLogical(42));
            _verificationProgress.SetBounds(
                _verificationPanel.ClientSize.Width - ScaleLogical(16) - progressSize,
                ScaleLogical(110),
                progressSize,
                progressSize);
            _verificationCodeLabel.SetBounds(
                ScaleLogical(16),
                ScaleLogical(68),
                innerWidth,
                ScaleLogical(42));
            var statusWidth = Math.Max(
                ScaleLogical(100),
                _verificationProgress.Left -
                ScaleLogical(16) -
                ScaleLogical(10));
            _verificationStatusLabel.SetBounds(
                ScaleLogical(16),
                ScaleLogical(110),
                statusWidth,
                ScaleLogical(48));
        }
        else
        {
            if (_verificationPanel.Height != ScaleLogical(116))
            {
                _verificationPanel.Height = ScaleLogical(116);
            }

            var buttonWidth = ScaleLogical(184);
            _verifyButton.SetBounds(
                ScaleLogical(16),
                ScaleLogical(36),
                buttonWidth,
                ScaleLogical(42));
            _verificationProgress.SetBounds(
                _verificationPanel.ClientSize.Width - ScaleLogical(16) - progressSize,
                ScaleLogical(31),
                progressSize,
                progressSize);
            var resultLeft = ScaleLogical(216);
            var resultWidth = Math.Max(
                ScaleLogical(120),
                _verificationProgress.Left - resultLeft - ScaleLogical(12));
            _verificationCodeLabel.SetBounds(
                resultLeft,
                ScaleLogical(15),
                resultWidth,
                ScaleLogical(44));
            _verificationStatusLabel.SetBounds(
                resultLeft,
                ScaleLogical(58),
                resultWidth,
                ScaleLogical(38));
        }
    }

    protected static AntSelect CreateSelectForRuntime(
        IEnumerable<object> items)
    {
        var palette = ThemePaletteService.Current;
        var select = new AntSelect
        {
            Radius = 10,
            PlaceholderText = L.Get("Common.SelectPlaceholder"),
            BackColor = palette.EditorChrome,
            ForeColor = palette.Text,
            PlaceholderColor = palette.TextSecondary,
            BorderColor = palette.EditorBorder,
            BorderHover = palette.Primary,
            BorderActive = palette.Primary,
            ListAutoWidth = true,
            MaxCount = 10,
            WheelModifyEnabled = false
        };
        select.Items.AddRange(items.ToArray());
        return select;
    }

    private static AntInput CreateInput(string placeholder)
    {
        var palette = ThemePaletteService.Current;
        return new AntInput
        {
            PlaceholderText = placeholder,
            Radius = 10,
            AllowClear = true,
            BackColor = palette.EditorChrome,
            ForeColor = palette.Text,
            PlaceholderColor = palette.TextSecondary,
            BorderColor = palette.EditorBorder,
            BorderHover = palette.Primary,
            BorderActive = palette.Primary,
            MaxLength = 16_384
        };
    }

    private static AntCheckbox CreateCheck(string text)
    {
        return new AntCheckbox
        {
            Text = text,
            Size = new Size(154, 30),
            ForeColor = ThemePaletteService.Current.Text
        };
    }

    private static int ParseInt(string value, string field, int minimum, int maximum)
    {
        if (!int.TryParse(value, out var parsed) || parsed < minimum || parsed > maximum)
        {
            throw new FormatException(
                L.Format(
                    "EntryEditor.Validation.Range",
                    field,
                    minimum,
                    maximum));
        }

        return parsed;
    }

    private static long ParseLong(string value, string field, long minimum)
    {
        if (!long.TryParse(value, out var parsed) || parsed < minimum)
        {
            throw new FormatException(
                L.Format(
                    "EntryEditor.Validation.Minimum",
                    field,
                    minimum));
        }

        return parsed;
    }

    private static long TryParseNonNegativeLong(string value, long fallback)
    {
        return long.TryParse(value, out var parsed) && parsed >= 0
            ? parsed
            : Math.Max(0, fallback);
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static AuthenticatorEntry CreateDesignerPreviewEntry()
    {
        return new AuthenticatorEntry
        {
            Name = "designer@example.com",
            Issuer = "Example",
            Secret = "JBSWY3DPEHPK3PXP",
            Kind = AuthenticatorKind.Totp,
            Algorithm = OtpAlgorithm.Sha1,
            Digits = 6,
            Period = 30,
            AccentColor = "#5B53FF",
            AutoRefresh = true
        };
    }

    private sealed record KindOption(string Text, AuthenticatorKind Kind)
    {
        public override string ToString() => Text;
    }

    private sealed record AlgorithmOption(string Text, OtpAlgorithm Algorithm)
    {
        public override string ToString() => Text;
    }

    private sealed record ActionOption(string Text, EntryHotkeyAction Action)
    {
        public override string ToString() => Text;
    }

    private sealed record AccentOption(string Text, string Color)
    {
        public override string ToString() => Text;
    }
}
