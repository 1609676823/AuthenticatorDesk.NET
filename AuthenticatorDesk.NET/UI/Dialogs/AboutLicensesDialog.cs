namespace AuthenticatorDesk.UI.Dialogs;

public sealed class AboutLicensesDialog : AboutLicensesDialogVisualBase
{
    private AboutLicensesDialog()
    {
        ConfigureAboutLicensesDialog();
    }

    public static void Open(IWin32Window? owner)
    {
        using var dialog = new AboutLicensesDialog();
        DialogSizing.FitToOwner(
            dialog,
            owner,
            new Size(680, 620),
            new Size(420, 500));
        dialog.ShowDialog(owner);
    }
}
