using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;
using AuthenticatorDesk.Services;
using AuthenticatorDesk.UI.Controls;
using AuthenticatorDesk.UI.Dialogs;
using Microsoft.Win32;

namespace AuthenticatorDesk.UI;

public sealed class MainForm : MainFormVisualBase
{
    private const int WmHotkey = 0x0312;
    private const int WmKeyFirst = 0x0100;
    private const int WmKeyLast = 0x0109;
    private const int WmMouseFirst = 0x0200;
    private const int WmMouseLast = 0x020E;

    protected override bool UseMessageFilter => true;

    private enum DashboardFilter
    {
        All,
        Favorite,
        TimeBased,
        CounterBased
    }

    private readonly VaultService _vault;
    private readonly SecureClipboardService _clipboard = new();
    private readonly HotkeyService _hotkeys = new();
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 500 };
    private readonly System.Windows.Forms.Timer _saveTimer = new() { Interval = 650 };
    private readonly System.Windows.Forms.Timer _idleTimer = new() { Interval = 15_000 };
    private readonly NotifyIcon _trayIcon;
    private readonly Dictionary<Guid, bool> _codeHiddenOverrides = [];

    private VaultPayload _payload;
    private DateTime _lastActivityUtc = DateTime.UtcNow;
    private bool _locked;
    private bool _forceExit;
    private bool _settingsVisible;
    private bool _loaded;
    private bool _updatingWindowState;
    private bool _updatingTopMostControls;
    private bool _topMostChangedWhileLocked;
    private bool _showEmptyState;
    private Form? _themeBlockingDialog;
    private AntdUI.Switch? _settingsTopMostSwitch;
    private int _themeEnvironmentSignature;
    private DashboardFilter _filter;

    internal bool RestartRequested { get; private set; }

    public MainForm(VaultService vault, VaultPayload payload, bool startMinimized)
    {
        ActivateRuntimeLayout();
        _vault = vault;
        _payload = payload;
        _payload.Normalize();
        ThemePaletteService.Apply(_payload.Settings.Theme);
        ApplyLocalizedVisualText();
        _topMostCheckBox.Checked = _payload.Settings.AlwaysOnTop;

        _trayIcon = CreateTrayIcon();
        WireEvents();
        RestoreWindowBounds();
        ApplyTheme();
        RebuildCards();
        UpdateNavigation();
        LayoutShell();

        _clockTimer.Tick += (_, _) => RefreshCodes();
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveNow(showError: true);
        };
        _idleTimer.Tick += (_, _) => CheckIdleLock();
        _clockTimer.Start();
        _idleTimer.Start();
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;

        if (startMinimized || _payload.Settings.StartMinimized)
        {
            Shown += (_, _) => BeginInvoke(HideToTray);
        }
    }

    protected override bool OnPreFilterMessage(Message message)
    {
        if ((message.Msg >= WmKeyFirst && message.Msg <= WmKeyLast) ||
            (message.Msg >= WmMouseFirst && message.Msg <= WmMouseLast))
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        return base.OnPreFilterMessage(message);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotkey &&
            _hotkeys.TryResolve(message.WParam.ToInt32(), out var entryId))
        {
            HandleGlobalHotkey(entryId);
        }

        base.WndProc(ref message);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
            if (_themeBlockingDialog is not null)
            {
                _themeBlockingDialog.FormClosed -= OnThemeBlockingDialogClosed;
                _themeBlockingDialog = null;
            }

            _clockTimer.Dispose();
            _saveTimer.Dispose();
            _idleTimer.Dispose();
            _hotkeys.Dispose();
            _clipboard.Dispose();
            var trayMenu = _trayIcon.ContextMenuStrip;
            var trayIconImage = _trayIcon.Icon;
            _trayIcon.ContextMenuStrip = null;
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            trayIconImage?.Dispose();
            trayMenu?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ApplyLocalizedVisualText()
    {
        _titleBar.Text = "AuthenticatorDesk";
        _titleBar.SubText = L.Get("Main.Header.Subtitle");
        _topMostCheckBox.Text = L.Get("Main.AlwaysOnTop.Label");
        _topMostCheckBox.AccessibleName = L.Get("Main.AlwaysOnTop.Label");
        _topMostCheckBox.AccessibleDescription =
            L.Get("Main.AlwaysOnTop.Description");
        _themeButton.Tag = L.Get("Main.Theme.ToggleTooltip");
        _aboutButton.Tag = L.Get("About.Tooltip");
        _aboutButton.AccessibleName = L.Get("About.Title");
        _aboutButton.AccessibleDescription = L.Get("About.Tooltip");
        _lockButton.Tag = L.Get("Main.Vault.LockTooltip");

        _brandTitle.Text = "AuthenticatorDesk";
        _brandSubtitle.Text = L.Get("Main.Brand.Subtitle");
        _navVault.Text = L.Get("Main.Nav.Authenticators");
        _navImport.Text = L.Get("Common.Import");
        _navExport.Text = L.Get("Common.Export");
        _navSettings.Text = L.Get("Common.Settings");

        _heroEyebrow.Text = L.Get("Dashboard.Eyebrow");
        _heroTitle.Text = L.Get("Dashboard.Title");
        _searchInput.PlaceholderText = L.Get("Dashboard.SearchPlaceholder");
        _addButton.Text = L.Get("Dashboard.AddAuthenticator");
        _importButton.Text = L.Get("Common.Import");
        _filterAllButton.Text = L.Get("Dashboard.Filter.All");
        _filterFavoriteButton.Text = L.Get("Dashboard.Filter.Favorites");
        _filterTimeButton.Text = L.Get("Dashboard.Filter.TimeBased");
        _filterCounterButton.Text = L.Get("Dashboard.Filter.CounterBased");
        _toggleAllCodesButton.Text = L.Get("Dashboard.HideAllCodes");
        _toggleAllCodesButton.Tag = L.Get("Dashboard.HideAllCodes");
        _toggleAllCodesButton.AccessibleName = L.Get("Dashboard.HideAllCodes");
        _emptyTitle.Text = L.Get("Dashboard.Empty.Title");
        _emptyText.Text = L.Get("Dashboard.Empty.Description");
        _emptyAddButton.Text = L.Get("Dashboard.Empty.AddFirst");

        _lockTitle.Text = L.Get("Vault.Locked.Title");
        _lockDescription.Text = L.Get("Vault.Locked.Description");
        _unlockButton.Text = L.Get("Vault.Unlock");
    }

    private void WireEvents()
    {
        Shown += (_, _) =>
        {
            _loaded = true;
            SetAlwaysOnTop(_payload.Settings.AlwaysOnTop, persist: false);
            RegisterHotkeys(showErrors: true);
            UpdateVaultStatus();
            BeginInvoke(RefreshHeaderActionsAfterShown);
        };
        Resize += (_, _) =>
        {
            LayoutShell();
            if (!_loaded || _updatingWindowState)
            {
                return;
            }

            if (WindowState == FormWindowState.Minimized)
            {
                if (_payload.Settings.LockOnMinimize && !_locked)
                {
                    if (!LockVault())
                    {
                        _updatingWindowState = true;
                        WindowState = FormWindowState.Normal;
                        _updatingWindowState = false;
                        return;
                    }
                }

                if (_payload.Settings.MinimizeToTray)
                {
                    BeginInvoke(HideToTray);
                }
            }
            else if (WindowState is
                     FormWindowState.Normal or
                     FormWindowState.Maximized)
            {
                CaptureWindowBounds();
            }
        };
        Move += (_, _) =>
        {
            if (_loaded && WindowState == FormWindowState.Normal)
            {
                CaptureWindowBounds();
            }
        };
        FormClosing += OnFormClosing;

        _themeButton.Click += (_, _) => ToggleTheme();
        _themeButton.MouseEnter += (_, _) => AntdUI.Tooltip.open(
            _themeButton,
            L.Get("Main.Theme.ToggleTooltip"),
            AntdUI.TAlign.Bottom);
        _aboutButton.Click += (_, _) => AboutLicensesDialog.Open(this);
        _aboutButton.MouseEnter += (_, _) => AntdUI.Tooltip.open(
            _aboutButton,
            L.Get("About.Tooltip"),
            AntdUI.TAlign.Bottom);
        _lockButton.MouseEnter += (_, _) => AntdUI.Tooltip.open(
            _lockButton,
            L.Get("Main.Vault.LockTooltip"),
            AntdUI.TAlign.Bottom);
        _topMostCheckBox.MouseEnter += (_, _) => AntdUI.Tooltip.open(
            _topMostCheckBox,
            L.Get("Main.AlwaysOnTop.Description"),
            AntdUI.TAlign.Bottom);
        _topMostCheckBox.CheckedChanged += (_, eventArgs) =>
        {
            if (!_updatingTopMostControls)
            {
                SetAlwaysOnTop(eventArgs.Value);
            }
        };
        _lockButton.Click += (_, _) =>
        {
            if (_locked)
            {
                UnlockVault();
            }
            else
            {
                LockVault();
            }
        };
        _navVault.Click += (_, _) => ShowDashboard();
        _navImport.Click += (_, _) => ShowImportMenu(_navImport);
        _navExport.Click += (_, _) => ShowExportMenu(_navExport);
        _navSettings.Click += (_, _) => ShowSettings();
        _addButton.Click += (_, _) => AddEntry();
        _emptyAddButton.Click += (_, _) => AddEntry();
        _importButton.Click += (_, _) => ShowImportMenu(_importButton);
        _searchInput.TextChanged += (_, _) => RebuildCards();
        _toggleAllCodesButton.Click += (_, _) => ToggleAllCodesVisibility();
        _toggleAllCodesButton.MouseEnter += (_, _) => AntdUI.Tooltip.open(
            _toggleAllCodesButton,
            _toggleAllCodesButton.Tag as string ?? L.Get("Dashboard.HideAllCodes"),
            AntdUI.TAlign.Top);
        _filterAllButton.Click += (_, _) => SetFilter(DashboardFilter.All);
        _filterFavoriteButton.Click += (_, _) => SetFilter(DashboardFilter.Favorite);
        _filterTimeButton.Click += (_, _) => SetFilter(DashboardFilter.TimeBased);
        _filterCounterButton.Click += (_, _) => SetFilter(DashboardFilter.CounterBased);
        _unlockButton.Click += (_, _) => UnlockVault();
        _settingsFlow.Resize += (_, _) => LayoutSettingsSections();
    }

    private void RefreshHeaderActionsAfterShown()
    {
        if (IsDisposed || Disposing || !_titleBar.IsHandleCreated)
        {
            return;
        }

        LayoutShell();
        _titleBar.Invalidate(true);
        _themeButton.Invalidate();
        _aboutButton.Invalidate();
        _lockButton.Invalidate();
        _titleBar.Update();
        _themeButton.Update();
        _aboutButton.Update();
        _lockButton.Update();
    }

    private void LayoutShell()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var shellWidth = _shell.ClientSize.Width;
        var shellHeight = _shell.ClientSize.Height;
        var logicalShellWidth = ToLogical(shellWidth);
        // Keep at least 650 logical pixels for desktop content. This avoids a
        // discontinuity where making the window one pixel wider used to
        // introduce the sidebar and make the actual content much narrower.
        var mobile = logicalShellWidth < 224 + 650;

        var headerContentRight = _titleBar.DisplayRectangle.Right;
        var headerButtonSize = ScaleLogical(48);
        _lockButton.SetBounds(
            headerContentRight - headerButtonSize,
            ScaleLogical(4),
            headerButtonSize,
            headerButtonSize);
        _themeButton.SetBounds(
            _lockButton.Left - headerButtonSize,
            ScaleLogical(4),
            headerButtonSize,
            headerButtonSize);
        _themeButton.Visible = ToLogical(_titleBar.Width) >= 460;

        var topMostRight =
            (_themeButton.Visible ? _themeButton.Left : _lockButton.Left) -
            ScaleLogical(8);
        var topMostLogicalWidth = Math.Max(
            96,
            ToLogical(TextRenderer.MeasureText(
                _topMostCheckBox.Text,
                _topMostCheckBox.Font).Width) + 28);
        _topMostCheckBox.SetBounds(
            topMostRight - ScaleLogical(topMostLogicalWidth),
            ScaleLogical(10),
            ScaleLogical(topMostLogicalWidth),
            ScaleLogical(36));
        _topMostCheckBox.Visible =
            ToLogical(_titleBar.Width) >= 560 + (topMostLogicalWidth - 96);
        _aboutButton.SetBounds(
            (_topMostCheckBox.Visible
                ? _topMostCheckBox.Left
                : _themeButton.Visible
                    ? _themeButton.Left
                    : _lockButton.Left) -
            headerButtonSize,
            ScaleLogical(4),
            headerButtonSize,
            headerButtonSize);
        var titleRight = _titleBar.DisplayRectangle.Left +
                         ScaleLogical(32) +
                         TextRenderer.MeasureText(_titleBar.Text, _titleBar.Font).Width;
        var subtitle = _locked
            ? L.Get("Vault.Locked.Title")
            : L.Get("Main.Header.Subtitle");
        var actionLeft = _aboutButton.Left;
        var subtitleWidth = TextRenderer.MeasureText(subtitle, _titleBar.Font).Width;
        _titleBar.SubText = actionLeft - titleRight >= subtitleWidth + ScaleLogical(20)
            ? subtitle
            : string.Empty;

        if (mobile)
        {
            var navigationHeight = ScaleLogical(62);
            _sidebar.SetBounds(0, 0, shellWidth, navigationHeight);
            _contentHost.SetBounds(
                0,
                navigationHeight,
                shellWidth,
                Math.Max(0, shellHeight - navigationHeight));
            _brandTitle.Visible = false;
            _brandSubtitle.Visible = false;
            _vaultStatusPanel.Visible = false;

            var gap = ScaleLogical(5);
            var width = Math.Max(
                ScaleLogical(74),
                (shellWidth - ScaleLogical(24) - gap * 3) / 4);
            var left = ScaleLogical(12);
            foreach (var button in new[] { _navVault, _navImport, _navExport, _navSettings })
            {
                button.SetBounds(left, ScaleLogical(8), width, ScaleLogical(46));
                button.Text = logicalShellWidth < 450
                    ? button == _navVault ? L.Get("Main.Nav.Authenticators")
                    : button == _navImport ? L.Get("Common.Import")
                    : button == _navExport ? L.Get("Common.Export")
                    : L.Get("Common.Settings")
                    : button == _navVault ? L.Get("Main.Nav.MyAuthenticators")
                    : button == _navImport ? L.Get("Common.Import")
                    : button == _navExport ? L.Get("Common.Export")
                    : L.Get("Common.Settings");
                left += width + gap;
            }
        }
        else
        {
            var sidebarWidth = ScaleLogical(224);
            _sidebar.SetBounds(0, 0, sidebarWidth, shellHeight);
            _contentHost.SetBounds(sidebarWidth, 0, shellWidth - sidebarWidth, shellHeight);
            _brandTitle.Visible = true;
            _brandSubtitle.Visible = true;
            _vaultStatusPanel.Visible = true;
            _brandTitle.SetBounds(
                ScaleLogical(24),
                ScaleLogical(24),
                sidebarWidth - ScaleLogical(44),
                ScaleLogical(30));
            _brandSubtitle.SetBounds(
                ScaleLogical(24),
                ScaleLogical(53),
                sidebarWidth - ScaleLogical(44),
                ScaleLogical(23));

            var top = ScaleLogical(108);
            foreach (var button in new[] { _navVault, _navImport, _navExport, _navSettings })
            {
                button.SetBounds(
                    ScaleLogical(14),
                    top,
                    sidebarWidth - ScaleLogical(28),
                    ScaleLogical(48));
                button.Text = button == _navVault ? L.Get("Main.Nav.MyAuthenticators")
                    : button == _navImport ? L.Get("Main.Nav.ImportAndMigration")
                    : button == _navExport ? L.Get("Main.Nav.ExportAndBackup")
                    : L.Get("Common.Settings");
                top += ScaleLogical(54);
            }

            _vaultStatusPanel.SetBounds(
                ScaleLogical(16),
                shellHeight - ScaleLogical(66),
                sidebarWidth - ScaleLogical(32),
                ScaleLogical(46));
            _vaultStatus.SetBounds(
                ScaleLogical(12),
                ScaleLogical(4),
                _vaultStatusPanel.Width - ScaleLogical(24),
                ScaleLogical(38));
        }

        _dashboard.SetBounds(0, 0, _contentHost.Width, _contentHost.Height);
        _settingsView.SetBounds(0, 0, _contentHost.Width, _contentHost.Height);
        _settingsFlow.SetBounds(0, 0, _settingsView.ClientSize.Width, _settingsView.ClientSize.Height);
        _lockView.SetBounds(0, 0, _contentHost.Width, _contentHost.Height);
        LayoutDashboard();
        LayoutLockView();
        LayoutSettingsSections();
    }

    private void LayoutDashboard()
    {
        var width = _dashboard.ClientSize.Width;
        var height = _dashboard.ClientSize.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var logicalWidth = ToLogical(width);
        var compactHero = logicalWidth < 650;
        var stackedToolbar = logicalWidth < 840;
        var heroMargin = ScaleLogical(logicalWidth < 460 ? 12 : 22);
        var heroHeight = ScaleLogical(compactHero ? 170 : 128);
        _dashboardHero.SetBounds(
            heroMargin,
            ScaleLogical(18),
            Math.Max(ScaleLogical(100), width - heroMargin * 2),
            heroHeight);

        var innerWidth = _dashboardHero.Width;
        var left = ScaleLogical(logicalWidth < 460 ? 18 : 28);
        if (compactHero)
        {
            var availableActionWidth = Math.Max(
                ScaleLogical(180),
                innerWidth - left * 2);
            var importWidth = Math.Max(
                ScaleLogical(logicalWidth < 430 ? 84 : 96),
                TextRenderer.MeasureText(_importButton.Text, _importButton.Font).Width +
                ScaleLogical(42));
            var addWidth = Math.Max(
                ScaleLogical(logicalWidth < 430 ? 122 : 142),
                TextRenderer.MeasureText(_addButton.Text, _addButton.Font).Width +
                ScaleLogical(46));
            var actionGap = ScaleLogical(8);
            if (importWidth + addWidth + actionGap > availableActionWidth)
            {
                importWidth = Math.Min(
                    importWidth,
                    (int)(availableActionWidth * 0.38F));
                addWidth = availableActionWidth - importWidth - actionGap;
            }
            _heroEyebrow.SetBounds(
                left,
                ScaleLogical(18),
                innerWidth - left * 2,
                ScaleLogical(20));
            _heroTitle.SetBounds(
                left,
                ScaleLogical(38),
                innerWidth - left * 2,
                ScaleLogical(42));
            _heroSubtitle.SetBounds(
                left,
                ScaleLogical(79),
                innerWidth - left * 2,
                ScaleLogical(28));
            _importButton.SetBounds(
                innerWidth - left - importWidth,
                ScaleLogical(112),
                importWidth,
                ScaleLogical(42));
            _addButton.SetBounds(
                _importButton.Left - addWidth - actionGap,
                ScaleLogical(112),
                addWidth,
                ScaleLogical(42));
        }
        else
        {
            var importWidth = Math.Max(
                ScaleLogical(94),
                TextRenderer.MeasureText(_importButton.Text, _importButton.Font).Width +
                ScaleLogical(42));
            var addWidth = Math.Max(
                ScaleLogical(142),
                TextRenderer.MeasureText(_addButton.Text, _addButton.Font).Width +
                ScaleLogical(46));
            importWidth = Math.Min(importWidth, ScaleLogical(180));
            addWidth = Math.Min(addWidth, ScaleLogical(240));
            _importButton.SetBounds(
                innerWidth - ScaleLogical(28) - importWidth,
                ScaleLogical(48),
                importWidth,
                ScaleLogical(44));
            _addButton.SetBounds(
                _importButton.Left - addWidth - ScaleLogical(8),
                ScaleLogical(48),
                addWidth,
                ScaleLogical(44));
            var textWidth = Math.Max(
                ScaleLogical(180),
                _addButton.Left - left - ScaleLogical(24));
            _heroEyebrow.SetBounds(left, ScaleLogical(18), textWidth, ScaleLogical(20));
            _heroTitle.SetBounds(left, ScaleLogical(38), textWidth, ScaleLogical(42));
            _heroSubtitle.SetBounds(left, ScaleLogical(79), textWidth, ScaleLogical(28));
        }

        var filterTop = _dashboardHero.Bottom + ScaleLogical(10);
        _filterBar.SetBounds(
            0,
            filterTop,
            width,
            ScaleLogical(stackedToolbar ? 108 : 58));
        var contentLeft = heroMargin;
        var contentRight = width - heroMargin;
        var contentWidth = Math.Max(ScaleLogical(100), contentRight - contentLeft);
        UpdateToggleAllCodesButton();
        var toggleAllWidth = string.IsNullOrEmpty(_toggleAllCodesButton.Text)
            ? ScaleLogical(42)
            : Math.Clamp(
                TextRenderer.MeasureText(
                    _toggleAllCodesButton.Text,
                    _toggleAllCodesButton.Font).Width + ScaleLogical(38),
                ScaleLogical(104),
                ScaleLogical(150));
        var searchToggleGap = ScaleLogical(8);
        var filterButtons = new[]
        {
            _filterAllButton,
            _filterFavoriteButton,
            _filterTimeButton,
            _filterCounterButton
        };
        var minimumButtonWidths = (logicalWidth < 520
                ? new[] { 58, 58, 58, 70 }
                : new[] { 68, 68, 68, 82 })
            .Select(ScaleLogical)
            .ToArray();
        var buttonWidths = filterButtons
            .Select((button, index) => Math.Max(
                minimumButtonWidths[index],
                TextRenderer.MeasureText(button.Text, button.Font).Width +
                ScaleLogical(24)))
            .ToArray();
        var buttonGap = ScaleLogical(6);
        var desktopSearchWidth = Math.Min(
            ScaleLogical(320),
            Math.Max(ScaleLogical(220), (int)(contentWidth * 0.36F)));
        var availableButtonWidth = stackedToolbar
            ? contentWidth
            : Math.Max(
                ScaleLogical(200),
                contentWidth - desktopSearchWidth - toggleAllWidth -
                searchToggleGap);
        var total = buttonWidths.Sum() + buttonGap * 3;
        if (total > availableButtonWidth)
        {
            var sharedWidth = Math.Max(
                ScaleLogical(46),
                (availableButtonWidth - buttonGap * 3) / filterButtons.Length);
            Array.Fill(buttonWidths, sharedWidth);
            total = buttonWidths.Sum() + buttonGap * 3;
        }

        int buttonLeft;
        int controlTop;
        if (stackedToolbar)
        {
            _searchInput.SetBounds(
                contentLeft,
                ScaleLogical(4),
                Math.Max(
                    ScaleLogical(80),
                    contentWidth - toggleAllWidth - searchToggleGap),
                ScaleLogical(42));
            _toggleAllCodesButton.SetBounds(
                contentRight - toggleAllWidth,
                ScaleLogical(4),
                toggleAllWidth,
                ScaleLogical(42));
            controlTop = ScaleLogical(60);
            buttonLeft = contentRight - total;
            var resultWidth = buttonLeft - contentLeft - ScaleLogical(10);
            _resultLabel.Visible = resultWidth >= ScaleLogical(76);
            if (_resultLabel.Visible)
            {
                _resultLabel.SetBounds(
                    contentLeft + ScaleLogical(2),
                    controlTop,
                    resultWidth,
                    ScaleLogical(36));
            }
            else
            {
                buttonLeft = contentLeft;
            }
        }
        else
        {
            _searchInput.SetBounds(
                contentLeft,
                ScaleLogical(6),
                desktopSearchWidth,
                ScaleLogical(42));
            _toggleAllCodesButton.SetBounds(
                _searchInput.Right + searchToggleGap,
                ScaleLogical(6),
                toggleAllWidth,
                ScaleLogical(42));
            controlTop = ScaleLogical(9);
            buttonLeft = contentRight - total;
            var resultLeft = _toggleAllCodesButton.Right + ScaleLogical(14);
            var resultWidth = buttonLeft - resultLeft - ScaleLogical(10);
            _resultLabel.Visible = resultWidth >= ScaleLogical(72);
            if (_resultLabel.Visible)
            {
                _resultLabel.SetBounds(
                    resultLeft,
                    controlTop,
                    resultWidth,
                    ScaleLogical(36));
            }
        }

        for (var index = 0; index < filterButtons.Length; index++)
        {
            filterButtons[index].SetBounds(
                buttonLeft,
                controlTop,
                buttonWidths[index],
                ScaleLogical(36));
            buttonLeft += buttonWidths[index] + buttonGap;
        }

        var cardsTop = _filterBar.Bottom + ScaleLogical(2);
        _cardFlow.SetBounds(0, cardsTop, width, Math.Max(0, height - cardsTop));
        _emptyState.SetBounds(0, cardsTop, width, Math.Max(0, height - cardsTop));
        LayoutCards();
        LayoutEmptyState(width);

        _searchInput.BringToFront();
        _toggleAllCodesButton.BringToFront();
        _addButton.BringToFront();
        _importButton.BringToFront();
        _filterBar.BringToFront();
        if (_showEmptyState)
        {
            _emptyState.BringToFront();
        }
        else
        {
            _cardFlow.BringToFront();
        }
    }

    private void LayoutCards()
    {
        if (_cardFlow.ClientSize.Width <= 0)
        {
            return;
        }

        var usable = Math.Max(
            ScaleLogical(260),
            _cardFlow.ClientSize.Width -
            _cardFlow.Padding.Horizontal -
            ScaleLogical(20));
        var columns = usable >= ScaleLogical(970)
            ? 3
            : usable >= ScaleLogical(620)
                ? 2
                : 1;
        var gap = ScaleLogical(16);
        var cardWidth = Math.Max(
            ScaleLogical(270),
            (usable - gap * (columns - 1)) / columns);
        foreach (var card in _cardFlow.Controls.OfType<AuthenticatorCard>())
        {
            card.Width = cardWidth;
            card.Compact =
                _payload.Settings.CompactCards ||
                cardWidth < ScaleLogical(310);
        }
    }

    private void LayoutLockView()
    {
        var width = Math.Min(
            ScaleLogical(500),
            Math.Max(
                ScaleLogical(320),
                _lockView.ClientSize.Width - ScaleLogical(36)));
        var height = Math.Min(
            ScaleLogical(390),
            Math.Max(
                ScaleLogical(330),
                _lockView.ClientSize.Height - ScaleLogical(48)));
        _lockCard.SetBounds(
            Math.Max(
                ScaleLogical(18),
                (_lockView.ClientSize.Width - width) / 2),
            Math.Max(
                ScaleLogical(24),
                (_lockView.ClientSize.Height - height) / 2),
            width,
            height);
        _lockGlyph.SetBounds(0, ScaleLogical(42), width, ScaleLogical(92));
        _lockTitle.SetBounds(
            ScaleLogical(20),
            ScaleLogical(139),
            width - ScaleLogical(40),
            ScaleLogical(45));
        _lockDescription.SetBounds(
            ScaleLogical(30),
            ScaleLogical(187),
            width - ScaleLogical(60),
            ScaleLogical(72));
        _unlockButton.SetBounds(
            (width - ScaleLogical(164)) / 2,
            height - ScaleLogical(80),
            ScaleLogical(164),
            ScaleLogical(46));
    }

    private void LayoutEmptyState(int width)
    {
        var height = _emptyState.ClientSize.Height;
        var buttonWidth = Math.Min(
            ScaleLogical(178),
            Math.Max(ScaleLogical(120), width - ScaleLogical(36)));

        if (height < ScaleLogical(150))
        {
            _emptyIcon.Visible = false;
            _emptyText.Visible = false;
            var titleHeight = Math.Min(ScaleLogical(32), Math.Max(1, height / 3));
            var buttonHeight = Math.Min(
                ScaleLogical(44),
                Math.Max(ScaleLogical(28), height - titleHeight - ScaleLogical(12)));
            _emptyTitle.SetBounds(
                ScaleLogical(12),
                ScaleLogical(3),
                Math.Max(1, width - ScaleLogical(24)),
                titleHeight);
            _emptyAddButton.SetBounds(
                Math.Max(0, (width - buttonWidth) / 2),
                Math.Max(
                    _emptyTitle.Bottom + ScaleLogical(4),
                    height - buttonHeight - ScaleLogical(4)),
                buttonWidth,
                buttonHeight);
            return;
        }

        _emptyText.Visible = true;
        if (height < ScaleLogical(230))
        {
            _emptyIcon.Visible = false;
            var groupHeight = ScaleLogical(126);
            var top = Math.Max(0, (height - groupHeight) / 2);
            _emptyTitle.SetBounds(
                ScaleLogical(20),
                top,
                Math.Max(1, width - ScaleLogical(40)),
                ScaleLogical(32));
            _emptyText.SetBounds(
                ScaleLogical(24),
                top + ScaleLogical(32),
                Math.Max(1, width - ScaleLogical(48)),
                ScaleLogical(42));
            _emptyAddButton.SetBounds(
                Math.Max(0, (width - buttonWidth) / 2),
                top + ScaleLogical(82),
                buttonWidth,
                ScaleLogical(44));
            return;
        }

        _emptyIcon.Visible = true;
        var centerY = Math.Max(
            ScaleLogical(12),
            (height - ScaleLogical(230)) / 2);
        _emptyIcon.SetBounds(0, centerY, width, ScaleLogical(70));
        _emptyTitle.SetBounds(
            ScaleLogical(20),
            centerY + ScaleLogical(66),
            Math.Max(ScaleLogical(80), width - ScaleLogical(40)),
            ScaleLogical(38));
        _emptyText.SetBounds(
            ScaleLogical(24),
            centerY + ScaleLogical(104),
            Math.Max(ScaleLogical(80), width - ScaleLogical(48)),
            ScaleLogical(52));
        _emptyAddButton.SetBounds(
            Math.Max(0, (width - buttonWidth) / 2),
            centerY + ScaleLogical(166),
            buttonWidth,
            ScaleLogical(44));
    }

    private void RebuildCards()
    {
        if (_locked)
        {
            return;
        }

        var search = _searchInput.Text.Trim();
        var entries = _payload.Entries
            .OrderBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.CreatedAtUtc)
            .Where(entry => _filter switch
            {
                DashboardFilter.Favorite => entry.Favorite,
                DashboardFilter.TimeBased => !entry.IsCounterBased,
                DashboardFilter.CounterBased => entry.IsCounterBased,
                _ => true
            })
            .Where(entry => string.IsNullOrWhiteSpace(search) ||
                            entry.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            entry.DisplayIssuer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            (entry.Notes?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        _cardFlow.SuspendLayout();
        foreach (Control control in _cardFlow.Controls)
        {
            control.Dispose();
        }

        _cardFlow.Controls.Clear();
        foreach (var entry in entries)
        {
            var card = new AuthenticatorCard(entry)
            {
                GlobalHideCodes = _payload.Settings.HideCodes,
                Compact = _payload.Settings.CompactCards
            };
            if (_codeHiddenOverrides.TryGetValue(entry.Id, out var hidden))
            {
                card.SetCodeHidden(hidden);
            }

            card.ActionRequested += OnCardAction;
            _cardFlow.Controls.Add(card);
        }

        _cardFlow.ResumeLayout();
        _showEmptyState = entries.Count == 0;
        _emptyState.Visible = _showEmptyState;
        if (_showEmptyState)
        {
            _emptyState.BringToFront();
            _emptyTitle.Text = _payload.Entries.Count == 0
                ? L.Get("Dashboard.Empty.Title")
                : L.Get("Dashboard.NoMatches.Title");
            _emptyText.Text = _payload.Entries.Count == 0
                ? L.Get("Dashboard.Empty.Description")
                : L.Get("Dashboard.NoMatches.Description");
            _emptyAddButton.Visible = _payload.Entries.Count == 0;
        }
        else
        {
            _cardFlow.BringToFront();
        }

        _resultLabel.Text = L.Format("Dashboard.ResultCount", entries.Count);
        _heroSubtitle.Text = _payload.Entries.Count == 0
            ? L.Get("Dashboard.GetStarted")
            : L.Format("Dashboard.AccountSummary", _payload.Entries.Count);
        UpdateFilterButtons();
        UpdateToggleAllCodesButton();
        LayoutDashboard();
    }

    private void RefreshCodes()
    {
        if (_locked)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var card in _cardFlow.Controls.OfType<AuthenticatorCard>())
        {
            card.RefreshCode(now);
        }
    }

    private void ToggleAllCodesVisibility()
    {
        if (_locked || _payload.Entries.Count == 0)
        {
            return;
        }

        var hide = _payload.Entries.Any(entry => !IsCodeHidden(entry));
        foreach (var entry in _payload.Entries)
        {
            _codeHiddenOverrides[entry.Id] = hide;
        }

        foreach (var card in _cardFlow.Controls.OfType<AuthenticatorCard>())
        {
            card.SetCodeHidden(hide);
        }

        UpdateToggleAllCodesButton();
        LayoutDashboard();
    }

    private bool IsCodeHidden(AuthenticatorEntry entry)
    {
        return _codeHiddenOverrides.TryGetValue(entry.Id, out var hidden)
            ? hidden
            : _payload.Settings.HideCodes || entry.RequireUnlock || !entry.AutoRefresh;
    }

    private void UpdateToggleAllCodesButton()
    {
        var hasEntries = !_locked && _payload.Entries.Count > 0;
        var allHidden = hasEntries && _payload.Entries.All(IsCodeHidden);
        var actionText = L.Get(
            allHidden
                ? "Dashboard.ShowAllCodes"
                : "Dashboard.HideAllCodes");
        _toggleAllCodesButton.Enabled = hasEntries;
        _toggleAllCodesButton.IconSvg = allHidden
            ? "EyeOutlined"
            : "EyeInvisibleOutlined";
        _toggleAllCodesButton.Text = ToLogical(_dashboard.ClientSize.Width) >= 650
            ? actionText
            : string.Empty;
        _toggleAllCodesButton.Tag = actionText;
        _toggleAllCodesButton.AccessibleName = actionText;
    }

    private void OnCardAction(object? sender, AuthenticatorCardActionEventArgs eventArgs)
    {
        if (_locked)
        {
            return;
        }

        switch (eventArgs.Action)
        {
            case AuthenticatorCardAction.Copy:
                CopyCode(eventArgs.Entry);
                break;
            case AuthenticatorCardAction.Reveal:
                if (sender is AuthenticatorCard revealCard)
                {
                    _codeHiddenOverrides[eventArgs.Entry.Id] = revealCard.CodeHidden;
                    UpdateToggleAllCodesButton();
                    LayoutDashboard();
                    if (!revealCard.CodeHidden && eventArgs.Entry.CopyOnReveal)
                    {
                        CopyCode(eventArgs.Entry);
                    }
                }

                break;
            case AuthenticatorCardAction.Favorite:
                eventArgs.Entry.Favorite = !eventArgs.Entry.Favorite;
                eventArgs.Entry.ModifiedAtUtc = DateTime.UtcNow;
                if (sender is AuthenticatorCard favoriteCard)
                {
                    favoriteCard.RefreshEntryPresentation();
                    favoriteCard.ApplyTheme();
                }

                MarkDirty();
                if (_filter == DashboardFilter.Favorite)
                {
                    RebuildCards();
                }

                break;
            case AuthenticatorCardAction.Edit:
                EditEntry(eventArgs.Entry);
                break;
            case AuthenticatorCardAction.Delete:
                ConfirmDelete(eventArgs.Entry);
                break;
            case AuthenticatorCardAction.Export:
                using (var qrDialog = new QrExportDialog(eventArgs.Entry))
                {
                    qrDialog.ShowDialog(this);
                }

                break;
            case AuthenticatorCardAction.ShowSecret:
                using (var secretDialog = new SensitiveValueDialog(
                           L.Get("Secret.Title"),
                           L.Get("Secret.Warning"),
                           eventArgs.Entry.Secret))
                {
                    secretDialog.ShowDialog(this);
                }

                break;
            case AuthenticatorCardAction.ShowRestoreCode:
                ShowRestoreCode(eventArgs.Entry);
                break;
            case AuthenticatorCardAction.AdvanceCounter:
                eventArgs.Entry.Counter++;
                eventArgs.Entry.ModifiedAtUtc = DateTime.UtcNow;
                (sender as AuthenticatorCard)?.RefreshCode(DateTimeOffset.UtcNow);
                MarkDirty(immediate: true);
                break;
            case AuthenticatorCardAction.MoveUp:
                MoveEntry(eventArgs.Entry, -1);
                break;
            case AuthenticatorCardAction.MoveDown:
                MoveEntry(eventArgs.Entry, 1);
                break;
        }
    }

    private void AddEntry()
    {
        if (!EnsureUnlocked())
        {
            return;
        }

        var entry = new AuthenticatorEntry
        {
            SortOrder = _payload.Entries.Count == 0
                ? 0
                : _payload.Entries.Max(item => item.SortOrder) + 10
        };
        var result = EntryEditorForm.Edit(this, entry, isNew: true);
        if (result is null)
        {
            return;
        }

        _payload.Entries.Add(result);
        NormalizeSortOrder();
        MarkDirty(immediate: true);
        RegisterHotkeys(showErrors: true);
        RebuildCards();
    }

    private void EditEntry(AuthenticatorEntry entry)
    {
        var result = EntryEditorForm.Edit(this, entry, isNew: false);
        if (result is null)
        {
            return;
        }

        var index = _payload.Entries.FindIndex(item => item.Id == entry.Id);
        if (index >= 0)
        {
            _payload.Entries[index] = result;
        }

        MarkDirty(immediate: true);
        RegisterHotkeys(showErrors: true);
        RebuildCards();
    }

    private void ConfirmDelete(AuthenticatorEntry entry)
    {
        if (!_payload.Settings.ConfirmBeforeDelete)
        {
            DeleteEntry(entry);
            return;
        }

        AntdUI.Modal.open(DialogSizing.MakeResizable(new AntdUI.Modal.Config(
            this,
            L.Get("Entry.Delete.Title"),
            L.Format("Entry.Delete.Confirmation", entry.DisplayName))
        {
            Icon = AntdUI.TType.Warn,
            OkText = L.Get("Common.Delete"),
            CancelText = L.Get("Common.Cancel"),
            OnOk = _ =>
            {
                DeleteEntry(entry);
                return true;
            }
        }));
    }

    private void DeleteEntry(AuthenticatorEntry entry)
    {
        if (InvokeRequired)
        {
            Invoke(() => DeleteEntry(entry));
            return;
        }

        _payload.Entries.RemoveAll(item => item.Id == entry.Id);
        _codeHiddenOverrides.Remove(entry.Id);
        NormalizeSortOrder();
        MarkDirty(immediate: true);
        RegisterHotkeys(showErrors: false);
        RebuildCards();
        AntdUI.Message.success(this, L.Get("Notification.Entry.Deleted"), autoClose: 2);
    }

    private void MoveEntry(AuthenticatorEntry entry, int offset)
    {
        var ordered = _payload.Entries
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAtUtc)
            .ToList();
        var index = ordered.FindIndex(item => item.Id == entry.Id);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= ordered.Count)
        {
            return;
        }

        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        for (var position = 0; position < ordered.Count; position++)
        {
            ordered[position].SortOrder = position * 10;
        }

        _payload.Entries = ordered;
        MarkDirty();
        RebuildCards();
    }

    private void CopyCode(AuthenticatorEntry entry)
    {
        try
        {
            var code = OtpService.GetCode(entry, DateTimeOffset.UtcNow);
            _clipboard.Copy(code, _payload.Settings.ClipboardClearSeconds);
            AntdUI.Message.success(
                this,
                _payload.Settings.ClipboardClearSeconds > 0
                    ? L.Format(
                        "Notification.Code.CopiedWithClearDelay",
                        _payload.Settings.ClipboardClearSeconds)
                    : L.Get("Notification.Code.Copied"),
                autoClose: 2);
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException or ExternalException)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.Code.GenerationFailed", exception.Message),
                autoClose: 4);
        }
    }

    private void ShowRestoreCode(AuthenticatorEntry entry)
    {
        var restoreCode = OtpService.GetBattleNetRestoreCode(entry);
        if (string.IsNullOrWhiteSpace(restoreCode))
        {
            AntdUI.Message.error(
                this,
                L.Get("Error.RecoveryCode.MissingData"),
                autoClose: 4);
            return;
        }

        using var dialog = new SensitiveValueDialog(
            L.Get("RecoveryCode.Title"),
            L.Get("RecoveryCode.Warning"),
            restoreCode);
        dialog.ShowDialog(this);
    }

    private void SetFilter(DashboardFilter filter)
    {
        _filter = filter;
        RebuildCards();
    }

    private void UpdateFilterButtons()
    {
        var palette = ThemePaletteService.Current;
        StyleFilterButton(
            _filterAllButton,
            _filter == DashboardFilter.All,
            palette);
        StyleFilterButton(
            _filterFavoriteButton,
            _filter == DashboardFilter.Favorite,
            palette);
        StyleFilterButton(
            _filterTimeButton,
            _filter == DashboardFilter.TimeBased,
            palette);
        StyleFilterButton(
            _filterCounterButton,
            _filter == DashboardFilter.CounterBased,
            palette);
    }

    private void ShowDashboard()
    {
        if (_locked)
        {
            return;
        }

        _settingsVisible = false;
        _settingsView.Visible = false;
        _dashboard.Visible = true;
        _dashboard.BringToFront();
        UpdateNavigation();
    }

    private void ShowSettings()
    {
        if (!EnsureUnlocked())
        {
            return;
        }

        BuildSettings();
        _settingsVisible = true;
        _dashboard.Visible = false;
        _settingsView.Visible = true;
        _settingsView.BringToFront();
        UpdateNavigation();
    }

    private void BuildSettings()
    {
        _settingsFlow.SuspendLayout();
        _settingsTopMostSwitch = null;
        foreach (Control control in _settingsFlow.Controls)
        {
            control.Dispose();
        }

        _settingsFlow.Controls.Clear();
        var header = new Panel
        {
            Height = 92,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.Transparent
        };
        var title = new AntdUI.Label
        {
            Text = L.Get("Settings.Title"),
            Font = LocalizationFonts.Create(22F, FontStyle.Bold),
            ForeColor = ThemePaletteService.Current.Text,
            BackColor = Color.Transparent
        };
        var subtitle = new AntdUI.Label
        {
            Text = L.Get("Settings.Subtitle"),
            ForeColor = ThemePaletteService.Current.TextSecondary,
            BackColor = Color.Transparent
        };
        header.Controls.AddRange([title, subtitle]);
        header.Resize += (_, _) =>
        {
            title.SetBounds(2, 2, header.Width - 4, 46);
            subtitle.SetBounds(3, 50, header.Width - 6, 30);
        };
        _settingsFlow.Controls.Add(header);

        var themeSelect = new AntdUI.Select
        {
            Items =
            {
                L.Get("Settings.Theme.System"),
                L.Get("Settings.Theme.Light"),
                L.Get("Settings.Theme.Dark")
            },
            SelectedIndex = (int)_payload.Settings.Theme,
            Radius = 9,
            WheelModifyEnabled = false
        };
        themeSelect.SelectedIndexChanged += (_, _) =>
        {
            if (themeSelect.SelectedIndex < 0)
            {
                return;
            }

            _payload.Settings.Theme = (AppThemeMode)themeSelect.SelectedIndex;
            ThemePaletteService.Apply(_payload.Settings.Theme);
            ApplyTheme();
            MarkDirty();
            BeginInvoke(BuildSettings);
        };

        var languageChoices = new List<LanguageChoice>
        {
            new("system", L.Get("Settings.Language.System"))
        };
        languageChoices.AddRange(L.AvailableLanguages.Select(language =>
            new LanguageChoice(language.Code, language.DisplayName)));
        var selectedLanguageIndex = languageChoices.FindIndex(choice =>
            choice.Code.Equals(
                L.RequestedLanguage,
                StringComparison.OrdinalIgnoreCase));
        if (selectedLanguageIndex < 0)
        {
            selectedLanguageIndex = 0;
        }

        var languageSelect = new AntdUI.Select
        {
            Radius = 9,
            WheelModifyEnabled = false
        };
        languageSelect.Items.AddRange(languageChoices.Cast<object>().ToArray());
        languageSelect.SelectedIndex = selectedLanguageIndex;
        languageSelect.SelectedIndexChanged += (_, _) =>
        {
            if (languageSelect.SelectedIndex < 0 ||
                languageSelect.SelectedIndex >= languageChoices.Count)
            {
                return;
            }

            var language = languageChoices[languageSelect.SelectedIndex].Code;
            if (language.Equals(
                    L.RequestedLanguage,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!LanguagePreferenceStore.Save(language))
            {
                languageSelect.SelectedIndex = selectedLanguageIndex;
                AntdUI.Message.error(
                    this,
                    L.Get("Settings.Language.SaveFailed"),
                    autoClose: 7);
                return;
            }

            RequestApplicationRestart();
        };

        var compactSwitch = SettingsSwitch(_payload.Settings.CompactCards, value =>
        {
            _payload.Settings.CompactCards = value;
            RebuildCards();
            MarkDirty();
        });
        var hideCodesSwitch = SettingsSwitch(_payload.Settings.HideCodes, value =>
        {
            _payload.Settings.HideCodes = value;
            _codeHiddenOverrides.Clear();
            foreach (var card in _cardFlow.Controls.OfType<AuthenticatorCard>())
            {
                card.GlobalHideCodes = value;
            }

            UpdateToggleAllCodesButton();
            LayoutDashboard();
            MarkDirty();
        });
        AddSettingsSection(
            L.Get("Settings.Appearance.Title"),
            L.Get("Settings.Appearance.Description"),
            CreateSettingRow(
                L.Get("Settings.Language.Label"),
                L.Get("Settings.Language.Description"),
                languageSelect),
            CreateSettingRow(
                L.Get("Settings.Theme.Label"),
                L.Get("Settings.Theme.Description"),
                themeSelect),
            CreateSettingRow(
                L.Get("Settings.CompactCards.Label"),
                L.Get("Settings.CompactCards.Description"),
                compactSwitch),
            CreateSettingRow(
                L.Get("Settings.HideCodes.Label"),
                L.Get("Settings.HideCodes.Description"),
                hideCodesSwitch));

        var autoLockInput = NumericInput(
            _payload.Settings.AutoLockMinutes,
            L.Get("Common.Minutes"),
            AppSettings.MinimumAutoLockMinutes,
            AppSettings.MaximumAutoLockMinutes,
            SetAutoLockMinutes);
        autoLockInput.Name = "autoLockMinutesInput";
        autoLockInput.AccessibleName = L.Get("Settings.AutoLock.Label");
        var clipboardInput = NumericInput(
            _payload.Settings.ClipboardClearSeconds,
            L.Get("Common.Seconds"),
            0,
            3_600,
            value =>
        {
            _payload.Settings.ClipboardClearSeconds = value;
            MarkDirty();
        });
        clipboardInput.Name = "clipboardClearSecondsInput";
        clipboardInput.AccessibleName = L.Get("Settings.ClipboardClear.Label");
        var lockOnMinimizeSwitch = SettingsSwitch(_payload.Settings.LockOnMinimize, value =>
        {
            _payload.Settings.LockOnMinimize = value;
            MarkDirty();
        });
        AddSettingsSection(
            L.Get("Settings.Security.Title"),
            _vault.ProtectionMode switch
            {
                VaultProtectionMode.Password =>
                    L.Get("Settings.Security.PasswordProtected"),
                VaultProtectionMode.WindowsAccount =>
                    L.Get("Settings.Security.WindowsProtected"),
                _ => L.Get("Settings.Security.Portable")
            },
            CreateSettingRow(
                L.Get("Settings.AutoLock.Label"),
                L.Get("Settings.AutoLock.Description"),
                autoLockInput),
            CreateSettingRow(
                L.Get("Settings.ClipboardClear.Label"),
                L.Get("Settings.ClipboardClear.Description"),
                clipboardInput),
            CreateSettingRow(
                L.Get("Settings.LockOnMinimize.Label"),
                L.Get("Settings.LockOnMinimize.Description"),
                lockOnMinimizeSwitch),
            CreateVaultProtectionRow());

        var traySwitch = SettingsSwitch(_payload.Settings.MinimizeToTray, value =>
        {
            _payload.Settings.MinimizeToTray = value;
            MarkDirty();
        });
        var closeTraySwitch = SettingsSwitch(_payload.Settings.CloseToTray, value =>
        {
            _payload.Settings.CloseToTray = value;
            MarkDirty();
        });
        _settingsTopMostSwitch = SettingsSwitch(
            _payload.Settings.AlwaysOnTop,
            value =>
            {
                if (!_updatingTopMostControls)
                {
                    SetAlwaysOnTop(value);
                }
            });
        var rememberWindowLayoutSwitch = SettingsSwitch(
            _payload.Settings.RememberWindowLayout,
            SetRememberWindowLayout);
        var startSwitch = SettingsSwitch(_payload.Settings.StartWithWindows, SetStartWithWindows);
        var startMinimizedSwitch = SettingsSwitch(_payload.Settings.StartMinimized, value =>
        {
            _payload.Settings.StartMinimized = value;
            if (_payload.Settings.StartWithWindows)
            {
                SetStartWithWindows(true);
            }

            MarkDirty();
        });
        var confirmDeleteSwitch = SettingsSwitch(_payload.Settings.ConfirmBeforeDelete, value =>
        {
            _payload.Settings.ConfirmBeforeDelete = value;
            MarkDirty();
        });
        AddSettingsSection(
            L.Get("Settings.Behavior.Title"),
            L.Get("Settings.Behavior.Description"),
            CreateSettingRow(
                L.Get("Settings.MinimizeToTray.Label"),
                L.Get("Settings.MinimizeToTray.Description"),
                traySwitch),
            CreateSettingRow(
                L.Get("Settings.CloseToTray.Label"),
                L.Get("Settings.CloseToTray.Description"),
                closeTraySwitch),
            CreateSettingRow(
                L.Get("Main.AlwaysOnTop.Label"),
                L.Get("Settings.AlwaysOnTop.Description"),
                _settingsTopMostSwitch),
            CreateSettingRow(
                L.Get("Settings.RememberLayout.Label"),
                L.Get("Settings.RememberLayout.Description"),
                rememberWindowLayoutSwitch),
            CreateActionRow(
                L.Get("Settings.ResetLayout.Label"),
                L.Get("Settings.ResetLayout.Description"),
                (L.Get("Settings.ResetLayout.Action"), "ReloadOutlined", (Action)ResetWindowLayout)),
            CreateSettingRow(
                L.Get("Settings.StartWithWindows.Label"),
                L.Get("Settings.StartWithWindows.Description"),
                startSwitch),
            CreateSettingRow(
                L.Get("Settings.StartMinimized.Label"),
                L.Get("Settings.StartMinimized.Description"),
                startMinimizedSwitch),
            CreateSettingRow(
                L.Get("Settings.ConfirmDelete.Label"),
                L.Get("Settings.ConfirmDelete.Description"),
                confirmDeleteSwitch));

        AddSettingsSection(
            L.Get("Settings.Data.Title"),
            L.Get("Settings.Data.Description"),
            CreateActionRow(
                L.Get("Settings.Import.Label"),
                L.Get("Settings.Import.Description"),
                (L.Get("Settings.Import.Action"), "ImportOutlined", (Action)(() => ShowImportMenu(_navImport)))),
            CreateActionRow(
                L.Get("Settings.Export.Label"),
                L.Get("Settings.Export.Description"),
                (L.Get("Settings.Export.Action"), "ExportOutlined", (Action)(() => ShowExportMenu(_navExport)))),
            CreateActionRow(
                L.Get("Settings.DataDirectory.Label"),
                DataDirectoryDescription(),
                (L.Get("Common.Select"), "FolderOutlined", (Action)ChangeDataDirectory),
                (L.Get("Common.Open"), "FolderOpenOutlined", (Action)OpenDataDirectory)));

        _settingsFlow.ResumeLayout();
        LayoutSettingsSections();
    }

    private Control CreateVaultProtectionRow()
    {
        var passwordText = _vault.ProtectionMode == VaultProtectionMode.Password
            ? L.Get("Settings.VaultProtection.ChangePassword")
            : L.Get("Settings.VaultProtection.SetPassword");
        var protectionText = _vault.ProtectionMode switch
        {
            VaultProtectionMode.WindowsAccount =>
                L.Get("Settings.VaultProtection.DisableWindows"),
            VaultProtectionMode.Password =>
                L.Get("Settings.VaultProtection.RemovePassword"),
            _ => L.Get("Settings.VaultProtection.EnableWindows")
        };
        var minimumActionWidth = Math.Min(
            300,
            Math.Max(
                126,
                Math.Max(
                    TextRenderer.MeasureText(passwordText, Font).Width,
                    TextRenderer.MeasureText(protectionText, Font).Width) + 44));
        var actions = new ResponsiveWrapPanel
        {
            BackColor = Color.Transparent,
            MinimumItemWidth = minimumActionWidth,
            ItemHeight = 38,
            MaximumColumns = minimumActionWidth * 2 + 6 <= 300 ? 2 : 1,
            HorizontalSpacing = 6,
            VerticalSpacing = 6,
            Size = new Size(300, 38)
        };
        var passwordButton = new AntdUI.Button
        {
            Text = passwordText,
            IconSvg = "KeyOutlined",
            Radius = 9,
            Size = new Size(130, 38)
        };
        ThemePaletteService.StyleSecondaryButton(passwordButton);
        passwordButton.Click += (_, _) => ChangeVaultPassword();
        actions.Controls.Add(passwordButton);

        var protectionButton = new AntdUI.Button
        {
            Text = protectionText,
            IconSvg = _vault.ProtectionMode == VaultProtectionMode.Password
                ? "UnlockOutlined"
                : "WindowsOutlined",
            Radius = 9,
            Size = new Size(
                _vault.ProtectionMode == VaultProtectionMode.Password ? 126 : 158,
                38)
        };
        ThemePaletteService.StyleSecondaryButton(protectionButton);
        protectionButton.Click += (_, _) =>
        {
            if (_vault.ProtectionMode == VaultProtectionMode.Portable)
            {
                EnableWindowsAccountProtection();
            }
            else
            {
                UsePortableProtection();
            }
        };
        actions.Controls.Add(protectionButton);

        return CreateSettingRow(
            L.Get("Settings.VaultProtection.Label"),
            _vault.ProtectionMode switch
            {
                VaultProtectionMode.Password =>
                    L.Get("Settings.VaultProtection.PasswordDescription"),
                VaultProtectionMode.WindowsAccount =>
                    L.Get("Settings.VaultProtection.WindowsDescription"),
                _ => L.Get("Settings.VaultProtection.PortableDescription")
            },
            actions);
    }

    private void ChangeVaultPassword()
    {
        var password = PasswordDialog.Prompt(
            this,
            _vault.ProtectionMode == VaultProtectionMode.Password
                ? L.Get("Settings.VaultProtection.ChangePassword")
                : L.Get("Settings.VaultProtection.SetPassword"),
            L.Get("Settings.VaultProtection.PasswordPrompt"),
            requireConfirmation: true,
            submitText: L.Get("Settings.VaultProtection.SavePassword"));
        if (password is null)
        {
            return;
        }

        try
        {
            _vault.ChangePassword(_payload, password);
            UpdateVaultStatus();
            BuildSettings();
            AntdUI.Message.success(
                this,
                L.Get("Notification.Vault.PasswordUpdated"),
                autoClose: 3);
        }
        catch (Exception exception)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.Vault.PasswordUpdateFailed", exception.Message),
                autoClose: 5);
        }
    }

    private void EnableWindowsAccountProtection()
    {
        AntdUI.Modal.open(DialogSizing.MakeResizable(new AntdUI.Modal.Config(
            this,
            L.Get("Settings.VaultProtection.EnableWindowsTitle"),
            L.Get("Settings.VaultProtection.EnableWindowsConfirmation"))
        {
            Icon = AntdUI.TType.Warn,
            OkText = L.Get("Settings.VaultProtection.ConfirmEnable"),
            CancelText = L.Get("Common.Cancel"),
            OnOk = _ =>
            {
                try
                {
                    _vault.EnableWindowsAccountProtection(_payload);
                    UpdateVaultStatus();
                    BuildSettings();
                    AntdUI.Message.success(
                        this,
                        L.Get("Notification.Vault.WindowsProtectionEnabled"),
                        autoClose: 3);
                    return true;
                }
                catch (Exception exception)
                {
                    AntdUI.Message.error(
                        this,
                        L.Format("Error.Vault.ProtectionChangeFailed", exception.Message),
                        autoClose: 5);
                    return false;
                }
            }
        }));
    }

    private void UsePortableProtection()
    {
        var removingPassword = _vault.ProtectionMode == VaultProtectionMode.Password;
        AntdUI.Modal.open(DialogSizing.MakeResizable(new AntdUI.Modal.Config(
            this,
            removingPassword
                ? L.Get("Settings.VaultProtection.RemovePassword")
                : L.Get("Settings.VaultProtection.DisableWindowsTitle"),
            L.Get("Settings.VaultProtection.PortableConfirmation"))
        {
            Icon = AntdUI.TType.Warn,
            OkText = removingPassword
                ? L.Get("Settings.VaultProtection.RemoveAndSwitch")
                : L.Get("Settings.VaultProtection.ConfirmDisable"),
            CancelText = L.Get("Common.Cancel"),
            OnOk = _ =>
            {
                try
                {
                    _vault.UsePortableProtection(_payload);
                    UpdateVaultStatus();
                    BuildSettings();
                    AntdUI.Message.success(
                        this,
                        L.Get("Notification.Vault.PortableModeEnabled"),
                        autoClose: 3);
                    return true;
                }
                catch (Exception exception)
                {
                    AntdUI.Message.error(
                        this,
                        L.Format("Error.Vault.ProtectionChangeFailed", exception.Message),
                        autoClose: 5);
                    return false;
                }
            }
        }));
    }

    private void AddSettingsSection(string title, string description, params Control[] rows)
    {
        var section = new AntdUI.Panel
        {
            Height = 88 + rows.Sum(row => row.Height),
            Radius = 18,
            Shadow = 6,
            ShadowOffsetY = 2,
            Margin = new Padding(0, 0, 0, 16),
            Padding = new Padding(20, 16, 20, 16),
            Tag = "settings-section"
        };
        var titleLabel = new AntdUI.Label
        {
            Text = title,
            Font = LocalizationFonts.Create(12F, FontStyle.Bold),
            ForeColor = ThemePaletteService.Current.Text,
            BackColor = Color.Transparent
        };
        var descriptionLabel = new AntdUI.Label
        {
            Text = description,
            Font = LocalizationFonts.Create(8.5F),
            ForeColor = ThemePaletteService.Current.TextSecondary,
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        section.Controls.AddRange([titleLabel, descriptionLabel]);
        section.Controls.AddRange(rows);
        var updatingLayout = false;
        void LayoutSection()
        {
            if (updatingLayout || section.Width <= 0)
            {
                return;
            }

            updatingLayout = true;
            try
            {
                titleLabel.SetBounds(
                    ScaleLogical(20),
                    ScaleLogical(14),
                    Math.Max(1, section.Width - ScaleLogical(40)),
                    ScaleLogical(28));
                descriptionLabel.SetBounds(
                    ScaleLogical(20),
                    ScaleLogical(41),
                    Math.Max(1, section.Width - ScaleLogical(40)),
                    ScaleLogical(25));
                var top = ScaleLogical(72);
                foreach (var row in rows)
                {
                    row.SetBounds(
                        ScaleLogical(16),
                        top,
                        Math.Max(1, section.Width - ScaleLogical(32)),
                        row.Height);
                    row.PerformLayout();
                    top += row.Height;
                }

                var targetHeight = top + ScaleLogical(16);
                if (section.Height != targetHeight)
                {
                    section.Height = targetHeight;
                }
            }
            finally
            {
                updatingLayout = false;
            }
        }

        section.Resize += (_, _) => LayoutSection();
        foreach (var row in rows)
        {
            row.SizeChanged += (_, _) => LayoutSection();
        }

        _settingsFlow.Controls.Add(section);
    }

    private Control CreateSettingRow(string title, string description, Control editor)
    {
        var row = new Panel
        {
            Height = ScaleLogical(72),
            BackColor = Color.Transparent
        };
        var titleLabel = new AntdUI.Label
        {
            Text = title,
            Font = LocalizationFonts.Create(9.5F, FontStyle.Bold),
            ForeColor = ThemePaletteService.Current.Text,
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        var descriptionLabel = new AntdUI.Label
        {
            Text = description,
            Font = LocalizationFonts.Create(8.3F),
            ForeColor = ThemePaletteService.Current.TextSecondary,
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };
        row.Controls.AddRange([titleLabel, descriptionLabel, editor]);
        var updatingLayout = false;
        void LayoutRow()
        {
            if (updatingLayout || row.Width <= 0)
            {
                return;
            }

            updatingLayout = true;
            try
            {
                var stacked = ToLogical(row.Width) < 500;
                descriptionLabel.TextMultiLine = stacked;
                if (stacked)
                {
                    var fillEditor = editor is not AntdUI.Switch;
                    var editorWidth = fillEditor
                        ? Math.Max(1, row.Width - ScaleLogical(8))
                        : Math.Min(
                            ScaleLogical(54),
                            Math.Max(1, row.Width - ScaleLogical(8)));
                    var editorHeight = editor is ResponsiveWrapPanel
                        ? Math.Max(ScaleLogical(38), editor.Height)
                        : ScaleLogical(38);
                    editor.SetBounds(
                        fillEditor
                            ? ScaleLogical(4)
                            : Math.Max(ScaleLogical(4), row.Width - editorWidth - ScaleLogical(4)),
                        ScaleLogical(66),
                        editorWidth,
                        editorHeight);
                    editor.PerformLayout();
                    editorHeight = Math.Max(ScaleLogical(38), editor.Height);
                    if (editor.Height != editorHeight)
                    {
                        editor.Height = editorHeight;
                    }

                    titleLabel.SetBounds(
                        ScaleLogical(4),
                        ScaleLogical(2),
                        Math.Max(1, row.Width - ScaleLogical(8)),
                        ScaleLogical(26));
                    descriptionLabel.SetBounds(
                        ScaleLogical(4),
                        ScaleLogical(29),
                        Math.Max(1, row.Width - ScaleLogical(8)),
                        ScaleLogical(34));
                    var targetHeight =
                        ScaleLogical(76) + editorHeight;
                    if (row.Height != targetHeight)
                    {
                        row.Height = targetHeight;
                    }
                }
                else
                {
                    var editorWidth = Math.Clamp(
                        editor.Width <= 0 ? ScaleLogical(150) : editor.Width,
                        ScaleLogical(58),
                        ScaleLogical(310));
                    if (editor is ResponsiveWrapPanel)
                    {
                        editorWidth = Math.Min(
                            ScaleLogical(310),
                            Math.Max(ScaleLogical(180), row.Width / 2));
                    }

                    var editorTop = ScaleLogical(15);
                    editor.SetBounds(
                        row.Width - editorWidth,
                        editorTop,
                        editorWidth,
                        ScaleLogical(38));
                    editor.PerformLayout();
                    var editorHeight = Math.Max(
                        ScaleLogical(38),
                        editor.Height);
                    var labelWidth = Math.Max(
                        ScaleLogical(90),
                        editor.Left - ScaleLogical(16));
                    titleLabel.SetBounds(
                        ScaleLogical(4),
                        ScaleLogical(9),
                        labelWidth,
                        ScaleLogical(26));
                    descriptionLabel.SetBounds(
                        ScaleLogical(4),
                        ScaleLogical(36),
                        labelWidth,
                        ScaleLogical(25));
                    var targetHeight = Math.Max(
                        ScaleLogical(72),
                        editorTop + editorHeight + ScaleLogical(8));
                    if (row.Height != targetHeight)
                    {
                        row.Height = targetHeight;
                    }
                }
            }
            finally
            {
                updatingLayout = false;
            }
        }

        row.Resize += (_, _) => LayoutRow();
        editor.SizeChanged += (_, _) => LayoutRow();
        return row;
    }

    private Control CreateActionRow(
        string title,
        string description,
        params (string Text, string Icon, Action Action)[] actions)
    {
        var baseButtonWidth = actions.Length > 1 ? 84 : 126;
        var buttonWidth = Math.Min(
            300,
            Math.Max(
                baseButtonWidth,
                actions.Max(action =>
                    TextRenderer.MeasureText(action.Text, Font).Width + 44)));
        var actionPanel = new ResponsiveWrapPanel
        {
            BackColor = Color.Transparent,
            MinimumItemWidth = buttonWidth,
            ItemHeight = 38,
            MaximumColumns = Math.Max(1, actions.Length),
            HorizontalSpacing = 6,
            VerticalSpacing = 6,
            Size = new Size(
                Math.Min(310, Math.Max(126, actions.Length * (buttonWidth + 6))),
                38)
        };
        foreach (var action in actions)
        {
            var button = new AntdUI.Button
            {
                Text = action.Text,
                IconSvg = action.Icon,
                Radius = 9,
                Size = new Size(buttonWidth, 38)
            };
            ThemePaletteService.StyleSecondaryButton(button);
            button.Click += (_, _) => action.Action();
            actionPanel.Controls.Add(button);
        }

        return CreateSettingRow(title, description, actionPanel);
    }

    private void LayoutSettingsSections()
    {
        if (_settingsFlow.ClientSize.Width <= 0)
        {
            return;
        }

        // Reserve the scrollbar before the sections grow. Waiting for
        // VerticalScroll.Visible creates a transient width that can leave a
        // horizontal scrollbar behind after the first narrow layout pass.
        var scrollbarWidth = SystemInformation.VerticalScrollBarWidth;
        var availableWidth = Math.Max(
            1,
            _settingsFlow.ClientSize.Width -
            _settingsFlow.Padding.Horizontal -
            scrollbarWidth -
            ScaleLogical(4));
        var width = Math.Min(ScaleLogical(860), availableWidth);
        foreach (Control control in _settingsFlow.Controls)
        {
            control.Width = width;
        }
    }

    private AntdUI.Switch SettingsSwitch(bool value, Action<bool> changed)
    {
        var toggle = new AntdUI.Switch
        {
            Checked = value,
            Size = new Size(54, 30)
        };
        toggle.CheckedChanged += (_, eventArgs) => changed(eventArgs.Value);
        return toggle;
    }

    private AntdUI.Input NumericInput(
        int value,
        string unit,
        int minimum,
        int maximum,
        Action<int> changed)
    {
        var committedValue = Math.Clamp(value, minimum, maximum);
        var input = new AntdUI.Input
        {
            Text = committedValue.ToString(),
            SuffixText = unit,
            Radius = 9,
            Size = new Size(118, 38)
        };

        void CommitValue()
        {
            if (!int.TryParse(input.Text, out var parsed))
            {
                parsed = committedValue;
            }

            parsed = Math.Clamp(parsed, minimum, maximum);
            input.Text = parsed.ToString();
            if (parsed == committedValue)
            {
                return;
            }

            committedValue = parsed;
            changed(parsed);
        }

        input.LostFocus += (_, _) => CommitValue();
        input.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode != Keys.Enter)
            {
                return;
            }

            CommitValue();
            eventArgs.SuppressKeyPress = true;
        };
        return input;
    }

    private void ShowImportMenu(Control anchor)
    {
        if (!EnsureUnlocked())
        {
            return;
        }

        AntdUI.ContextMenuStrip.open(
            anchor,
            item =>
            {
                switch (item.Tag)
                {
                    case "file":
                        ImportConfigurationFile();
                        break;
                    case "qr":
                        ImportQrImage();
                        break;
                    case "uri":
                        ImportUriText();
                        break;
                }
            },
            [
                new AntdUI.ContextMenuStripItem(L.Get("Import.Menu.FileOrBackup"))
                {
                    IconSvg = "FileProtectOutlined",
                    Tag = "file"
                },
                new AntdUI.ContextMenuStripItem(L.Get("Import.Menu.QrImage"))
                {
                    IconSvg = "QrcodeOutlined",
                    Tag = "qr"
                },
                new AntdUI.ContextMenuStripItem(L.Get("Import.Menu.OtpUri"))
                {
                    IconSvg = "LinkOutlined",
                    Tag = "uri"
                }
            ],
            sleep: InteractionTiming.ContextMenuCloseDelayMilliseconds);
    }

    private void ShowExportMenu(Control anchor)
    {
        if (!EnsureUnlocked())
        {
            return;
        }

        AntdUI.ContextMenuStrip.open(
            anchor,
            item =>
            {
                switch (item.Tag)
                {
                    case "backup":
                        ExportEncryptedBackup();
                        break;
                    case "winauth-password":
                        ExportWinAuth(passwordProtected: true);
                        break;
                    case "winauth-plain":
                        ExportWinAuth(passwordProtected: false);
                        break;
                    case "uris":
                        ExportOtpUris();
                        break;
                }
            },
            [
                new AntdUI.ContextMenuStripItem(L.Get("Export.Menu.EncryptedBackup"))
                {
                    IconSvg = "SafetyCertificateOutlined",
                    Tag = "backup"
                },
                new AntdUI.ContextMenuStripItem(L.Get("Export.Menu.WinAuthPassword"))
                {
                    IconSvg = "FileProtectOutlined",
                    Tag = "winauth-password"
                },
                new AntdUI.ContextMenuStripItem(L.Get("Export.Menu.WinAuthPlain"))
                {
                    IconSvg = "FileOutlined",
                    Tag = "winauth-plain"
                },
                new AntdUI.ContextMenuStripItem(L.Get("Export.Menu.OtpUris"))
                {
                    IconSvg = "LinkOutlined",
                    Tag = "uris"
                }
            ],
            sleep: InteractionTiming.ContextMenuCloseDelayMilliseconds);
    }

    private void ImportConfigurationFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = L.Get("FileDialog.Import.Title"),
            Filter =
                $"{L.Get("FileFilter.SupportedConfigurations")} (*.xml;*.authdesk;*.json)|*.xml;*.authdesk;*.json|" +
                $"{L.Get("FileFilter.WinAuthXml")} (*.xml)|*.xml|" +
                $"{L.Get("FileFilter.AuthenticatorDeskBackup")} (*.authdesk;*.json)|*.authdesk;*.json|" +
                $"{L.Get("FileFilter.AllFiles")} (*.*)|*.*",
            Multiselect = false,
            InitialDirectory = ExistingDirectory(_payload.Settings.LastImportDirectory)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            IReadOnlyList<AuthenticatorEntry> entries;
            if (BackupService.IsBackupFile(dialog.FileName))
            {
                var password = PasswordDialog.Prompt(
                    this,
                    L.Get("Import.Backup.UnlockTitle"),
                    L.Get("Import.Backup.PasswordPrompt"),
                    submitText: L.Get("Common.Import"));
                if (password is null)
                {
                    return;
                }

                entries = BackupService.Import(dialog.FileName, password);
            }
            else
            {
                var info = WinAuthConfigService.Inspect(dialog.FileName);
                if (info.UsesYubiKey)
                {
                    throw new WinAuthCompatibilityException(
                        L.Get("Error.Import.WinAuthYubiKey"));
                }

                string? password = null;
                if (info.RequiresConfigurationPassword)
                {
                    password = PasswordDialog.Prompt(
                        this,
                        L.Get("Import.WinAuth.UnlockTitle"),
                        L.Get("Import.WinAuth.PasswordPrompt"),
                        submitText: L.Get("Common.Import"));
                    if (password is null)
                    {
                        return;
                    }
                }

                var result = WinAuthConfigService.Import(
                    dialog.FileName,
                    password,
                    entryName => PasswordDialog.Prompt(
                        this,
                        L.Format("Import.WinAuth.UnlockEntryTitle", entryName),
                        L.Get("Import.WinAuth.EntryPasswordPrompt"),
                        submitText: L.Get("Import.Continue")));
                entries = result.Entries;
                ApplyImportedWinAuthSettings(result.Settings);
            }

            _payload.Settings.LastImportDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            var (added, skipped) = MergeEntries(entries);
            MarkDirty(immediate: true);
            RegisterHotkeys(showErrors: true);
            RebuildCards();
            AntdUI.Message.success(
                this,
                skipped > 0
                    ? L.Format("Notification.Import.CompletedWithDuplicates", added, skipped)
                    : L.Format("Notification.Import.Completed", added),
                autoClose: 4);
        }
        catch (OperationCanceledException)
        {
            AntdUI.Message.info(this, L.Get("Notification.Import.Cancelled"), autoClose: 2);
        }
        catch (Exception exception)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.Import.Failed", exception.Message),
                autoClose: 7);
        }
    }

    private void ImportQrImage()
    {
        using var dialog = new OpenFileDialog
        {
            Title = L.Get("FileDialog.QrImage.Title"),
            Filter =
                $"{L.Get("FileFilter.ImageFiles")} (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|" +
                $"{L.Get("FileFilter.AllFiles")} (*.*)|*.*",
            InitialDirectory = ExistingDirectory(_payload.Settings.LastImportDirectory)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var value = QrCodeService.DecodeFile(dialog.FileName);
            var entries = OtpAuthUriService.Parse(value);
            _payload.Settings.LastImportDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            var (added, skipped) = MergeEntries(entries);
            MarkDirty(immediate: true);
            RebuildCards();
            AntdUI.Message.success(
                this,
                skipped > 0
                    ? L.Format("Notification.Import.CompletedWithDuplicates", added, skipped)
                    : L.Format("Notification.Import.Completed", added),
                autoClose: 4);
        }
        catch (Exception exception)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.Import.QrFailed", exception.Message),
                autoClose: 6);
        }
    }

    private void ImportUriText()
    {
        var text = TextImportDialog.Prompt(this);
        if (text is null)
        {
            return;
        }

        try
        {
            var entries = OtpAuthUriService.Parse(text);
            var (added, skipped) = MergeEntries(entries);
            MarkDirty(immediate: true);
            RebuildCards();
            AntdUI.Message.success(
                this,
                skipped > 0
                    ? L.Format("Notification.Import.CompletedWithDuplicates", added, skipped)
                    : L.Format("Notification.Import.Completed", added),
                autoClose: 4);
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException or ExternalException)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.Import.UriFailed", exception.Message),
                autoClose: 6);
        }
    }

    private void ExportEncryptedBackup()
    {
        var password = PasswordDialog.Prompt(
            this,
            L.Get("Export.Backup.CreateTitle"),
            L.Get("Export.Backup.PasswordPrompt"),
            requireConfirmation: true,
            submitText: L.Get("Common.Continue"));
        if (password is null)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = L.Get("FileDialog.BackupSave.Title"),
            Filter =
                $"{L.Get("FileFilter.AuthenticatorDeskEncryptedBackup")} (*.authdesk)|*.authdesk",
            FileName = $"AuthenticatorDesk-{DateTime.Now:yyyyMMdd-HHmm}.authdesk",
            AddExtension = true,
            DefaultExt = "authdesk",
            InitialDirectory = ExistingDirectory(_payload.Settings.LastExportDirectory)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            BackupService.Export(dialog.FileName, _payload.Entries, password);
            _payload.Settings.LastExportDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            MarkDirty();
            AntdUI.Message.success(
                this,
                L.Get("Notification.Backup.Created"),
                autoClose: 3);
        }
        catch (Exception exception)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.Export.BackupFailed", exception.Message),
                autoClose: 6);
        }
    }

    private void ExportWinAuth(bool passwordProtected)
    {
        string? password = null;
        if (passwordProtected)
        {
            password = PasswordDialog.Prompt(
                this,
                L.Get("Export.WinAuth.PasswordTitle"),
                L.Get("Export.WinAuth.PasswordPrompt"),
                requireConfirmation: true,
                submitText: L.Get("Common.Continue"));
            if (password is null)
            {
                return;
            }
        }

        using var dialog = new SaveFileDialog
        {
            Title = L.Get("FileDialog.WinAuthSave.Title"),
            Filter = $"{L.Get("FileFilter.WinAuthXml")} (*.xml)|*.xml",
            FileName = $"winauth-{DateTime.Now:yyyyMMdd-HHmm}.xml",
            AddExtension = true,
            DefaultExt = "xml",
            InitialDirectory = ExistingDirectory(_payload.Settings.LastExportDirectory)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            WinAuthConfigService.Export(dialog.FileName, _payload.Entries, _payload.Settings, password);
            _payload.Settings.LastExportDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            MarkDirty();
            AntdUI.Message.success(
                this,
                passwordProtected
                    ? L.Get("Notification.Export.WinAuthPassword")
                    : L.Get("Notification.Export.WinAuthPlain"),
                autoClose: 4);
        }
        catch (Exception exception)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.Export.WinAuthFailed", exception.Message),
                autoClose: 6);
        }
    }

    private void ExportOtpUris()
    {
        using var dialog = new SaveFileDialog
        {
            Title = L.Get("FileDialog.OtpUrisSave.Title"),
            Filter = $"{L.Get("FileFilter.TextFiles")} (*.txt)|*.txt",
            FileName = $"otp-uris-{DateTime.Now:yyyyMMdd-HHmm}.txt",
            AddExtension = true,
            DefaultExt = "txt",
            InitialDirectory = ExistingDirectory(_payload.Settings.LastExportDirectory)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var text = string.Join(
                Environment.NewLine,
                _payload.Entries
                    .OrderBy(entry => entry.SortOrder)
                    .Select(entry => OtpAuthUriService.Build(entry, includeProviderData: true)));
            File.WriteAllText(dialog.FileName, text, new UTF8Encoding(false));
            _payload.Settings.LastExportDirectory = Path.GetDirectoryName(dialog.FileName) ?? string.Empty;
            MarkDirty();
            AntdUI.Message.warn(
                this,
                L.Get("Notification.Export.OtpUrisWarning"),
                autoClose: 5);
        }
        catch (Exception exception)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.Export.OtpUrisFailed", exception.Message),
                autoClose: 6);
        }
    }

    private (int Added, int Skipped) MergeEntries(IEnumerable<AuthenticatorEntry> imported)
    {
        var existing = new HashSet<string>(
            _payload.Entries.Select(IdentityKey),
            StringComparer.OrdinalIgnoreCase);
        var nextSort = _payload.Entries.Count == 0
            ? 0
            : _payload.Entries.Max(entry => entry.SortOrder) + 10;
        var added = 0;
        var skipped = 0;

        foreach (var source in imported)
        {
            OtpService.ValidateForCodeGeneration(source);
            var key = IdentityKey(source);
            if (!existing.Add(key))
            {
                skipped++;
                continue;
            }

            var entry = source.Clone();
            entry.Id = Guid.NewGuid();
            entry.SortOrder = nextSort;
            entry.ModifiedAtUtc = DateTime.UtcNow;
            nextSort += 10;
            _payload.Entries.Add(entry);
            added++;
        }

        return (added, skipped);
    }

    private static string IdentityKey(AuthenticatorEntry entry)
    {
        return $"{entry.Kind}:{Base32Encoding.Normalize(entry.Secret)}";
    }

    private void ApplyImportedWinAuthSettings(AppSettings imported)
    {
        _payload.Settings.MinimizeToTray = imported.MinimizeToTray;
        _payload.Settings.StartWithWindows = imported.StartWithWindows;
        if (imported.StartWithWindows)
        {
            try
            {
                StartupService.SetEnabled(true, _payload.Settings.StartMinimized);
            }
            catch
            {
                // The entries are still migrated even if Windows startup registration is unavailable.
            }
        }
    }

    private void SetAlwaysOnTop(bool value, bool persist = true)
    {
        var changed = _payload.Settings.AlwaysOnTop != value;
        _payload.Settings.AlwaysOnTop = value;
        if (TopMost != value)
        {
            TopMost = value;
        }

        _updatingTopMostControls = true;
        try
        {
            if (_topMostCheckBox.Checked != value)
            {
                _topMostCheckBox.Checked = value;
            }

            if (_settingsTopMostSwitch is { IsDisposed: false } &&
                _settingsTopMostSwitch.Checked != value)
            {
                _settingsTopMostSwitch.Checked = value;
            }
        }
        finally
        {
            _updatingTopMostControls = false;
        }

        _topMostCheckBox.ForeColor = value
            ? ThemePaletteService.Current.Primary
            : ThemePaletteService.Current.TextSecondary;
        if (_locked && changed)
        {
            _topMostChangedWhileLocked = true;
        }

        if (persist && changed)
        {
            MarkDirty();
        }
    }

    private bool LockVault()
    {
        if (_locked)
        {
            return true;
        }

        if (!SaveNow(showError: true))
        {
            _lastActivityUtc = DateTime.UtcNow;
            AntdUI.Message.warn(
                this,
                L.Get("Error.Vault.LockCancelledUnsaved"),
                autoClose: 6);
            return false;
        }

        _hotkeys.UnregisterAll();
        _clipboard.ClearNow();
        _vault.Lock();
        var retainedSettings = _payload.Settings;
        foreach (var entry in _payload.Entries)
        {
            entry.Secret = string.Empty;
            entry.ProviderData.Clear();
        }

        _codeHiddenOverrides.Clear();
        _payload = new VaultPayload { Settings = retainedSettings };
        _topMostChangedWhileLocked = false;
        _locked = true;
        _settingsVisible = false;
        _dashboard.Visible = false;
        _settingsView.Visible = false;
        _lockView.Visible = true;
        _lockView.BringToFront();
        _lockButton.IconSvg = "UnlockOutlined";
        _lockButton.Tag = L.Get("Vault.Unlock");
        UpdateToggleAllCodesButton();
        UpdateNavigation();
        UpdateVaultStatus();
        LayoutShell();
        return true;
    }

    private bool UnlockVault()
    {
        if (!_locked)
        {
            return true;
        }

        var retainedAlwaysOnTop = _payload.Settings.AlwaysOnTop;
        while (true)
        {
            VaultProtectionMode? inspectedMode = null;
            try
            {
                string? password = null;
                inspectedMode = _vault.InspectProtectionMode();
                if (inspectedMode == VaultProtectionMode.Password)
                {
                    password = PasswordDialog.Prompt(
                        this,
                        L.Get("Vault.Unlock"),
                        L.Get("Vault.Unlock.PasswordPrompt"),
                        submitText: L.Get("Common.Unlock"));
                    if (password is null)
                    {
                        return false;
                    }
                }

                _payload = _vault.Open(password);
                break;
            }
            catch (VaultAuthenticationException exception)
            {
                AntdUI.Message.error(this, exception.Message, autoClose: 5);
                if (inspectedMode != VaultProtectionMode.Password)
                {
                    return false;
                }
            }
        }

        var persistRetainedAlwaysOnTop =
            _topMostChangedWhileLocked &&
            _payload.Settings.AlwaysOnTop != retainedAlwaysOnTop;
        if (_topMostChangedWhileLocked)
        {
            _payload.Settings.AlwaysOnTop = retainedAlwaysOnTop;
        }

        _topMostChangedWhileLocked = false;
        _locked = false;
        _lastActivityUtc = DateTime.UtcNow;
        ThemePaletteService.Apply(_payload.Settings.Theme);
        ApplyTheme();
        SetAlwaysOnTop(_payload.Settings.AlwaysOnTop, persist: false);
        _lockView.Visible = false;
        _dashboard.Visible = true;
        _dashboard.BringToFront();
        _lockButton.IconSvg = "LockOutlined";
        _lockButton.Tag = L.Get("Main.Vault.LockTooltip");
        RebuildCards();
        RegisterHotkeys(showErrors: true);
        UpdateNavigation();
        UpdateVaultStatus();
        if (persistRetainedAlwaysOnTop)
        {
            MarkDirty();
        }

        return true;
    }

    private bool EnsureUnlocked()
    {
        return !_locked || UnlockVault();
    }

    private void CheckIdleLock()
    {
        if (_locked || _payload.Settings.AutoLockMinutes <= 0)
        {
            return;
        }

        if (DateTime.UtcNow - _lastActivityUtc >=
            TimeSpan.FromMinutes(_payload.Settings.AutoLockMinutes))
        {
            LockVault();
        }
    }

    private void SetAutoLockMinutes(int value)
    {
        var normalized = Math.Clamp(
            value,
            AppSettings.MinimumAutoLockMinutes,
            AppSettings.MaximumAutoLockMinutes);
        _lastActivityUtc = DateTime.UtcNow;
        if (_payload.Settings.AutoLockMinutes == normalized)
        {
            return;
        }

        _payload.Settings.AutoLockMinutes = normalized;
        MarkDirty(immediate: true);
    }

    private void RegisterHotkeys(bool showErrors)
    {
        if (_locked || !IsHandleCreated)
        {
            return;
        }

        var errors = _hotkeys.RegisterAll(Handle, _payload.Entries);
        if (showErrors && errors.Count > 0)
        {
            AntdUI.Message.warn(
                this,
                errors.Count == 1
                    ? L.Format(
                        "Error.Hotkey.RegistrationFailed",
                        errors[0].Hotkey,
                        errors[0].Message)
                    : L.Format(
                        "Error.Hotkey.MultipleRegistrationFailed",
                        errors.Count),
                autoClose: 6);
        }
    }

    private void HandleGlobalHotkey(Guid entryId)
    {
        if (_locked)
        {
            ShowFromTray();
            UnlockVault();
            return;
        }

        var entry = _payload.Entries.FirstOrDefault(item => item.Id == entryId);
        if (entry is null)
        {
            return;
        }

        try
        {
            var code = OtpService.GetCode(entry, DateTimeOffset.UtcNow);
            switch (entry.HotkeyAction)
            {
                case EntryHotkeyAction.Copy:
                    _clipboard.Copy(code, _payload.Settings.ClipboardClearSeconds);
                    _trayIcon.BalloonTipTitle = entry.DisplayName;
                    _trayIcon.BalloonTipText = L.Get("Tray.Notification.CodeCopied");
                    _trayIcon.ShowBalloonTip(2_000);
                    break;
                case EntryHotkeyAction.Type:
                    SendKeys.SendWait(code);
                    break;
                case EntryHotkeyAction.Show:
                    _trayIcon.BalloonTipTitle = entry.DisplayName;
                    _trayIcon.BalloonTipText = OtpService.FormatCode(code);
                    _trayIcon.ShowBalloonTip(5_000);
                    break;
            }
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException or ExternalException)
        {
            _trayIcon.BalloonTipTitle =
                L.Get("Tray.Notification.GenerationFailedTitle");
            _trayIcon.BalloonTipText = exception.Message;
            _trayIcon.ShowBalloonTip(4_000);
        }
    }

    private void ToggleTheme()
    {
        _payload.Settings.Theme = _payload.Settings.Theme switch
        {
            AppThemeMode.Dark => AppThemeMode.Light,
            AppThemeMode.Light => AppThemeMode.Dark,
            _ => ThemePaletteService.Current.Dark
                ? AppThemeMode.Light
                : AppThemeMode.Dark
        };
        ThemePaletteService.Apply(_payload.Settings.Theme);
        ApplyTheme();
        if (_settingsVisible)
        {
            BuildSettings();
        }

        MarkDirty();
    }

    private void ApplyTheme()
    {
        var palette = ThemePaletteService.Current;
        BackColor = palette.Canvas;
        _shell.BackColor = palette.Canvas;
        _contentHost.BackColor = palette.Canvas;
        _dashboard.BackColor = palette.Canvas;
        _settingsView.BackColor = palette.Canvas;
        _lockView.BackColor = palette.Canvas;
        _filterBar.BackColor = palette.Canvas;
        _titleBar.BackColor = palette.Surface;
        _titleBar.ForeColor = palette.Text;
        _titleBar.DividerColor = palette.Border;
        _sidebar.Back = palette.SurfaceElevated;
        _sidebar.BorderColor = palette.Border;
        _vaultStatusPanel.Back = palette.Dark ? palette.SurfaceMuted : palette.Surface;
        _vaultStatusPanel.BorderColor = palette.Border;
        _dashboardHero.Back = palette.Surface;
        _dashboardHero.BorderWidth = 1;
        _dashboardHero.BorderColor = palette.Border;
        _dashboardHero.ShadowColor = palette.Dark ? Color.Black : Color.FromArgb(63, 78, 116);
        _lockCard.Back = palette.Surface;
        _lockCard.BorderWidth = 1;
        _lockCard.BorderColor = palette.Border;
        _lockCard.ShadowColor = palette.Dark ? Color.Black : Color.FromArgb(63, 78, 116);

        _brandTitle.ForeColor = palette.Text;
        _brandSubtitle.ForeColor = palette.TextSecondary;
        _vaultStatus.ForeColor = palette.TextSecondary;
        _heroEyebrow.ForeColor = palette.Primary;
        _heroTitle.ForeColor = palette.Text;
        _heroSubtitle.ForeColor = palette.TextSecondary;
        _resultLabel.ForeColor = palette.TextSecondary;
        _resultLabel.BackColor = palette.Canvas;
        _emptyIcon.ForeColor = palette.Primary;
        _emptyTitle.ForeColor = palette.Text;
        _emptyText.ForeColor = palette.TextSecondary;
        _lockGlyph.ForeColor = palette.Primary;
        _lockTitle.ForeColor = palette.Text;
        _lockDescription.ForeColor = palette.TextSecondary;
        _topMostCheckBox.ForeColor = _payload.Settings.AlwaysOnTop
            ? palette.Primary
            : palette.TextSecondary;
        _themeButton.Toggle = palette.Dark;

        _searchInput.BackColor = palette.Surface;
        _searchInput.ForeColor = palette.Text;
        _searchInput.PlaceholderColor = palette.TextSecondary;
        _searchInput.PrefixFore = palette.TextSecondary;
        _searchInput.BorderColor = palette.Border;
        _searchInput.BorderHover = palette.Primary;
        _searchInput.BorderActive = palette.Primary;

        foreach (var button in new[] { _themeButton, _aboutButton, _lockButton })
        {
            var highContrast = SystemInformation.HighContrast;
            var hoverBackground = highContrast ? SystemColors.Highlight : palette.PrimarySoft;
            var normalForeground = palette.TextSecondary;
            var activeForeground = highContrast ? SystemColors.HighlightText : palette.Primary;
            button.Ghost = false;
            button.BackColor = palette.Surface;
            button.DefaultBack = palette.Surface;
            button.DefaultBorderColor = Color.Transparent;
            button.BorderWidth = 0;
            button.BackHover = hoverBackground;
            button.BackActive = hoverBackground;
            button.ForeColor = normalForeground;
            button.ForeHover = activeForeground;
            button.ForeActive = activeForeground;
        }

        _themeButton.ToggleBack = palette.Surface;
        _themeButton.ToggleBackHover = SystemInformation.HighContrast
            ? SystemColors.Highlight
            : palette.PrimarySoft;
        _themeButton.ToggleBackActive = _themeButton.ToggleBackHover;
        _themeButton.ToggleFore = palette.TextSecondary;
        _themeButton.ToggleForeHover = SystemInformation.HighContrast
            ? SystemColors.HighlightText
            : palette.Primary;
        _themeButton.ToggleForeActive = _themeButton.ToggleForeHover;

        ThemePaletteService.StyleSecondaryButton(_importButton);
        StyleToggleAllCodesButton(palette);
        foreach (var card in _cardFlow.Controls.OfType<AuthenticatorCard>())
        {
            card.ApplyTheme();
        }

        foreach (var section in _settingsFlow.Controls.OfType<AntdUI.Panel>())
        {
            section.Back = palette.Surface;
            section.BorderWidth = 1;
            section.BorderColor = palette.Border;
            section.ShadowColor = palette.Dark ? Color.Black : Color.FromArgb(74, 82, 128);
        }

        ApplyTrayMenuTheme(palette);
        UpdateNavigation();
        UpdateFilterButtons();
        _themeEnvironmentSignature = GetThemeEnvironmentSignature();
        Invalidate(true);
    }

    private void StyleToggleAllCodesButton(ThemePalette palette)
    {
        var highContrast = SystemInformation.HighContrast;
        var normalBackground = highContrast
            ? SystemColors.Control
            : palette.SurfaceMuted;
        var normalForeground = highContrast
            ? SystemColors.ControlText
            : palette.Text;
        var hoverBackground = highContrast
            ? SystemColors.Highlight
            : palette.Primary;
        var hoverForeground = highContrast
            ? SystemColors.HighlightText
            : palette.Dark
                ? palette.Canvas
                : Color.White;

        _toggleAllCodesButton.AutoToggle = false;
        _toggleAllCodesButton.Toggle = false;
        _toggleAllCodesButton.Ghost = false;
        _toggleAllCodesButton.BackColor = normalBackground;
        _toggleAllCodesButton.DefaultBack = normalBackground;
        _toggleAllCodesButton.DefaultBorderColor = highContrast
            ? SystemColors.WindowText
            : palette.Border;
        _toggleAllCodesButton.BorderWidth = 1F;
        _toggleAllCodesButton.ForeColor = normalForeground;
        _toggleAllCodesButton.BackHover = hoverBackground;
        _toggleAllCodesButton.BackActive = hoverBackground;
        _toggleAllCodesButton.ForeHover = hoverForeground;
        _toggleAllCodesButton.ForeActive = hoverForeground;

        // Define both palettes so AntdUI cannot fall back to a low-contrast
        // selected-state color when the button receives pointer or key focus.
        _toggleAllCodesButton.ToggleBack = normalBackground;
        _toggleAllCodesButton.ToggleBackHover = hoverBackground;
        _toggleAllCodesButton.ToggleBackActive = hoverBackground;
        _toggleAllCodesButton.ToggleFore = normalForeground;
        _toggleAllCodesButton.ToggleForeHover = hoverForeground;
        _toggleAllCodesButton.ToggleForeActive = hoverForeground;
    }

    private void OnSystemPreferenceChanged(
        object? sender,
        UserPreferenceChangedEventArgs eventArgs)
    {
        if (eventArgs.Category is not (
                UserPreferenceCategory.Accessibility or
                UserPreferenceCategory.Color or
                UserPreferenceCategory.General or
                UserPreferenceCategory.VisualStyle) ||
            IsDisposed ||
            Disposing ||
            !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(RefreshThemeFromSystem);
        }
        catch (InvalidOperationException)
        {
            // The window may be closing while Windows broadcasts the preference change.
        }
    }

    private void RefreshThemeFromSystem()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (GetThemeEnvironmentSignature() == _themeEnvironmentSignature)
        {
            return;
        }

        var openDialog = Application.OpenForms
            .Cast<Form>()
            .FirstOrDefault(form =>
                form != this &&
                form.Visible &&
                !form.IsDisposed);
        if (openDialog is not null)
        {
            if (_themeBlockingDialog != openDialog)
            {
                if (_themeBlockingDialog is not null)
                {
                    _themeBlockingDialog.FormClosed -= OnThemeBlockingDialogClosed;
                }

                _themeBlockingDialog = openDialog;
                _themeBlockingDialog.FormClosed += OnThemeBlockingDialogClosed;
            }

            return;
        }

        _themeBlockingDialog = null;
        ThemePaletteService.Apply(_payload.Settings.Theme);
        ApplyTheme();
        if (_settingsVisible)
        {
            BuildSettings();
        }
    }

    private int GetThemeEnvironmentSignature()
    {
        var hash = new HashCode();
        hash.Add(_payload.Settings.Theme);
        hash.Add(SystemInformation.HighContrast);
        hash.Add(ThemePaletteService.IsSystemDark());
        hash.Add(SystemColors.Window.ToArgb());
        hash.Add(SystemColors.WindowText.ToArgb());
        hash.Add(SystemColors.Control.ToArgb());
        hash.Add(SystemColors.ControlText.ToArgb());
        hash.Add(SystemColors.Highlight.ToArgb());
        hash.Add(SystemColors.HighlightText.ToArgb());
        return hash.ToHashCode();
    }

    private void OnThemeBlockingDialogClosed(object? sender, FormClosedEventArgs eventArgs)
    {
        if (sender is Form dialog)
        {
            dialog.FormClosed -= OnThemeBlockingDialogClosed;
        }

        _themeBlockingDialog = null;
        if (!IsDisposed && !Disposing && IsHandleCreated)
        {
            BeginInvoke(RefreshThemeFromSystem);
        }
    }

    private void ApplyTrayMenuTheme(ThemePalette palette)
    {
        var menu = _trayIcon.ContextMenuStrip;
        if (menu is null)
        {
            return;
        }

        if (SystemInformation.HighContrast)
        {
            menu.BackColor = SystemColors.Menu;
            menu.ForeColor = SystemColors.MenuText;
            menu.Renderer = new ToolStripSystemRenderer();
            foreach (ToolStripItem item in menu.Items)
            {
                item.BackColor = SystemColors.Menu;
                item.ForeColor = SystemColors.MenuText;
            }

            return;
        }

        menu.BackColor = palette.SurfaceElevated;
        menu.ForeColor = palette.Text;
        menu.Renderer = new ToolStripProfessionalRenderer(new AppToolStripColorTable(palette));
        foreach (ToolStripItem item in menu.Items)
        {
            item.BackColor = palette.SurfaceElevated;
            item.ForeColor = palette.Text;
        }
    }

    private void UpdateNavigation()
    {
        var palette = ThemePaletteService.Current;
        foreach (var button in new[] { _navVault, _navImport, _navExport, _navSettings })
        {
            StyleNavigationButton(button, selected: false, palette);
            button.Enabled = !_locked || button == _navVault;
        }

        var selected = _settingsVisible ? _navSettings : _navVault;
        StyleNavigationButton(selected, selected: true, palette);
        _navVault.Enabled = !_locked;
        _navImport.Enabled = !_locked;
        _navExport.Enabled = !_locked;
        _navSettings.Enabled = !_locked;
    }

    private static void StyleNavigationButton(
        AntdUI.Button button,
        bool selected,
        ThemePalette palette)
    {
        if (SystemInformation.HighContrast)
        {
            StyleHighContrastChoiceButton(button, selected);
            return;
        }

        button.Type = AntdUI.TTypeMini.Default;
        button.Ghost = false;
        button.BorderWidth = 0;
        button.DefaultBorderColor = Color.Transparent;
        button.BackColor = selected ? palette.PrimarySoft : Color.Transparent;
        button.DefaultBack = selected ? palette.PrimarySoft : Color.Transparent;
        button.BackHover = selected ? palette.PrimarySoft : palette.SurfaceMuted;
        button.BackActive = palette.PrimarySoft;
        button.ForeColor = selected ? palette.Primary : palette.TextSecondary;
        button.ForeHover = selected ? palette.Primary : palette.Text;
        button.ForeActive = palette.Primary;
    }

    private static void StyleFilterButton(
        AntdUI.Button button,
        bool selected,
        ThemePalette palette)
    {
        if (SystemInformation.HighContrast)
        {
            StyleHighContrastChoiceButton(button, selected);
            return;
        }

        button.Type = AntdUI.TTypeMini.Default;
        button.Ghost = false;
        button.BorderWidth = 0;
        button.DefaultBorderColor = Color.Transparent;
        button.BackColor = selected ? palette.PrimarySoft : Color.Transparent;
        button.DefaultBack = selected ? palette.PrimarySoft : Color.Transparent;
        button.BackHover = selected ? palette.PrimarySoft : palette.SurfaceMuted;
        button.BackActive = palette.PrimarySoft;
        button.ForeColor = selected ? palette.Primary : palette.TextSecondary;
        button.ForeHover = selected ? palette.Primary : palette.Text;
        button.ForeActive = palette.Primary;
    }

    private static void StyleHighContrastChoiceButton(
        AntdUI.Button button,
        bool selected)
    {
        button.Type = AntdUI.TTypeMini.Default;
        button.Ghost = false;
        button.BorderWidth = selected ? 1F : 0F;
        button.DefaultBorderColor = selected
            ? SystemColors.HighlightText
            : Color.Transparent;
        button.BackColor = selected ? SystemColors.Highlight : SystemColors.Control;
        button.DefaultBack = selected ? SystemColors.Highlight : SystemColors.Control;
        button.BackHover = SystemColors.Highlight;
        button.BackActive = SystemColors.Highlight;
        button.ForeColor = selected ? SystemColors.HighlightText : SystemColors.ControlText;
        button.ForeHover = SystemColors.HighlightText;
        button.ForeActive = SystemColors.HighlightText;
    }

    private void UpdateVaultStatus()
    {
        _vaultStatus.Text = _locked
            ? L.Get("Vault.Status.Locked")
            : _vault.ProtectionMode switch
            {
                VaultProtectionMode.Password =>
                    L.Get("Vault.Status.PasswordProtected"),
                VaultProtectionMode.WindowsAccount =>
                    L.Get("Vault.Status.WindowsProtected"),
                _ => L.Get("Vault.Status.Portable")
            };
        _vaultStatus.ForeColor = _locked
            ? ThemePaletteService.Current.Warning
            : _vault.ProtectionMode == VaultProtectionMode.Portable
                ? ThemePaletteService.Current.Warning
                : ThemePaletteService.Current.Success;
    }

    private void MarkDirty(bool immediate = false)
    {
        if (_locked || !_vault.IsOpen)
        {
            return;
        }

        _saveTimer.Stop();
        if (immediate)
        {
            SaveNow(showError: true);
        }
        else
        {
            _saveTimer.Start();
        }
    }

    private bool SaveNow(bool showError)
    {
        if (_locked || !_vault.IsOpen)
        {
            return false;
        }

        try
        {
            _saveTimer.Stop();
            _vault.Save(_payload);
            return true;
        }
        catch (Exception exception)
        {
            if (showError)
            {
                AntdUI.Message.error(
                    this,
                    L.Format("Error.Vault.SaveFailed", exception.Message),
                    autoClose: 6);
            }

            return false;
        }
    }

    private void CaptureWindowBounds()
    {
        var settings = _payload.Settings;
        if (_locked ||
            !_vault.IsOpen ||
            _updatingWindowState ||
            !settings.RememberWindowLayout)
        {
            return;
        }

        var maximized = WindowState == FormWindowState.Maximized;
        var bounds = maximized
            ? RestoreBounds
            : WindowState == FormWindowState.Normal
                ? Bounds
                : Rectangle.Empty;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        settings.WindowLeft = bounds.Left;
        settings.WindowTop = bounds.Top;
        settings.WindowWidth = bounds.Width;
        settings.WindowHeight = bounds.Height;
        settings.WindowDpi = Math.Max(1, DeviceDpi);
        settings.WindowMaximized = maximized;
        MarkDirty();
    }

    private void RestoreWindowBounds()
    {
        var settings = _payload.Settings;
        var rememberWindowLayout = settings.RememberWindowLayout;
        var storedBounds = new Rectangle(
            rememberWindowLayout ? settings.WindowLeft : -1,
            rememberWindowLayout ? settings.WindowTop : -1,
            rememberWindowLayout
                ? Math.Max(1, settings.WindowWidth)
                : AppSettings.DefaultWindowWidth,
            rememberWindowLayout
                ? Math.Max(1, settings.WindowHeight)
                : AppSettings.DefaultWindowHeight);
        var hasStoredLocation =
            rememberWindowLayout &&
            (settings.WindowLeft != -1 ||
             settings.WindowTop != -1);
        var intersections = Screen.AllScreens
            .Select(screen => new
            {
                Screen = screen,
                Intersection = Rectangle.Intersect(screen.WorkingArea, storedBounds)
            })
            .OrderByDescending(item =>
                (long)item.Intersection.Width * item.Intersection.Height)
            .ToArray();
        var best = intersections.FirstOrDefault();
        var visible =
            hasStoredLocation &&
            best is not null &&
            best.Intersection.Width >= 120 &&
            best.Intersection.Height >= 80;
        var targetScreen = visible
            ? best!.Screen
            : hasStoredLocation
                ? Screen.FromRectangle(storedBounds) ?? Screen.PrimaryScreen
                : Screen.PrimaryScreen;
        var working = targetScreen?.WorkingArea ??
                      new Rectangle(0, 0, 1200, 800);
        var targetDpi = WindowDpi.ForScreen(targetScreen);
        // Defaults are logical pixels. When layout memory is enabled, a zero
        // DPI identifies legacy data; preserve its old device-pixel size until
        // the next normal capture upgrades it.
        var storedDpi = !rememberWindowLayout
            ? WindowDpi.LogicalDpi
            : settings.WindowDpi is >= 48 and <= 768
                ? settings.WindowDpi
                : targetDpi;
        var logicalSize = new Size(
            Math.Max(
                MinimumSize.Width,
                WindowDpi.Convert(
                    storedBounds.Width,
                    storedDpi,
                    WindowDpi.LogicalDpi)),
            Math.Max(
                MinimumSize.Height,
                WindowDpi.Convert(
                    storedBounds.Height,
                    storedDpi,
                    WindowDpi.LogicalDpi)));
        var bounds = new Rectangle(
            storedBounds.Location,
            new Size(
                WindowDpi.Convert(
                    logicalSize.Width,
                    WindowDpi.LogicalDpi,
                    targetDpi),
                WindowDpi.Convert(
                    logicalSize.Height,
                    WindowDpi.LogicalDpi,
                    targetDpi)));
        if (!visible)
        {
            bounds.Size = new Size(
                Math.Min(bounds.Width, working.Width),
                Math.Min(bounds.Height, working.Height));
            bounds.Location = new Point(
                working.Left + (working.Width - bounds.Width) / 2,
                working.Top + (working.Height - bounds.Height) / 2);
        }
        else if (bounds.Width > working.Width || bounds.Height > working.Height)
        {
            bounds.Size = new Size(
                Math.Min(bounds.Width, working.Width),
                Math.Min(bounds.Height, working.Height));
            bounds.Location = new Point(
                Math.Clamp(bounds.Left, working.Left, working.Right - bounds.Width),
                Math.Clamp(bounds.Top, working.Top, working.Bottom - bounds.Height));
        }

        Location = bounds.Location;
        // The handle does not exist yet. AntdUI will convert this logical size
        // once when it creates the window on the selected monitor.
        Size = new Size(
            WindowDpi.Convert(
                bounds.Width,
                targetDpi,
                WindowDpi.LogicalDpi),
            WindowDpi.Convert(
                bounds.Height,
                targetDpi,
                WindowDpi.LogicalDpi));

        // AntdUI keeps the pre-DPI window centered while it scales it, which
        // can shift an explicitly restored top-left coordinate. Reapply the
        // exact device-pixel bounds after its one-time DPI pass but before the
        // form becomes visible.
        Load += (_, _) =>
        {
            _updatingWindowState = true;
            try
            {
                ScreenRectangle = bounds;
                if (rememberWindowLayout && settings.WindowMaximized)
                {
                    WindowState = FormWindowState.Maximized;
                }
            }
            finally
            {
                _updatingWindowState = false;
            }
        };
    }

    private void SetRememberWindowLayout(bool enabled)
    {
        if (_payload.Settings.RememberWindowLayout == enabled)
        {
            return;
        }

        _payload.Settings.RememberWindowLayout = enabled;
        if (enabled)
        {
            CaptureWindowBounds();
        }
        else
        {
            MarkDirty();
        }
    }

    private void ResetWindowLayout()
    {
        ApplyDefaultWindowLayout();
        AntdUI.Message.success(
            this,
            L.Get("Notification.Layout.Reset"),
            autoClose: 3);
    }

    private void ApplyDefaultWindowLayout()
    {
        var targetScreen = Screen.FromControl(this) ?? Screen.PrimaryScreen;
        var working = targetScreen?.WorkingArea ??
                      new Rectangle(0, 0, 1200, 800);
        // This action runs on an already-created window. DeviceDpi reflects
        // the DPI Windows actually applied after dragging between monitors,
        // while a monitor lookup can still report a process-virtualized DPI.
        var targetDpi = DeviceDpi is >= 48 and <= 768
            ? DeviceDpi
            : WindowDpi.ForScreen(targetScreen);
        var width = Math.Min(
            WindowDpi.Convert(
                AppSettings.DefaultWindowWidth,
                WindowDpi.LogicalDpi,
                targetDpi),
            working.Width);
        var height = Math.Min(
            WindowDpi.Convert(
                AppSettings.DefaultWindowHeight,
                WindowDpi.LogicalDpi,
                targetDpi),
            working.Height);
        var bounds = new Rectangle(
            working.Left + ((working.Width - width) / 2),
            working.Top + ((working.Height - height) / 2),
            width,
            height);

        _updatingWindowState = true;
        try
        {
            WindowState = FormWindowState.Normal;
            // AntdUI caches the last native drag/resize rectangle and its
            // Bounds setter can reapply that cached size. ScreenRectangle
            // clears the cache before assigning the requested screen bounds.
            ScreenRectangle = bounds;
        }
        finally
        {
            _updatingWindowState = false;
        }

        var settings = _payload.Settings;
        settings.WindowLeft = -1;
        settings.WindowTop = -1;
        settings.WindowWidth = AppSettings.DefaultWindowWidth;
        settings.WindowHeight = AppSettings.DefaultWindowHeight;
        settings.WindowDpi = 0;
        settings.WindowMaximized = false;
        if (settings.RememberWindowLayout)
        {
            CaptureWindowBounds();
        }
        else
        {
            MarkDirty();
        }
    }

    private void SetStartWithWindows(bool enabled)
    {
        try
        {
            StartupService.SetEnabled(enabled, _payload.Settings.StartMinimized);
            _payload.Settings.StartWithWindows = enabled;
            MarkDirty();
        }
        catch (Exception exception)
        {
            _payload.Settings.StartWithWindows = StartupService.IsEnabled();
            AntdUI.Message.error(
                this,
                L.Format("Error.Startup.UpdateFailed", exception.Message),
                autoClose: 5);
            if (_settingsVisible)
            {
                BeginInvoke(BuildSettings);
            }
        }
    }

    private void OpenDataDirectory()
    {
        try
        {
            ApplicationPaths.EnsureCreated();
            Process.Start(new ProcessStartInfo
            {
                FileName = ApplicationPaths.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.DataDirectory.OpenFailed", exception.Message),
                autoClose: 5);
        }
    }

    private string DataDirectoryDescription()
    {
        var suffix = ApplicationPaths.LocationSource switch
        {
            StorageLocationSource.ProgramDirectory =>
                L.Get("DataDirectory.Location.Program"),
            StorageLocationSource.LegacyCompatibility =>
                L.Get("DataDirectory.Location.Legacy"),
            _ => L.Get("DataDirectory.Location.Custom")
        };
        return ApplicationPaths.DataDirectory + " " + suffix;
    }

    private void ChangeDataDirectory()
    {
        if (!EnsureUnlocked())
        {
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = L.Get("DataDirectory.Choose.Description"),
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = ApplicationPaths.DataDirectory,
            InitialDirectory = ApplicationPaths.DataDirectory
        };
        if (dialog.ShowDialog(this) != DialogResult.OK ||
            string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        string targetDirectory;
        try
        {
            targetDirectory = StorageLocationService.NormalizeDirectory(dialog.SelectedPath);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            AntdUI.Message.error(
                this,
                L.Format("Error.DataDirectory.InvalidPath", exception.Message),
                autoClose: 5);
            return;
        }

        if (StorageLocationService.PathsEqual(
                targetDirectory,
                ApplicationPaths.DataDirectory))
        {
            AntdUI.Message.info(
                this,
                L.Get("DataDirectory.AlreadyInUse"),
                autoClose: 3);
            return;
        }

        var targetVault = Path.Combine(
            targetDirectory,
            StorageLocationService.VaultFileName);
        if (File.Exists(targetVault))
        {
            SwitchToExistingDataDirectory(targetDirectory, targetVault);
            return;
        }

        var sourceVault = _vault.FilePath;
        var portabilityNotice = _vault.ProtectionMode switch
        {
            VaultProtectionMode.WindowsAccount =>
                L.Get("DataDirectory.Change.WindowsProtectionNotice"),
            VaultProtectionMode.Password =>
                L.Get("DataDirectory.Change.PasswordNotice"),
            _ => string.Empty
        };
        AntdUI.Modal.open(DialogSizing.MakeResizable(new AntdUI.Modal.Config(
            this,
            L.Get("DataDirectory.Change.Title"),
            L.Format(
                "DataDirectory.Change.Confirmation",
                targetDirectory,
                portabilityNotice))
        {
            Icon = AntdUI.TType.Info,
            OkText = L.Get("DataDirectory.Change.CopyAndSwitch"),
            CancelText = L.Get("Common.Cancel"),
            OnOk = _ =>
            {
                var copyCreated = false;
                try
                {
                    if (!SaveNow(showError: true))
                    {
                        return false;
                    }

                    Directory.CreateDirectory(targetDirectory);
                    _vault.SaveCopy(_payload, targetVault);
                    copyCreated = true;
                    _vault.SwitchFilePath(targetVault);
                    try
                    {
                        ApplicationPaths.SetDataDirectory(targetDirectory);
                    }
                    catch
                    {
                        _vault.SwitchFilePath(sourceVault);
                        throw;
                    }
                }
                catch (Exception exception)
                {
                    var cleanupFailed = false;
                    if (copyCreated &&
                        !StorageLocationService.PathsEqual(
                            _vault.FilePath,
                            targetVault))
                    {
                        cleanupFailed = !TryDeleteRelocationCopy(targetVault);
                    }

                    AntdUI.Message.error(
                        this,
                        cleanupFailed
                            ? L.Format(
                                "Error.DataDirectory.ChangeFailedWithCopy",
                                exception.Message,
                                targetVault)
                            : L.Format(
                                "Error.DataDirectory.ChangeFailed",
                                exception.Message),
                        autoClose: 7);
                    return false;
                }

                BuildSettings();
                AntdUI.Message.success(
                    this,
                    L.Get("Notification.DataDirectory.Changed"),
                    autoClose: 5);
                return true;
            }
        }));
    }

    private void SwitchToExistingDataDirectory(
        string targetDirectory,
        string targetVault)
    {
        AntdUI.Modal.open(DialogSizing.MakeResizable(new AntdUI.Modal.Config(
            this,
            L.Get("DataDirectory.UseExisting.Title"),
            L.Format("DataDirectory.UseExisting.Confirmation", targetDirectory))
        {
            Icon = AntdUI.TType.Warn,
            OkText = L.Get("DataDirectory.UseExisting.VerifyAndSwitch"),
            CancelText = L.Get("Common.Cancel"),
            OnOk = _ =>
            {
                if (!SaveNow(showError: true))
                {
                    return false;
                }

                try
                {
                    using var candidate = new VaultService(targetVault);
                    string? password = null;
                    if (candidate.InspectProtectionMode() == VaultProtectionMode.Password)
                    {
                        password = PasswordDialog.Prompt(
                            this,
                            L.Get("DataDirectory.UseExisting.VerifyTitle"),
                            L.Get("DataDirectory.UseExisting.PasswordPrompt"),
                            submitText: L.Get("Common.Verify"));
                        if (password is null)
                        {
                            return false;
                        }
                    }

                    var candidatePayload = candidate.Open(password);
                    candidatePayload.Entries.Clear();
                    ApplicationPaths.SetDataDirectory(targetDirectory);
                }
                catch (Exception exception)
                {
                    AntdUI.Message.error(
                        this,
                        L.Format(
                            "Error.DataDirectory.UseExistingFailed",
                            exception.Message),
                        autoClose: 7);
                    return false;
                }

                AntdUI.Message.success(
                    this,
                    L.Get("DataDirectory.UseExisting.Verified"),
                    autoClose: 3);
                BeginInvoke(RequestApplicationRestart);
                return true;
            }
        }));
    }

    private void RequestApplicationRestart()
    {
        RestartRequested = true;
        _forceExit = true;
        Close();
    }

    private static bool TryDeleteRelocationCopy(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return true;
        }
        catch
        {
            // The source vault remains authoritative; a failed cleanup is only an extra copy.
            return false;
        }
    }

    private NotifyIcon CreateTrayIcon()
    {
        var menu = new ContextMenuStrip
        {
            Font = LocalizationFonts.Create(9F)
        };
        menu.Items.Add(L.Get("Tray.Open"), null, (_, _) => ShowFromTray());
        menu.Items.Add(L.Get("Tray.Lock"), null, (_, _) => LockVault());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.Get("Tray.Exit"), null, (_, _) => ExitApplication());

        var icon = new NotifyIcon
        {
            Icon = ApplicationIconProvider.Create(
                SystemInformation.SmallIconSize),
            Text = "AuthenticatorDesk",
            Visible = true,
            ContextMenuStrip = menu
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private sealed class AppToolStripColorTable(ThemePalette palette) : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => palette.SurfaceElevated;

        public override Color ImageMarginGradientBegin => palette.SurfaceElevated;

        public override Color ImageMarginGradientMiddle => palette.SurfaceElevated;

        public override Color ImageMarginGradientEnd => palette.SurfaceElevated;

        public override Color MenuBorder => palette.Border;

        public override Color MenuItemBorder => palette.Primary;

        public override Color MenuItemSelected => palette.PrimarySoft;

        public override Color MenuItemPressedGradientBegin => palette.PrimarySoft;

        public override Color MenuItemPressedGradientMiddle => palette.PrimarySoft;

        public override Color MenuItemPressedGradientEnd => palette.PrimarySoft;

        public override Color SeparatorDark => palette.Border;

        public override Color SeparatorLight => palette.SurfaceMuted;
    }

    private void HideToTray()
    {
        if (IsDisposed)
        {
            return;
        }

        Hide();
        ShowInTaskbar = false;
    }

    private void ShowFromTray()
    {
        if (IsDisposed)
        {
            return;
        }

        ShowInTaskbar = true;
        Show();
        _updatingWindowState = true;
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        _updatingWindowState = false;
        Activate();
        BringToFront();
    }

    private void ExitApplication()
    {
        _forceExit = true;
        Close();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (!_forceExit &&
            eventArgs.CloseReason == CloseReason.UserClosing &&
            _payload.Settings.CloseToTray)
        {
            eventArgs.Cancel = true;
            if (_payload.Settings.LockOnMinimize && !_locked)
            {
                if (!LockVault())
                {
                    return;
                }
            }

            HideToTray();
            return;
        }

        if (!_locked &&
            _vault.IsOpen &&
            !SaveNow(showError: true))
        {
            eventArgs.Cancel = true;
            _forceExit = false;
            RestartRequested = false;
            _trayIcon.Visible = true;
            return;
        }

        _trayIcon.Visible = false;
    }

    private static string ExistingDirectory(string? path)
    {
        return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path)
            ? path
            : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    }

    private void NormalizeSortOrder()
    {
        var ordered = _payload.Entries
            .OrderBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].SortOrder = index * 10;
        }

        _payload.Entries = ordered;
    }

    private sealed record LanguageChoice(string Code, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
