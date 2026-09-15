using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.UI.Dialogs;

public sealed class QrExportDialog : QrExportDialogVisualBase
{
    public QrExportDialog(AuthenticatorEntry entry)
        : base(entry)
    {
    }
}
