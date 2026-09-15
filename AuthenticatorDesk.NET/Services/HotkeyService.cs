using System.Runtime.InteropServices;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.Services;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
    NoRepeat = 0x4000
}

public sealed record HotkeyRegistrationError(Guid EntryId, string Hotkey, string Message);

public sealed class HotkeyService : IDisposable
{
    private readonly Dictionary<int, Guid> _registrations = [];
    private IntPtr _windowHandle;
    private bool _disposed;

    public IReadOnlyList<HotkeyRegistrationError> RegisterAll(
        IntPtr windowHandle,
        IEnumerable<AuthenticatorEntry> entries)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        UnregisterAll();
        _windowHandle = windowHandle;

        var errors = new List<HotkeyRegistrationError>();
        var id = 0x4100;
        foreach (var entry in entries.Where(item => !string.IsNullOrWhiteSpace(item.Hotkey)))
        {
            if (!TryParse(entry.Hotkey, out var modifiers, out var key))
            {
                errors.Add(new HotkeyRegistrationError(
                    entry.Id,
                    entry.Hotkey!,
                    L.Get("service.hotkey.error.invalidFormat")));
                continue;
            }

            modifiers |= HotkeyModifiers.NoRepeat;
            if (!RegisterHotKey(windowHandle, id, (uint)modifiers, (uint)key))
            {
                errors.Add(new HotkeyRegistrationError(
                    entry.Id,
                    entry.Hotkey!,
                    L.Get("service.hotkey.error.alreadyInUse")));
                continue;
            }

            _registrations[id] = entry.Id;
            id++;
        }

        return errors;
    }

    public bool TryResolve(int registrationId, out Guid entryId)
    {
        return _registrations.TryGetValue(registrationId, out entryId);
    }

    public void UnregisterAll()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            foreach (var id in _registrations.Keys)
            {
                _ = UnregisterHotKey(_windowHandle, id);
            }
        }

        _registrations.Clear();
        _windowHandle = IntPtr.Zero;
    }

    public static bool TryParse(
        string? value,
        out HotkeyModifiers modifiers,
        out Keys key)
    {
        modifiers = HotkeyModifiers.None;
        key = Keys.None;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var part in value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= HotkeyModifiers.Control;
                    continue;
                case "alt":
                    modifiers |= HotkeyModifiers.Alt;
                    continue;
                case "shift":
                    modifiers |= HotkeyModifiers.Shift;
                    continue;
                case "win":
                case "windows":
                    modifiers |= HotkeyModifiers.Windows;
                    continue;
            }

            if (key != Keys.None)
            {
                return false;
            }

            if (part.Length == 1 && char.IsDigit(part[0]))
            {
                key = Keys.D0 + (part[0] - '0');
            }
            else if (part.Length == 1 && char.IsLetter(part[0]))
            {
                key = (Keys)char.ToUpperInvariant(part[0]);
            }
            else if (!Enum.TryParse(part, true, out key))
            {
                return false;
            }
        }

        return key != Keys.None && modifiers != HotkeyModifiers.None;
    }

    public static string Format(Keys keyData)
    {
        var parts = new List<string>(4);
        if (keyData.HasFlag(Keys.Control))
        {
            parts.Add("Ctrl");
        }

        if (keyData.HasFlag(Keys.Alt))
        {
            parts.Add("Alt");
        }

        if (keyData.HasFlag(Keys.Shift))
        {
            parts.Add("Shift");
        }

        var key = keyData & Keys.KeyCode;
        if (key is >= Keys.D0 and <= Keys.D9)
        {
            parts.Add(((int)key - (int)Keys.D0).ToString());
        }
        else
        {
            parts.Add(key.ToString());
        }

        return string.Join('+', parts);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterAll();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(
        IntPtr windowHandle,
        int id,
        uint modifiers,
        uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);
}
