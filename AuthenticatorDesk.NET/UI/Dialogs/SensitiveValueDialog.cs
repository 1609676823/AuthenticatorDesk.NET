namespace AuthenticatorDesk.UI.Dialogs;

public sealed class SensitiveValueDialog : SensitiveValueDialogVisualBase
{
    public SensitiveValueDialog(
        string title,
        string description,
        string value,
        bool initiallyRevealed = false)
    {
        ConfigureSensitiveValueDialog(
            title,
            description,
            value,
            initiallyRevealed);
    }
}
