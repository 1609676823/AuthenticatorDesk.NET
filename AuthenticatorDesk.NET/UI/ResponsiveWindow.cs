using System.Runtime.InteropServices;

namespace AuthenticatorDesk.UI;

internal static class WindowDpi
{
    public const int LogicalDpi = 96;
    private const uint MonitorDefaultToNearest = 2;

    public static int ForScreen(Screen? screen)
    {
        if (screen is null)
        {
            return LogicalDpi;
        }

        var bounds = screen.Bounds;
        var point = new Point(
            bounds.Left + (bounds.Width / 2),
            bounds.Top + (bounds.Height / 2));
        var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return LogicalDpi;
        }

        try
        {
            return GetDpiForMonitor(
                       monitor,
                       MonitorDpiType.Effective,
                       out var dpiX,
                       out _) == 0 &&
                   dpiX is >= 48 and <= 768
                ? (int)dpiX
                : LogicalDpi;
        }
        catch (DllNotFoundException)
        {
            return LogicalDpi;
        }
        catch (EntryPointNotFoundException)
        {
            return LogicalDpi;
        }
    }

    public static int Convert(int pixels, int sourceDpi, int targetDpi)
    {
        if (pixels == 0)
        {
            return 0;
        }

        sourceDpi = Math.Max(1, sourceDpi);
        targetDpi = Math.Max(1, targetDpi);
        var scaled = Math.Round(
            pixels * (double)targetDpi / sourceDpi,
            MidpointRounding.AwayFromZero);
        return (int)Math.Clamp(scaled, 1D, int.MaxValue);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(
        Point point,
        uint flags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(
        IntPtr monitor,
        MonitorDpiType dpiType,
        out uint dpiX,
        out uint dpiY);

    private enum MonitorDpiType
    {
        Effective
    }
}

/// <summary>
/// Common window behavior for the application's borderless, DPI-aware windows.
/// This type must remain concrete because the WinForms inherited-form designer
/// creates an instance of the direct base window while loading derived forms.
/// </summary>
public class ResponsiveWindow : AntdUI.Window
{
    private readonly Icon _applicationIcon;

    protected ResponsiveWindow()
    {
        _applicationIcon = ApplicationIconProvider.Create(
            SystemInformation.IconSize);
        Icon = _applicationIcon;

        // AntdUI currently enables both options by default. Keep them explicit
        // because every application window relies on edge hit-testing after
        // removing the native frame.
        Resizable = true;
        EnableHitTest = true;
        ResizeRedraw = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _applicationIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    protected int ScaleLogical(int logicalPixels)
    {
        return WindowDpi.Convert(
            logicalPixels,
            WindowDpi.LogicalDpi,
            DeviceDpi);
    }

    protected int ToLogical(int devicePixels)
    {
        return WindowDpi.Convert(
            devicePixels,
            DeviceDpi,
            WindowDpi.LogicalDpi);
    }
}
