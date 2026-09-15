namespace AuthenticatorDesk.UI.Dialogs;

internal static class DialogSizing
{
    public static AntdUI.Modal.Config MakeResizable(
        AntdUI.Modal.Config config,
        Size? minimumSize = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Draggable = true;
        config.Resizable = true;
        config.MinimumSize = minimumSize ?? new Size(320, 180);
        return config;
    }

    public static void FitToOwner(
        Form dialog,
        IWin32Window? owner,
        Size preferred,
        Size minimum)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        var ownerForm = owner as Form ?? (owner as Control)?.FindForm();
        // Dialogs never force themselves on top; they only mirror the user's
        // current choice on the owning window.
        dialog.TopMost = ownerForm?.TopMost ?? false;

        var screen = owner is not null && owner.Handle != IntPtr.Zero
            ? Screen.FromHandle(owner.Handle)
            : dialog.IsHandleCreated
                ? Screen.FromHandle(dialog.Handle)
                : Screen.PrimaryScreen;
        var workingArea = screen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800);
        var ownerSize = owner is Control control && control.Width > 0 && control.Height > 0
            ? control.Size
            : workingArea.Size;
        var targetDpi = dialog.IsHandleCreated
            ? Math.Max(1, dialog.DeviceDpi)
            : WindowDpi.ForScreen(screen);
        var preferredDevice = new Size(
            WindowDpi.Convert(preferred.Width, WindowDpi.LogicalDpi, targetDpi),
            WindowDpi.Convert(preferred.Height, WindowDpi.LogicalDpi, targetDpi));
        var minimumDevice = new Size(
            WindowDpi.Convert(minimum.Width, WindowDpi.LogicalDpi, targetDpi),
            WindowDpi.Convert(minimum.Height, WindowDpi.LogicalDpi, targetDpi));

        var maximumWidth = Math.Max(
            1,
            Math.Min(
                Math.Max(1, workingArea.Width - 16),
                Math.Max(1, ownerSize.Width - 12)));
        var maximumHeight = Math.Max(
            1,
            Math.Min(
                Math.Max(1, workingArea.Height - 16),
                Math.Max(1, ownerSize.Height - 12)));
        var fittedMinimumDevice = new Size(
            Math.Min(minimumDevice.Width, maximumWidth),
            Math.Min(minimumDevice.Height, maximumHeight));
        var fittedSizeDevice = new Size(
            Math.Clamp(
                preferredDevice.Width,
                fittedMinimumDevice.Width,
                maximumWidth),
            Math.Clamp(
                preferredDevice.Height,
                fittedMinimumDevice.Height,
                maximumHeight));

        if (dialog.IsHandleCreated)
        {
            dialog.MinimumSize = fittedMinimumDevice;
            dialog.Size = fittedSizeDevice;
            return;
        }

        // AntdUI applies the target monitor's DPI when the handle is created.
        // Keep pre-handle values logical so that pass does not scale them twice.
        dialog.MinimumSize = new Size(
            WindowDpi.Convert(
                fittedMinimumDevice.Width,
                targetDpi,
                WindowDpi.LogicalDpi),
            WindowDpi.Convert(
                fittedMinimumDevice.Height,
                targetDpi,
                WindowDpi.LogicalDpi));
        dialog.Size = new Size(
            WindowDpi.Convert(
                fittedSizeDevice.Width,
                targetDpi,
                WindowDpi.LogicalDpi),
            WindowDpi.Convert(
                fittedSizeDevice.Height,
                targetDpi,
                WindowDpi.LogicalDpi));
    }
}
