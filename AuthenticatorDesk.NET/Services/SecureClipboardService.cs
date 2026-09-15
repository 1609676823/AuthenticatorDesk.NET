using System.Runtime.InteropServices;

namespace AuthenticatorDesk.Services;

public sealed class SecureClipboardService : IDisposable
{
    private readonly System.Windows.Forms.Timer _clearTimer;
    private string? _expectedText;
    private bool _disposed;

    public SecureClipboardService()
    {
        _clearTimer = new System.Windows.Forms.Timer();
        _clearTimer.Tick += ClearTimerOnTick;
    }

    public void Copy(string value, int clearAfterSeconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        SetClipboardText(value);
        _expectedText = value;
        _clearTimer.Stop();
        if (clearAfterSeconds > 0)
        {
            _clearTimer.Interval = Math.Clamp(clearAfterSeconds, 1, 3_600) * 1_000;
            _clearTimer.Start();
        }
    }

    public void ClearNow()
    {
        _clearTimer.Stop();
        ClearIfUnchanged();
    }

    private void ClearTimerOnTick(object? sender, EventArgs eventArgs)
    {
        _clearTimer.Stop();
        ClearIfUnchanged();
    }

    private void ClearIfUnchanged()
    {
        try
        {
            if (_expectedText is not null &&
                Clipboard.ContainsText() &&
                string.Equals(Clipboard.GetText(), _expectedText, StringComparison.Ordinal))
            {
                Clipboard.Clear();
            }
        }
        catch (ExternalException)
        {
            // Another process currently owns the clipboard; leave its contents untouched.
        }
        finally
        {
            _expectedText = null;
        }
    }

    private static void SetClipboardText(string value)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                Clipboard.SetText(value);
                return;
            }
            catch (ExternalException) when (attempt < 3)
            {
                Thread.Sleep(40 * (attempt + 1));
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _clearTimer.Stop();
        _clearTimer.Dispose();
        _expectedText = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
