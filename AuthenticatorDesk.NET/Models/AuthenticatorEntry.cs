using System.Text.Json.Serialization;

namespace AuthenticatorDesk.Models;

public enum AuthenticatorKind
{
    Totp,
    Hotp,
    Google,
    Microsoft,
    OktaVerify,
    GuildWars,
    Steam,
    BattleNet,
    Trion
}

public enum OtpAlgorithm
{
    Sha1,
    Sha256,
    Sha512
}

public enum EntryHotkeyAction
{
    Copy,
    Type,
    Show
}

public sealed class AuthenticatorEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The Base32-encoded shared secret. The entire payload containing this value is encrypted at rest.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    public AuthenticatorKind Kind { get; set; } = AuthenticatorKind.Totp;

    public OtpAlgorithm Algorithm { get; set; } = OtpAlgorithm.Sha1;

    public int Digits { get; set; } = 6;

    public int Period { get; set; } = 30;

    public long Counter { get; set; }

    public long TimeOffsetSeconds { get; set; }

    public string? Serial { get; set; }

    public string? DeviceId { get; set; }

    public string? Notes { get; set; }

    public bool Favorite { get; set; }

    public bool RequireUnlock { get; set; }

    public bool CopyOnReveal { get; set; }

    public bool AutoRefresh { get; set; } = true;

    public bool HideSerial { get; set; } = true;

    public string AccentColor { get; set; } = "#5B7CFA";

    /// <summary>
    /// A portable representation such as "Ctrl+Alt+G".
    /// </summary>
    public string? Hotkey { get; set; }

    public EntryHotkeyAction HotkeyAction { get; set; } = EntryHotkeyAction.Copy;

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Provider-specific recovery fields. This dictionary is encrypted with the rest of the vault.
    /// </summary>
    public Dictionary<string, string> ProviderData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsCounterBased => Kind == AuthenticatorKind.Hotp;

    [JsonIgnore]
    public string DisplayIssuer => string.IsNullOrWhiteSpace(Issuer) ? Kind switch
    {
        AuthenticatorKind.Steam => "Steam",
        AuthenticatorKind.BattleNet => "Battle.net",
        AuthenticatorKind.Trion => "Trion / Glyph",
        AuthenticatorKind.Google => "Google",
        AuthenticatorKind.Microsoft => "Microsoft",
        AuthenticatorKind.OktaVerify => "Okta Verify",
        AuthenticatorKind.GuildWars => "Guild Wars 2",
        AuthenticatorKind.Hotp => "HOTP",
        _ => "TOTP"
    } : Issuer;

    [JsonIgnore]
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? DisplayIssuer : Name;

    public AuthenticatorEntry Clone()
    {
        return new AuthenticatorEntry
        {
            Id = Id,
            Name = Name,
            Issuer = Issuer,
            Secret = Secret,
            Kind = Kind,
            Algorithm = Algorithm,
            Digits = Digits,
            Period = Period,
            Counter = Counter,
            TimeOffsetSeconds = TimeOffsetSeconds,
            Serial = Serial,
            DeviceId = DeviceId,
            Notes = Notes,
            Favorite = Favorite,
            RequireUnlock = RequireUnlock,
            CopyOnReveal = CopyOnReveal,
            AutoRefresh = AutoRefresh,
            HideSerial = HideSerial,
            AccentColor = AccentColor,
            Hotkey = Hotkey,
            HotkeyAction = HotkeyAction,
            SortOrder = SortOrder,
            CreatedAtUtc = CreatedAtUtc,
            ModifiedAtUtc = ModifiedAtUtc,
            ProviderData = new Dictionary<string, string>(ProviderData, StringComparer.OrdinalIgnoreCase)
        };
    }

    public void ApplyProviderDefaults()
    {
        switch (Kind)
        {
            case AuthenticatorKind.Steam:
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? "Steam" : Issuer;
                Algorithm = OtpAlgorithm.Sha1;
                Digits = 5;
                Period = 30;
                break;
            case AuthenticatorKind.BattleNet:
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? "Battle.net" : Issuer;
                Algorithm = OtpAlgorithm.Sha1;
                Digits = 8;
                Period = 30;
                break;
            case AuthenticatorKind.Trion:
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? "Trion / Glyph" : Issuer;
                Algorithm = OtpAlgorithm.Sha1;
                Digits = 6;
                Period = 30;
                break;
            case AuthenticatorKind.Google:
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? "Google" : Issuer;
                Digits = Digits is >= 4 and <= 10 ? Digits : 6;
                Period = Period is >= 1 and <= 3600 ? Period : 30;
                break;
            case AuthenticatorKind.Microsoft:
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? "Microsoft" : Issuer;
                Digits = Digits is >= 4 and <= 10 ? Digits : 6;
                Period = Period is >= 1 and <= 3600 ? Period : 30;
                break;
            case AuthenticatorKind.OktaVerify:
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? "Okta Verify" : Issuer;
                Digits = Digits is >= 4 and <= 10 ? Digits : 6;
                Period = Period is >= 1 and <= 3600 ? Period : 30;
                break;
            case AuthenticatorKind.GuildWars:
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? "Guild Wars 2" : Issuer;
                Digits = Digits is >= 4 and <= 10 ? Digits : 6;
                Period = Period is >= 1 and <= 3600 ? Period : 30;
                break;
            case AuthenticatorKind.Hotp:
                Period = 30;
                Digits = Digits is >= 4 and <= 10 ? Digits : 6;
                break;
            default:
                Digits = Digits is >= 4 and <= 10 ? Digits : 6;
                Period = Period is >= 1 and <= 3600 ? Period : 30;
                break;
        }
    }
}
