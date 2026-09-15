using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.UI.Dialogs;

public sealed class EntryEditorForm : EntryEditorFormVisualBase
{
    private EntryEditorForm(AuthenticatorEntry entry, bool isNew)
        : base(entry, isNew)
    {
    }

    public AuthenticatorEntry Result => ResultValue;

    public static AuthenticatorEntry? Edit(
        IWin32Window owner,
        AuthenticatorEntry entry,
        bool isNew)
    {
        using var dialog = new EntryEditorForm(entry, isNew);
        DialogSizing.FitToOwner(
            dialog,
            owner,
            new Size(720, 760),
            new Size(340, 500));
        return dialog.ShowDialog(owner) == DialogResult.OK
            ? dialog.Result
            : null;
    }

    // Kept declared on the runtime type for reflection-based compatibility.
    private static AntdUI.Select CreateSelect(IEnumerable<object> items)
    {
        return CreateSelectForRuntime(items);
    }
}
