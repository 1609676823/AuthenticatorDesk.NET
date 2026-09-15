namespace AuthenticatorDesk.Localization;

public sealed record LanguageInfo(
    string Code,
    string Name,
    string NativeName)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name) ||
        string.Equals(Name, NativeName, StringComparison.OrdinalIgnoreCase)
            ? NativeName
            : $"{NativeName} ({Name})";

    public override string ToString() => DisplayName;
}
