namespace AuthenticatorDesk.UI.Dialogs;

public sealed class TextImportDialog : TextImportDialogVisualBase
{
    public TextImportDialog(string initialText = "")
        : base(initialText)
    {
    }

    public string InputText => InputTextValue;

    public static string? Prompt(IWin32Window? owner, string initialText = "")
    {
        using var dialog = new TextImportDialog(initialText);
        DialogSizing.FitToOwner(
            dialog,
            owner,
            new Size(620, 450),
            new Size(340, 340));
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.InputText : null;
    }
}
