using AuthenticatorDesk.Localization;

namespace AuthenticatorDesk.UI.Dialogs;

public sealed class PasswordDialog : PasswordDialogVisualBase
{
    private PasswordDialog(
        string title,
        string description,
        bool requireConfirmation,
        string submitText)
        : base(title, description, requireConfirmation, submitText)
    {
    }

    public string Password => PasswordValue;

    public static string? Prompt(
        IWin32Window? owner,
        string title,
        string description,
        bool requireConfirmation = false,
        string? submitText = null)
    {
        using var dialog = new PasswordDialog(
            title,
            description,
            requireConfirmation,
            submitText ?? L.Get("Common.Confirm"));
        DialogSizing.FitToOwner(
            dialog,
            owner,
            new Size(440, requireConfirmation ? 350 : 296),
            new Size(340, requireConfirmation ? 330 : 276));
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.Password : null;
    }
}
