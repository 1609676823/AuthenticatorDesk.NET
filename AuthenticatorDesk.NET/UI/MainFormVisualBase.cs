using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.UI;

/// <summary>
///     Compiled, side-effect-free visual base used by the out-of-process
///     WinForms designer. MainForm itself requires runtime services, so the
///     designer cannot execute its constructor to discover these controls.
/// </summary>
public class MainFormVisualBase : ResponsiveWindow
{
    private bool _runtimeLayoutActive;

    protected AntdUI.PageHeader _titleBar = null!;
    protected AntdUI.Checkbox _topMostCheckBox = null!;
    protected AntdUI.Button _themeButton = null!;
    protected AntdUI.Button _aboutButton = null!;
    protected AntdUI.Button _lockButton = null!;
    protected Panel _shell = null!;
    protected AntdUI.Panel _sidebar = null!;
    protected AntdUI.Label _brandTitle = null!;
    protected AntdUI.Label _brandSubtitle = null!;
    protected AntdUI.Label _vaultStatus = null!;
    protected AntdUI.Panel _vaultStatusPanel = null!;
    protected AntdUI.Button _navVault = null!;
    protected AntdUI.Button _navImport = null!;
    protected AntdUI.Button _navExport = null!;
    protected AntdUI.Button _navSettings = null!;
    protected Panel _contentHost = null!;

    protected Panel _dashboard = null!;
    protected AntdUI.Panel _dashboardHero = null!;
    protected AntdUI.Label _heroEyebrow = null!;
    protected AntdUI.Label _heroTitle = null!;
    protected AntdUI.Label _heroSubtitle = null!;
    protected AntdUI.Input _searchInput = null!;
    protected AntdUI.Button _addButton = null!;
    protected AntdUI.Button _importButton = null!;
    protected Panel _filterBar = null!;
    protected AntdUI.Label _resultLabel = null!;
    protected AntdUI.Button _toggleAllCodesButton = null!;
    protected AntdUI.Button _filterAllButton = null!;
    protected AntdUI.Button _filterFavoriteButton = null!;
    protected AntdUI.Button _filterTimeButton = null!;
    protected AntdUI.Button _filterCounterButton = null!;
    protected FlowLayoutPanel _cardFlow = null!;
    protected Panel _emptyState = null!;
    protected AntdUI.Label _emptyIcon = null!;
    protected AntdUI.Label _emptyTitle = null!;
    protected AntdUI.Label _emptyText = null!;
    protected AntdUI.Button _emptyAddButton = null!;

    protected Panel _settingsView = null!;
    protected FlowLayoutPanel _settingsFlow = null!;

    protected Panel _lockView = null!;
    protected AntdUI.Panel _lockCard = null!;
    protected AntdUI.Label _lockGlyph = null!;
    protected AntdUI.Label _lockTitle = null!;
    protected AntdUI.Label _lockDescription = null!;
    protected AntdUI.Button _unlockButton = null!;

    protected MainFormVisualBase()
    {
        InitializeVisualTree();
        ApplyDesignerPreviewAppearance();
        LayoutDesignerPreview();
        Resize += (_, _) =>
        {
            if (!_runtimeLayoutActive)
            {
                LayoutDesignerPreview();
            }
        };
    }

    /// <summary>
    ///     Stops the static designer preview layout once MainForm's responsive
    ///     runtime layout has taken ownership.
    /// </summary>
    protected void ActivateRuntimeLayout()
    {
        _runtimeLayoutActive = true;
    }

