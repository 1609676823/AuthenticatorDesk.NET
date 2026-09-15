namespace AuthenticatorDesk.Models;

public sealed class VaultPayload
{
    public int SchemaVersion { get; set; } = 1;

    public List<AuthenticatorEntry> Entries { get; set; } = [];

    public AppSettings Settings { get; set; } = new();

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public void Normalize()
    {
        Entries ??= [];
        Settings ??= new AppSettings();
        Settings.AutoLockMinutes = Math.Clamp(
            Settings.AutoLockMinutes,
            AppSettings.MinimumAutoLockMinutes,
            AppSettings.MaximumAutoLockMinutes);

        var usedIds = new HashSet<Guid>();
        for (var index = 0; index < Entries.Count; index++)
        {
            var entry = Entries[index] ?? new AuthenticatorEntry();
            Entries[index] = entry;

            if (entry.Id == Guid.Empty || !usedIds.Add(entry.Id))
            {
                entry.Id = Guid.NewGuid();
                usedIds.Add(entry.Id);
            }

            entry.Name = entry.Name?.Trim() ?? string.Empty;
            entry.Issuer = entry.Issuer?.Trim() ?? string.Empty;
            entry.Secret = entry.Secret?.Trim() ?? string.Empty;
            entry.ProviderData ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            entry.ApplyProviderDefaults();
        }
    }
}
