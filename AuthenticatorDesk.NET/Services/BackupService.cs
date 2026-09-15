using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.Services;

public static class BackupService
{
    private const string Magic = "AuthenticatorDesk.Backup";
    private const int Version = 1;
    private const string AssociatedData = "AuthenticatorDesk.Backup/v1";

    public static void Export(
        string filePath,
        IEnumerable<AuthenticatorEntry> entries,
        string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                L.Get("service.backup.error.passwordRequired"),
                nameof(password));
        }

        var payload = new BackupPayload
        {
            CreatedAtUtc = DateTime.UtcNow,
            Entries = entries.Select(entry => entry.Clone()).ToList()
        };

        var salt = RandomNumberGenerator.GetBytes(CryptoService.SaltSize);
        var key = CryptoService.DeriveKey(password, salt, CryptoService.PasswordIterations);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload, VaultService.JsonOptions);

        try
        {
            var encrypted = CryptoService.Encrypt(plaintext, key, AssociatedData);
            var envelope = new BackupEnvelope
            {
                Magic = Magic,
                Version = Version,
                Salt = Convert.ToBase64String(salt),
                Iterations = CryptoService.PasswordIterations,
                Nonce = Convert.ToBase64String(encrypted.Nonce),
                Tag = Convert.ToBase64String(encrypted.Tag),
                Ciphertext = Convert.ToBase64String(encrypted.Ciphertext)
            };

            var directory = Path.GetDirectoryName(Path.GetFullPath(filePath))
                ?? throw new IOException(L.Get("service.backup.error.invalidPath"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                filePath,
                JsonSerializer.Serialize(envelope, VaultService.JsonOptions),
                new UTF8Encoding(false));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static IReadOnlyList<AuthenticatorEntry> Import(string filePath, string password)
    {
        BackupEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<BackupEnvelope>(
                    File.ReadAllText(filePath, Encoding.UTF8),
                    VaultService.JsonOptions)
                ?? throw new JsonException("Backup is empty.");
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            throw new FormatException(
                L.Get("service.backup.error.unreadableOrInvalid"),
                exception);
        }

        if (envelope.Magic != Magic || envelope.Version != Version)
        {
            throw new FormatException(
                L.Get("service.backup.error.unsupportedFormat"));
        }

        byte[] key;
        byte[] plaintext;
        try
        {
            var salt = Convert.FromBase64String(envelope.Salt);
            key = CryptoService.DeriveKey(password, salt, envelope.Iterations);
            plaintext = CryptoService.Decrypt(
                new CipherPackage(
                    Convert.FromBase64String(envelope.Nonce),
                    Convert.FromBase64String(envelope.Tag),
                    Convert.FromBase64String(envelope.Ciphertext)),
                key,
                AssociatedData);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            throw new VaultAuthenticationException(
                L.Get("service.backup.error.wrongPasswordOrCorrupt"),
                exception);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<BackupPayload>(plaintext, VaultService.JsonOptions)
                ?? throw new FormatException(
                    L.Get("service.backup.error.emptyContent"));
            payload.Entries ??= [];
            foreach (var entry in payload.Entries)
            {
                entry.ProviderData ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                entry.ApplyProviderDefaults();
                OtpService.ValidateForCodeGeneration(entry);
            }

            return payload.Entries;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public static bool IsBackupFile(string filePath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(filePath, Encoding.UTF8));
            return document.RootElement.TryGetProperty("magic", out var value) &&
                   value.GetString() == Magic;
        }
        catch
        {
            return false;
        }
    }

    private sealed class BackupPayload
    {
        public DateTime CreatedAtUtc { get; set; }
        public List<AuthenticatorEntry> Entries { get; set; } = [];
    }

    private sealed class BackupEnvelope
    {
        public string Magic { get; set; } = string.Empty;
        public int Version { get; set; }
        public string Salt { get; set; } = string.Empty;
        public int Iterations { get; set; }
        public string Nonce { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Ciphertext { get; set; } = string.Empty;
    }
}