    private void InitializeVisualTree()
    {
        SuspendLayout();

        Text = "AuthenticatorDesk";
        Name = "MainForm";
        StartPosition = FormStartPosition.Manual;
        Size = new Size(
            AppSettings.DefaultWindowWidth,
            AppSettings.DefaultWindowHeight);
        MinimumSize = new Size(360, 540);
        Font = LocalizationFonts.Create(9.5F);
        FormBorderStyle = FormBorderStyle.None;
        ControlBox = false;
        DoubleBuffered = true;

        _titleBar = new AntdUI.PageHeader
        {
            Name = "titleBar",
            Dock = DockStyle.Top,
            Height = 56,
            Text = "AuthenticatorDesk",
            SubText = L.Get("Main.Header.Subtitle"),
            ShowButton = true,
            ShowIcon = true,
            DragMove = true,
            EnableDoubleClickMaximize = true,
            DividerShow = true
        };
        _topMostCheckBox = new AntdUI.Checkbox
        {
            Name = "topMostCheckBox",
            Text = L.Get("Main.AlwaysOnTop.Label"),
            Size = new Size(132, 36),
            TabStop = true,
            AccessibleName = L.Get("Main.AlwaysOnTop.Label"),
            AccessibleDescription = L.Get("Main.AlwaysOnTop.Description"),
            AccessibleRole = AccessibleRole.CheckButton
        };
        _themeButton = CreateHeaderButton(
            "themeButton",
            "SunOutlined",
            L.Get("Main.Theme.ToggleTooltip"));
        _themeButton.ToggleIconSvg = "MoonOutlined";
        _themeButton.IconToggleAnimation = 0;
        _aboutButton = CreateHeaderButton(
            "aboutButton",
            "InfoCircleOutlined",
            L.Get("About.Tooltip"));
        _lockButton = CreateHeaderButton(
            "lockButton",
            "LockOutlined",
            L.Get("Main.Vault.LockTooltip"));
        _titleBar.Controls.AddRange(
        [
            _topMostCheckBox,
            _themeButton,
            _aboutButton,
            _lockButton
        ]);

        _shell = new Panel
        {
            Name = "shell",
            Dock = DockStyle.Fill
        };
        _sidebar = new AntdUI.Panel
        {
            Name = "sidebar",
            Radius = 0,
            Shadow = 0,
            BorderWidth = 1
        };
        _brandTitle = new AntdUI.Label
        {
            Name = "brandTitle",
            Text = "AuthenticatorDesk",
            Font = LocalizationFonts.Create(12F, FontStyle.Bold),
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        _brandSubtitle = new AntdUI.Label
        {
            Name = "brandSubtitle",
            Text = L.Get("Main.Brand.Subtitle"),
            Font = LocalizationFonts.Create(8.5F),
            BackColor = Color.Transparent
        };
        _vaultStatus = new AntdUI.Label
        {
            Name = "vaultStatus",
            Text = L.Get("Vault.Status.Portable"),
            Font = LocalizationFonts.Create(8.5F),
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        _vaultStatusPanel = new AntdUI.Panel
        {
            Name = "vaultStatusPanel",
            Radius = 11,
            Shadow = 0,
            BorderWidth = 1
        };
        _vaultStatusPanel.Controls.Add(_vaultStatus);
        _navVault = CreateNavigationButton(
            "navVault",
            "SafetyCertificateOutlined",
            L.Get("Main.Nav.MyAuthenticators"));
        _navImport = CreateNavigationButton(
            "navImport",
            "ImportOutlined",
            L.Get("Main.Nav.ImportAndMigration"));
        _navExport = CreateNavigationButton(
            "navExport",
            "ExportOutlined",
            L.Get("Main.Nav.ExportAndBackup"));
        _navSettings = CreateNavigationButton(
            "navSettings",
            "SettingOutlined",
            L.Get("Common.Settings"));
        _sidebar.Controls.AddRange(
        [
            _brandTitle,
            _brandSubtitle,
            _navVault,
            _navImport,
            _navExport,
            _navSettings,
            _vaultStatusPanel
        ]);

        _contentHost = new Panel { Name = "contentHost" };
        _dashboard = new Panel { Name = "dashboard" };
        _dashboardHero = new AntdUI.Panel
        {
            Name = "dashboardHero",
            Radius = 20,
            Shadow = 8,
            ShadowOffsetY = 3,
            ShadowOpacity = 0.07F
        };
        _heroEyebrow = new AntdUI.Label
        {
            Name = "heroEyebrow",
            Text = L.Get("Dashboard.Eyebrow"),
            Font = LocalizationFonts.Create(8F, FontStyle.Bold),
            BackColor = Color.Transparent
        };
        _heroTitle = new AntdUI.Label
        {
            Name = "heroTitle",
            Text = L.Get("Dashboard.Title"),
            Font = LocalizationFonts.Create(21F, FontStyle.Bold),
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        _heroSubtitle = new AntdUI.Label
        {
            Name = "heroSubtitle",
            Text = L.Get("Dashboard.GetStarted"),
            Font = LocalizationFonts.Create(9F),
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        _searchInput = new AntdUI.Input
        {
            Name = "searchInput",
            PlaceholderText = L.Get("Dashboard.SearchPlaceholder"),
            PrefixSvg = "SearchOutlined",
            AllowClear = true,
            Radius = 11
        };
        _addButton = new AntdUI.Button
        {
            Name = "addButton",
            Text = L.Get("Dashboard.AddAuthenticator"),
            IconSvg = "PlusOutlined",
            Type = AntdUI.TTypeMini.Primary,
            Radius = 11
        };
        _importButton = new AntdUI.Button
        {
            Name = "importButton",
            Text = L.Get("Common.Import"),
            IconSvg = "ImportOutlined",
            Radius = 11
        };
        _dashboardHero.Controls.AddRange(
        [
            _heroEyebrow,
            _heroTitle,
            _heroSubtitle,
            _addButton,
            _importButton
        ]);

        _filterBar = new Panel
        {
            Name = "filterBar",
            BackColor = Color.Transparent
        };
        _resultLabel = new AntdUI.Label
        {
            Name = "resultLabel",
            Text = L.Format("Dashboard.ResultCount", 0),
            Font = LocalizationFonts.Create(9F, FontStyle.Bold),
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        _toggleAllCodesButton = new AntdUI.Button
        {
            Name = "toggleAllCodesButton",
            Text = L.Get("Dashboard.HideAllCodes"),
            IconSvg = "EyeInvisibleOutlined",
            Radius = 9,
            AccessibleName = L.Get("Dashboard.HideAllCodes")
        };
        _filterAllButton = CreateFilterButton(
            "filterAllButton",
            L.Get("Dashboard.Filter.All"));
        _filterFavoriteButton = CreateFilterButton(
            "filterFavoriteButton",
            L.Get("Dashboard.Filter.Favorites"));
        _filterTimeButton = CreateFilterButton(
            "filterTimeButton",
            L.Get("Dashboard.Filter.TimeBased"));
        _filterCounterButton = CreateFilterButton(
            "filterCounterButton",
            L.Get("Dashboard.Filter.CounterBased"));
        _filterBar.Controls.AddRange(
        [
            _searchInput,
            _resultLabel,
            _toggleAllCodesButton,
            _filterAllButton,
            _filterFavoriteButton,
            _filterTimeButton,
            _filterCounterButton
        ]);

        _cardFlow = new FlowLayoutPanel
        {
            Name = "cardFlow",
            AutoScroll = true,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 6, 12, 18)
        };
        _emptyState = new Panel
        {
            Name = "emptyState",
            BackColor = Color.Transparent,
            Visible = true
        };
        _emptyIcon = new AntdUI.Label
        {
            Name = "emptyIcon",
            Text = "◎",
            Font = new Font("Segoe UI Symbol", 38F),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        _emptyTitle = new AntdUI.Label
        {
            Name = "emptyTitle",
            Text = L.Get("Dashboard.Empty.Title"),
            Font = LocalizationFonts.Create(14F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        _emptyText = new AntdUI.Label
        {
            Name = "emptyText",
            Text = L.Get("Dashboard.Empty.Description"),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        _emptyAddButton = new AntdUI.Button
        {
            Name = "emptyAddButton",
            Text = L.Get("Dashboard.Empty.AddFirst"),
            IconSvg = "PlusOutlined",
            Type = AntdUI.TTypeMini.Primary,
            Radius = 11
        };
        _emptyState.Controls.AddRange(
        [
            _emptyIcon,
            _emptyTitle,
            _emptyText,
            _emptyAddButton
        ]);
        _dashboard.Controls.AddRange(
        [
            _cardFlow,
            _emptyState,
            _filterBar,
            _dashboardHero
        ]);

        _settingsView = new Panel
        {
            Name = "settingsView",
            Visible = false
        };
        _settingsFlow = new FlowLayoutPanel
        {
            Name = "settingsFlow",
            AutoScroll = true,
            WrapContents = false,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(24, 22, 24, 28),
            BackColor = Color.Transparent
        };
        _settingsView.Controls.Add(_settingsFlow);

        _lockView = new Panel
        {
            Name = "lockView",
            Visible = false
        };
        _lockCard = new AntdUI.Panel
        {
            Name = "lockCard",
            Radius = 24,
            Shadow = 14,
            ShadowOffsetY = 5
        };
        _lockGlyph = new AntdUI.Label
        {
            Name = "lockGlyph",
            Text = "◇",
            Font = new Font("Segoe UI Symbol", 48F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        _lockTitle = new AntdUI.Label
        {
            Name = "lockTitle",
            Text = L.Get("Vault.Locked.Title"),
            Font = LocalizationFonts.Create(18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        _lockDescription = new AntdUI.Label
        {
            Name = "lockDescription",
            Text = L.Get("Vault.Locked.Description"),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        _unlockButton = new AntdUI.Button
        {
            Name = "unlockButton",
            Text = L.Get("Vault.Unlock"),
            IconSvg = "UnlockOutlined",
            Type = AntdUI.TTypeMini.Primary,
            Radius = 12
        };
        _lockCard.Controls.AddRange(
        [
            _lockGlyph,
            _lockTitle,
            _lockDescription,
            _unlockButton
        ]);
        _lockView.Controls.Add(_lockCard);

        _contentHost.Controls.AddRange(
        [
            _dashboard,
            _settingsView,
            _lockView
        ]);
        _shell.Controls.AddRange([_contentHost, _sidebar]);
        Controls.Add(_shell);
        Controls.Add(_titleBar);

        ResumeLayout(performLayout: true);
    }

    private void ApplyDesignerPreviewAppearance()
    {
        var canvas = Color.FromArgb(246, 248, 252);
        var surface = Color.White;
        var surfaceMuted = Color.FromArgb(238, 242, 249);
        var border = Color.FromArgb(218, 224, 235);
        var text = Color.FromArgb(31, 39, 55);
        var textSecondary = Color.FromArgb(98, 108, 128);
        var primary = Color.FromArgb(61, 101, 225);
        var primarySoft = Color.FromArgb(228, 235, 255);

        BackColor = canvas;
        _shell.BackColor = canvas;
        _contentHost.BackColor = canvas;
        _dashboard.BackColor = canvas;
        _settingsView.BackColor = canvas;
        _lockView.BackColor = canvas;
        _filterBar.BackColor = canvas;
        _titleBar.BackColor = surface;
        _titleBar.ForeColor = text;
        _titleBar.DividerColor = border;
        _sidebar.Back = surface;
        _sidebar.BorderColor = border;
        _vaultStatusPanel.Back = surfaceMuted;
        _vaultStatusPanel.BorderColor = border;
        _dashboardHero.Back = surface;
        _dashboardHero.BorderWidth = 1;
        _dashboardHero.BorderColor = border;
        _dashboardHero.ShadowColor = Color.FromArgb(63, 78, 116);
        _lockCard.Back = surface;
        _lockCard.BorderWidth = 1;
        _lockCard.BorderColor = border;

        _brandTitle.ForeColor = text;
        _brandSubtitle.ForeColor = textSecondary;
        _vaultStatus.ForeColor = textSecondary;
        _heroEyebrow.ForeColor = primary;
        _heroTitle.ForeColor = text;
        _heroSubtitle.ForeColor = textSecondary;
        _resultLabel.ForeColor = textSecondary;
        _emptyIcon.ForeColor = primary;
        _emptyTitle.ForeColor = text;
        _emptyText.ForeColor = textSecondary;
        _lockGlyph.ForeColor = primary;
        _lockTitle.ForeColor = text;
        _lockDescription.ForeColor = textSecondary;
        _topMostCheckBox.ForeColor = textSecondary;

        _searchInput.BackColor = surface;
        _searchInput.ForeColor = text;
        _searchInput.PlaceholderColor = textSecondary;
        _searchInput.PrefixFore = textSecondary;
        _searchInput.BorderColor = border;
        _searchInput.BorderHover = primary;
        _searchInput.BorderActive = primary;

        _toggleAllCodesButton.BackColor = surfaceMuted;
        _toggleAllCodesButton.DefaultBack = surfaceMuted;
        _toggleAllCodesButton.DefaultBorderColor = border;
        _toggleAllCodesButton.BorderWidth = 1F;
        _toggleAllCodesButton.ForeColor = text;
        _toggleAllCodesButton.BackHover = primary;
        _toggleAllCodesButton.BackActive = primary;
        _toggleAllCodesButton.ForeHover = Color.White;
        _toggleAllCodesButton.ForeActive = Color.White;
        _toggleAllCodesButton.AutoToggle = false;
        _toggleAllCodesButton.Toggle = false;
        _toggleAllCodesButton.ToggleBack = surfaceMuted;
        _toggleAllCodesButton.ToggleBackHover = primary;
        _toggleAllCodesButton.ToggleBackActive = primary;
        _toggleAllCodesButton.ToggleFore = text;
        _toggleAllCodesButton.ToggleForeHover = Color.White;
        _toggleAllCodesButton.ToggleForeActive = Color.White;

        foreach (var button in new[]
                 {
                     _navVault,
                     _navImport,
                     _navExport,
                     _navSettings,
                     _filterAllButton,
                     _filterFavoriteButton,
                     _filterTimeButton,
                     _filterCounterButton
                 })
        {
            button.BackColor = Color.Transparent;
            button.DefaultBack = Color.Transparent;
            button.DefaultBorderColor = Color.Transparent;
            button.BackHover = surfaceMuted;
            button.BackActive = primarySoft;
            button.ForeColor = textSecondary;
            button.ForeHover = text;
            button.ForeActive = primary;
        }

        _navVault.BackColor = primarySoft;
        _navVault.DefaultBack = primarySoft;
        _navVault.ForeColor = primary;
        _filterAllButton.BackColor = primarySoft;
        _filterAllButton.DefaultBack = primarySoft;
        _filterAllButton.ForeColor = primary;

        foreach (var button in new[] { _themeButton, _aboutButton, _lockButton })
        {
            button.Ghost = false;
            button.BackColor = surface;
            button.DefaultBack = surface;
            button.DefaultBorderColor = Color.Transparent;
            button.BorderWidth = 0;
            button.BackHover = primarySoft;
            button.BackActive = primarySoft;
            button.ForeColor = textSecondary;
            button.ForeHover = primary;
            button.ForeActive = primary;
        }
    }

    private void LayoutDesignerPreview()
    {
        if (_runtimeLayoutActive ||
            ClientSize.Width <= 0 ||
            ClientSize.Height <= 0)
        {
            return;
        }

        PerformLayout();
        var shellWidth = _shell.ClientSize.Width;
        var shellHeight = _shell.ClientSize.Height;
        if (shellWidth <= 0 || shellHeight <= 0)
        {
            return;
        }

        const int headerButtonSize = 48;
        _lockButton.SetBounds(
            Math.Max(0, _titleBar.ClientSize.Width - 52),
            4,
            headerButtonSize,
            headerButtonSize);
        _themeButton.SetBounds(
            Math.Max(0, _lockButton.Left - headerButtonSize),
            4,
            headerButtonSize,
            headerButtonSize);
        _themeButton.Visible = _titleBar.Width >= 460;
        _topMostCheckBox.SetBounds(
            Math.Max(
                0,
                (_themeButton.Visible ? _themeButton.Left : _lockButton.Left) -
                140),
            10,
            132,
            36);
        _topMostCheckBox.Visible = _titleBar.Width >= 560;
        _aboutButton.SetBounds(
            Math.Max(
                0,
                (_topMostCheckBox.Visible
                    ? _topMostCheckBox.Left
                    : _themeButton.Visible
                        ? _themeButton.Left
                        : _lockButton.Left) -
                headerButtonSize),
            4,
            headerButtonSize,
            headerButtonSize);

        if (shellWidth < 874)
        {
            LayoutMobilePreview(shellWidth, shellHeight);
        }
        else
        {
            LayoutDesktopPreview(shellWidth, shellHeight);
        }

        _dashboard.SetBounds(
            0,
            0,
            _contentHost.Width,
            _contentHost.Height);
        _settingsView.SetBounds(
            0,
            0,
            _contentHost.Width,
            _contentHost.Height);
        _settingsFlow.SetBounds(
            0,
            0,
            _settingsView.Width,
            _settingsView.Height);
        _lockView.SetBounds(
            0,
            0,
            _contentHost.Width,
            _contentHost.Height);

        LayoutDashboardPreview();
        LayoutLockPreview();
        _dashboard.BringToFront();
    }

    private void LayoutDesktopPreview(int shellWidth, int shellHeight)
    {
        const int sidebarWidth = 224;
        _sidebar.SetBounds(0, 0, sidebarWidth, shellHeight);
        _contentHost.SetBounds(
            sidebarWidth,
            0,
            Math.Max(0, shellWidth - sidebarWidth),
            shellHeight);

        _brandTitle.Visible = true;
        _brandSubtitle.Visible = true;
        _vaultStatusPanel.Visible = true;
        _brandTitle.SetBounds(24, 24, 180, 30);
        _brandSubtitle.SetBounds(24, 53, 180, 23);

        var top = 108;
        foreach (var button in new[]
                 {
                     _navVault,
                     _navImport,
                     _navExport,
                     _navSettings
                 })
        {
            button.SetBounds(14, top, 196, 48);
            top += 54;
        }

        _vaultStatusPanel.SetBounds(
            16,
            Math.Max(0, shellHeight - 66),
            192,
            46);
        _vaultStatus.SetBounds(12, 4, 168, 38);
    }

    private void LayoutMobilePreview(int shellWidth, int shellHeight)
    {
        const int navigationHeight = 62;
        _sidebar.SetBounds(0, 0, shellWidth, navigationHeight);
        _contentHost.SetBounds(
            0,
            navigationHeight,
            shellWidth,
            Math.Max(0, shellHeight - navigationHeight));
        _brandTitle.Visible = false;
        _brandSubtitle.Visible = false;
        _vaultStatusPanel.Visible = false;

        const int gap = 5;
        var width = Math.Max(74, (shellWidth - 24 - gap * 3) / 4);
        var left = 12;
        foreach (var button in new[]
                 {
                     _navVault,
                     _navImport,
                     _navExport,
                     _navSettings
                 })
        {
            button.SetBounds(left, 8, width, 46);
            left += width + gap;
        }
    }

    private void LayoutDashboardPreview()
    {
        var width = _dashboard.ClientSize.Width;
        var height = _dashboard.ClientSize.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var compact = width < 650;
        var margin = width < 460 ? 12 : 22;
        var heroHeight = compact ? 170 : 128;
        _dashboardHero.SetBounds(
            margin,
            18,
            Math.Max(100, width - margin * 2),
            heroHeight);

        var innerWidth = _dashboardHero.Width;
        var left = width < 460 ? 18 : 28;
        if (compact)
        {
            _heroEyebrow.SetBounds(left, 18, innerWidth - left * 2, 20);
            _heroTitle.SetBounds(left, 38, innerWidth - left * 2, 42);
            _heroSubtitle.SetBounds(left, 79, innerWidth - left * 2, 28);
            _importButton.SetBounds(
                Math.Max(left, innerWidth - left - 96),
                112,
                96,
                42);
            _addButton.SetBounds(
                Math.Max(left, _importButton.Left - 150),
                112,
                142,
                42);
        }
        else
        {
            _importButton.SetBounds(innerWidth - 122, 48, 94, 44);
            _addButton.SetBounds(_importButton.Left - 150, 48, 142, 44);
            var textWidth = Math.Max(180, _addButton.Left - left - 24);
            _heroEyebrow.SetBounds(left, 18, textWidth, 20);
            _heroTitle.SetBounds(left, 38, textWidth, 42);
            _heroSubtitle.SetBounds(left, 79, textWidth, 28);
        }

        var stacked = width < 840;
        var filterTop = _dashboardHero.Bottom + 10;
        _filterBar.SetBounds(0, filterTop, width, stacked ? 108 : 58);
        var contentWidth = Math.Max(100, width - margin * 2);
        var showToggleText = width >= 650;
        _toggleAllCodesButton.Text = showToggleText
            ? L.Get("Dashboard.HideAllCodes")
            : string.Empty;
        var toggleWidth = showToggleText
            ? Math.Max(
                104,
                TextRenderer.MeasureText(
                    L.Get("Dashboard.HideAllCodes"),
                    _toggleAllCodesButton.Font).Width + 38)
            : 42;
        var searchToggleGap = 8;
        var searchWidth = stacked
            ? Math.Max(80, contentWidth - toggleWidth - searchToggleGap)
            : Math.Min(300, Math.Max(180, contentWidth / 2 - 24));
        _searchInput.SetBounds(
            margin,
            stacked ? 4 : 6,
            searchWidth,
            42);
        _toggleAllCodesButton.SetBounds(
            _searchInput.Right + searchToggleGap,
            stacked ? 4 : 6,
            toggleWidth,
            42);

        var buttons = new[]
        {
            _filterAllButton,
            _filterFavoriteButton,
            _filterTimeButton,
            _filterCounterButton
        };
        var buttonWidths = width < 520
            ? new[] { 58, 64, 72, 82 }
            : new[] { 68, 82, 88, 102 };
        var total = buttonWidths.Sum() + 18;
        var buttonLeft = Math.Max(margin, width - margin - total);
        var buttonTop = stacked ? 60 : 9;
        for (var index = 0; index < buttons.Length; index++)
        {
            buttons[index].SetBounds(
                buttonLeft,
                buttonTop,
                buttonWidths[index],
                36);
            buttonLeft += buttonWidths[index] + 6;
        }

        var resultLeft = stacked ? margin + 2 : _toggleAllCodesButton.Right + 14;
        var resultRight = width - margin - total - 10;
        _resultLabel.Visible = resultRight - resultLeft >= 76;
        _resultLabel.SetBounds(
            resultLeft,
            buttonTop,
            Math.Max(72, resultRight - resultLeft),
            36);

        var cardsTop = _filterBar.Bottom + 2;
        var cardsHeight = Math.Max(0, height - cardsTop);
        _cardFlow.SetBounds(0, cardsTop, width, cardsHeight);
        _emptyState.SetBounds(0, cardsTop, width, cardsHeight);
        LayoutEmptyPreview(width);
        _emptyState.BringToFront();
        _searchInput.BringToFront();
        _toggleAllCodesButton.BringToFront();
        _filterBar.BringToFront();
        _dashboardHero.BringToFront();
    }

    private void LayoutEmptyPreview(int width)
    {
        var height = _emptyState.ClientSize.Height;
        var buttonWidth = Math.Min(178, Math.Max(120, width - 36));
        var centerY = Math.Max(12, (height - 230) / 2);
        _emptyIcon.SetBounds(0, centerY, width, 70);
        _emptyTitle.SetBounds(20, centerY + 66, Math.Max(80, width - 40), 38);
        _emptyText.SetBounds(24, centerY + 104, Math.Max(80, width - 48), 52);
        _emptyAddButton.SetBounds(
            Math.Max(0, (width - buttonWidth) / 2),
            centerY + 166,
            buttonWidth,
            44);
    }

    private void LayoutLockPreview()
    {
        var width = Math.Min(500, Math.Max(320, _lockView.Width - 36));
        var height = Math.Min(390, Math.Max(330, _lockView.Height - 48));
        _lockCard.SetBounds(
            Math.Max(0, (_lockView.Width - width) / 2),
            Math.Max(0, (_lockView.Height - height) / 2),
            width,
            height);
        _lockGlyph.SetBounds(0, 38, width, 84);
        _lockTitle.SetBounds(24, 126, width - 48, 46);
        _lockDescription.SetBounds(36, 174, width - 72, 74);
        _unlockButton.SetBounds((width - 164) / 2, height - 80, 164, 46);
    }

    private static AntdUI.Button CreateHeaderButton(
        string name,
        string icon,
        string tooltip)
    {
        return new AntdUI.Button
        {
            Name = name,
            Width = 48,
            IconSvg = icon,
            IconSize = new Size(20, 20),
            Ghost = true,
            Radius = 0,
            WaveSize = 0,
            Tag = tooltip
        };
    }

    private static AntdUI.Button CreateNavigationButton(
        string name,
        string icon,
        string text)
    {
        return new AntdUI.Button
        {
            Name = name,
            Text = text,
            IconSvg = icon,
            TextAlign = ContentAlignment.MiddleLeft,
            Radius = 11,
            Ghost = false,
            BorderWidth = 0,
            WaveSize = 0,
            IconGap = 0.25F
        };
    }

    private static AntdUI.Button CreateFilterButton(string name, string text)
    {
        return new AntdUI.Button
        {
            Name = name,
            Text = text,
            Radius = 18,
            Ghost = false,
            BorderWidth = 0,
            WaveSize = 0,
            Type = AntdUI.TTypeMini.Default
        };
    }
}
