using AuthenticatorDesk.Localization;
using Microsoft.Win32;

namespace AuthenticatorDesk.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AuthenticatorDesk";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return !string.IsNullOrWhiteSpace(key?.GetValue(ValueName) as string);
    }

    public static void SetEnabled(bool enabled, bool startMinimized)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (!enabled)
        {
            key.DeleteValue(ValueName, false);
            return;
        }

        var executable = Environment.ProcessPath
                         ?? throw new InvalidOperationException(
                             L.Get("service.startup.error.applicationPathUnavailable"));
        var command = $"\"{executable}\"";
        if (startMinimized)
        {
            command += " --minimized";
        }

        key.SetValue(ValueName, command, RegistryValueKind.String);
    }
}
