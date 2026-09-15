namespace AuthenticatorDesk.Models;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public sealed class AppSettings
{
    public const int DefaultWindowWidth = 1080;

    public const int DefaultWindowHeight = 720;

    public const int DefaultAutoLockMinutes = 10;

    public const int MinimumAutoLockMinutes = 0;

    public const int MaximumAutoLockMinutes = 1_440;

    public const int DefaultClipboardClearSeconds = 0;

    public AppThemeMode Theme { get; set; } = AppThemeMode.Dark;

    public int ClipboardClearSeconds { get; set; } = DefaultClipboardClearSeconds;

    public int AutoLockMinutes { get; set; } = DefaultAutoLockMinutes;

    public bool MinimizeToTray { get; set; } = true;

    public bool CloseToTray { get; set; }

    public bool AlwaysOnTop { get; set; } = false;

    public bool StartWithWindows { get; set; }

    public bool HideCodes { get; set; }

    public bool ShowOnlyFavorites { get; set; }

    public bool LockOnMinimize { get; set; }

    public bool ConfirmBeforeDelete { get; set; } = true;

    public bool StartMinimized { get; set; }

    public bool CompactCards { get; set; }

    public bool RememberWindowLayout { get; set; } = false;

    public string LastImportDirectory { get; set; } = string.Empty;

    public string LastExportDirectory { get; set; } = string.Empty;

    public int WindowLeft { get; set; } = -1;

    public int WindowTop { get; set; } = -1;

    public int WindowWidth { get; set; } = DefaultWindowWidth;

    public int WindowHeight { get; set; } = DefaultWindowHeight;

    public int WindowDpi { get; set; }

    public bool WindowMaximized { get; set; }

    public Guid? SelectedEntryId { get; set; }
}
