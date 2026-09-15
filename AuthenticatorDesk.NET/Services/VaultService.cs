using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.Services;

public enum VaultProtectionMode
{
    WindowsAccount = 0,
    Password = 1,
    Portable = 2
}

public sealed class VaultAuthenticationException : Exception
{
    public VaultAuthenticationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

internal interface IWindowsAccountProtector
{
    byte[] Protect(byte[] plaintext);

    byte[] Unprotect(byte[] protectedData);
}

internal sealed class DpapiWindowsAccountProtector : IWindowsAccountProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AuthenticatorDesk.NET/v1");

    public byte[] Protect(byte[] plaintext)
    {
        return ProtectedData.Protect(
            plaintext,
            Entropy,
            DataProtectionScope.CurrentUser);
    }

    public byte[] Unprotect(byte[] protectedData)
    {
        return ProtectedData.Unprotect(
            protectedData,
            Entropy,
            DataProtectionScope.CurrentUser);
    }
}

public sealed class VaultService : IDisposable
{
    private const string Magic = "AuthenticatorDesk.Vault";
    private const int FormatVersion = 1;
    private const string AssociatedData = "AuthenticatorDesk.Vault/v1";

    private readonly IWindowsAccountProtector _windowsAccountProtector;
    private string _filePath;
    private byte[]? _sessionKey;
    private byte[]? _storedKey;
    private byte[]? _salt;
    private int _iterations = CryptoService.PasswordIterations;
    private VaultProtectionMode _mode = VaultProtectionMode.Portable;
    private bool _mustAlreadyExist;
    private bool _pendingInitialCreate;
    private bool _disposed;

    public VaultService(string filePath)
        : this(filePath, new DpapiWindowsAccountProtector())
    {
    }

    internal VaultService(
        string filePath,
        IWindowsAccountProtector windowsAccountProtector)
    {
        _filePath = Path.GetFullPath(filePath);
        _mustAlreadyExist = File.Exists(_filePath) || HasRecoveryArtifacts(_filePath);
        _windowsAccountProtector = windowsAccountProtector
            ?? throw new ArgumentNullException(nameof(windowsAccountProtector));
    }

    public string FilePath => _filePath;

    public VaultProtectionMode ProtectionMode => _mode;

    public bool IsOpen => _sessionKey is not null;

    public bool Exists => File.Exists(_filePath);

    public static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();

    public VaultProtectionMode InspectProtectionMode()
    {
        if (!File.Exists(_filePath))
        {
            if (_mustAlreadyExist)
            {
                throw new VaultAuthenticationException(
                    L.Get("service.vault.error.fileMissing"));
            }

            return VaultProtectionMode.Portable;
        }

        try
        {
            var envelope = ReadEnvelope(_filePath);
            return envelope.Protection;
        }
        catch (Exception exception) when (
            exception is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new VaultAuthenticationException(
                L.Get("service.vault.error.headerUnreadable"),
                exception);
        }
    }

    public VaultPayload Open(string? password = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!File.Exists(_filePath))
        {
            if (_mustAlreadyExist)
            {
                throw new VaultAuthenticationException(
                    L.Get("service.vault.error.fileMissing"));
            }

            InitializePortableKey();
            _mustAlreadyExist = true;
            _pendingInitialCreate = true;
            return new VaultPayload();
        }

        VaultEnvelope envelope;
        try
        {
            envelope = ReadEnvelope(_filePath);
            ValidateEnvelope(envelope);
        }
        catch (Exception exception) when (
            exception is JsonException or IOException or UnauthorizedAccessException or FormatException)
        {
            throw new VaultAuthenticationException(
                L.Get("service.vault.error.unreadableOrInvalid"),
                exception);
        }

        byte[] key;
        try
        {
            key = envelope.Protection switch
            {
                VaultProtectionMode.Portable => DecodePortableKey(envelope.ProtectedKey),
                VaultProtectionMode.WindowsAccount => UnprotectWindowsKey(envelope.ProtectedKey),
                VaultProtectionMode.Password => CryptoService.DeriveKey(
                    password ?? throw new VaultAuthenticationException(
                        L.Get("service.vault.error.passwordRequired")),
                    DecodeRequired(envelope.Salt, "salt"),
                    envelope.Iterations),
                _ => throw new VaultAuthenticationException(
                    L.Get("service.vault.error.unknownProtectionMode"))
            };
        }
        catch (VaultAuthenticationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            var message = envelope.Protection switch
            {
                VaultProtectionMode.Password =>
                    L.Get("service.vault.error.wrongPasswordOrCorrupt"),
                VaultProtectionMode.WindowsAccount =>
                    L.Get("service.vault.error.windowsUnlockFailed"),
                _ => L.Get("service.vault.error.portableKeyInvalid")
            };
            throw new VaultAuthenticationException(message, exception);
        }

        byte[] plaintext;
        try
        {
            plaintext = CryptoService.Decrypt(
                new CipherPackage(
                    DecodeRequired(envelope.Nonce, "nonce"),
                    DecodeRequired(envelope.Tag, "tag"),
                    DecodeRequired(envelope.Ciphertext, "ciphertext")),
                key,
                AssociatedData);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new VaultAuthenticationException(
                envelope.Protection == VaultProtectionMode.Password
                    ? L.Get("service.vault.error.wrongPasswordOrIntegrityFailed")
                    : L.Get("service.vault.error.integrityFailed"),
                exception);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<VaultPayload>(plaintext, JsonOptions)
                ?? throw new JsonException("Vault payload is empty.");
            payload.Normalize();

            ReplaceSessionKey(key);
            _mode = envelope.Protection;
            ReplaceStoredKey(
                envelope.Protection is VaultProtectionMode.Portable or
                    VaultProtectionMode.WindowsAccount
                    ? DecodeRequired(envelope.ProtectedKey, "protectedKey")
                    : null);
            ReplaceSalt(
                envelope.Protection == VaultProtectionMode.Password
                    ? DecodeRequired(envelope.Salt, "salt")
                    : null);
            _iterations = envelope.Iterations > 0
                ? envelope.Iterations
                : CryptoService.PasswordIterations;
            _mustAlreadyExist = true;
            _pendingInitialCreate = false;
            return payload;
        }
        catch (JsonException exception)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new VaultAuthenticationException(
                L.Get("service.vault.error.contentInvalid"),
                exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public void Save(VaultPayload payload)
    {
        EnsureOpen(payload);
        var envelope = CreateEnvelope(
            payload,
            _sessionKey!,
            _mode,
            _storedKey,
            _salt,
            _iterations);
        if (_pendingInitialCreate)
        {
            WriteNewFile(_filePath, envelope.Json);
        }
        else
        {
            WriteAtomic(_filePath, envelope.Json, createBackup: true);
        }

        payload.UpdatedAtUtc = envelope.UpdatedAtUtc;
        _mustAlreadyExist = true;
        _pendingInitialCreate = false;
    }

    public void SaveCopy(VaultPayload payload, string filePath)
    {
        EnsureOpen(payload);
        var targetPath = Path.GetFullPath(filePath);
        if (StorageLocationService.PathsEqual(targetPath, _filePath))
        {
            throw new IOException(
                L.Get("service.vault.error.targetIsCurrent"));
        }

        if (File.Exists(targetPath) || HasRecoveryArtifacts(targetPath))
        {
            throw new IOException(
                L.Get("service.vault.error.targetConflict"));
        }

        var envelope = CreateEnvelope(
            payload,
            _sessionKey!,
            _mode,
            _storedKey,
            _salt,
            _iterations);
        WriteNewFile(targetPath, envelope.Json);
        payload.UpdatedAtUtc = envelope.UpdatedAtUtc;
    }

    public void SwitchFilePath(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var targetPath = Path.GetFullPath(filePath);
        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundException(
                L.Get("service.vault.error.newFileMissing"),
                targetPath);
        }

        _filePath = targetPath;
        _mustAlreadyExist = true;
        _pendingInitialCreate = false;
    }

    public void ChangePassword(VaultPayload payload, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException(
                L.Get("service.vault.error.newPasswordRequired"),
                nameof(newPassword));
        }

        var salt = RandomNumberGenerator.GetBytes(CryptoService.SaltSize);
        var key = CryptoService.DeriveKey(
            newPassword,
            salt,
            CryptoService.PasswordIterations);
        ChangeProtection(
            payload,
            VaultProtectionMode.Password,
            key,
            storedKey: null,
            salt,
            CryptoService.PasswordIterations);
    }

    public void EnableWindowsAccountProtection(VaultPayload payload)
    {
        if (_mode == VaultProtectionMode.WindowsAccount)
        {
            return;
        }

        var key = CryptoService.CreateRandomKey();
        byte[] protectedKey;
        try
        {
            protectedKey = _windowsAccountProtector.Protect(key);
        }
        catch (Exception exception) when (
            exception is CryptographicException or PlatformNotSupportedException)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new VaultAuthenticationException(
                L.Get("service.vault.error.windowsProtectionUnavailable"),
                exception);
        }

        ChangeProtection(
            payload,
            VaultProtectionMode.WindowsAccount,
            key,
            protectedKey,
            salt: null,
            iterations: 0);
    }

    public void UsePortableProtection(VaultPayload payload)
    {
        if (_mode == VaultProtectionMode.Portable)
        {
            return;
        }

        var key = CryptoService.CreateRandomKey();
        ChangeProtection(
            payload,
            VaultProtectionMode.Portable,
            key,
            key.ToArray(),
            salt: null,
            iterations: 0);
    }

    public void DisableWindowsAccountProtection(VaultPayload payload)
    {
        UsePortableProtection(payload);
    }

    public void RemovePassword(VaultPayload payload)
    {
        UsePortableProtection(payload);
    }

    public void Lock()
    {
        if (_sessionKey is not null)
        {
            CryptographicOperations.ZeroMemory(_sessionKey);
            _sessionKey = null;
        }

        if (_mode == VaultProtectionMode.Portable && _storedKey is not null)
        {
            CryptographicOperations.ZeroMemory(_storedKey);
            _storedKey = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Lock();
        ReplaceStoredKey(null);
        ReplaceSalt(null);

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void EnsureOpen(VaultPayload payload)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(payload);
        if (_sessionKey is null)
        {
            throw new InvalidOperationException(
                L.Get("service.vault.error.locked"));
        }
    }

    private void InitializePortableKey()
    {
        var key = CryptoService.CreateRandomKey();
        ReplaceSessionKey(key);
        _mode = VaultProtectionMode.Portable;
        ReplaceStoredKey(key.ToArray());
        ReplaceSalt(null);
        _iterations = 0;
    }

    private byte[] UnprotectWindowsKey(string? encoded)
    {
        var protectedKey = DecodeRequired(encoded, "protectedKey");
        var key = _windowsAccountProtector.Unprotect(protectedKey);
        ValidateKeyLength(key, "Windows protected key");
        return key;
    }

    private static byte[] DecodePortableKey(string? encoded)
    {
        var key = DecodeRequired(encoded, "protectedKey");
        ValidateKeyLength(key, "Portable key");
        return key;
    }

    private static void ValidateKeyLength(byte[] key, string description)
    {
        if (key.Length == CryptoService.KeySize)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(key);
        throw new CryptographicException($"{description} has an invalid length.");
    }

    private void ChangeProtection(
        VaultPayload payload,
        VaultProtectionMode mode,
        byte[] key,
        byte[]? storedKey,
        byte[]? salt,
        int iterations)
    {
        EnsureOpen(payload);

        try
        {
            var envelope = CreateEnvelope(
                payload,
                key,
                mode,
                storedKey,
                salt,
                iterations);
            if (_pendingInitialCreate)
            {
                WriteNewFile(_filePath, envelope.Json);
            }
            else
            {
                WriteProtectionChange(_filePath, envelope.Json);
            }

            payload.UpdatedAtUtc = envelope.UpdatedAtUtc;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(key);
            if (storedKey is not null)
            {
                CryptographicOperations.ZeroMemory(storedKey);
            }

            if (salt is not null)
            {
                CryptographicOperations.ZeroMemory(salt);
            }

            throw;
        }

        ReplaceSessionKey(key);
        _mode = mode;
        ReplaceStoredKey(storedKey);
        ReplaceSalt(salt);
        _iterations = iterations;
        _pendingInitialCreate = false;
    }

    private static SerializedEnvelope CreateEnvelope(
        VaultPayload payload,
        byte[] key,
        VaultProtectionMode mode,
        byte[]? storedKey,
        byte[]? salt,
        int iterations)
    {
        var previousUpdatedAtUtc = payload.UpdatedAtUtc;
        var updatedAtUtc = DateTime.UtcNow;
        byte[]? plaintext = null;
        try
        {
            payload.UpdatedAtUtc = updatedAtUtc;
            payload.Normalize();
            plaintext = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            var encrypted = CryptoService.Encrypt(plaintext, key, AssociatedData);
            var envelope = new VaultEnvelope
            {
                Magic = Magic,
                Version = FormatVersion,
                Protection = mode,
                ProtectedKey = storedKey is null ? null : Convert.ToBase64String(storedKey),
                Salt = salt is null ? null : Convert.ToBase64String(salt),
                Iterations = mode == VaultProtectionMode.Password ? iterations : 0,
                Nonce = Convert.ToBase64String(encrypted.Nonce),
                Tag = Convert.ToBase64String(encrypted.Tag),
                Ciphertext = Convert.ToBase64String(encrypted.Ciphertext),
                UpdatedAtUtc = updatedAtUtc
            };

            return new SerializedEnvelope(
                JsonSerializer.Serialize(envelope, JsonOptions),
                updatedAtUtc);
        }
        finally
        {
            payload.UpdatedAtUtc = previousUpdatedAtUtc;
            if (plaintext is not null)
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
    }

    private void ReplaceSessionKey(byte[] key)
    {
        if (_sessionKey is not null)
        {
            CryptographicOperations.ZeroMemory(_sessionKey);
        }

        _sessionKey = key;
    }

    private void ReplaceStoredKey(byte[]? storedKey)
    {
        if (_storedKey is not null)
        {
            CryptographicOperations.ZeroMemory(_storedKey);
        }

        _storedKey = storedKey;
    }

    private void ReplaceSalt(byte[]? salt)
    {
        if (_salt is not null)
        {
            CryptographicOperations.ZeroMemory(_salt);
        }

        _salt = salt;
    }

    private static VaultEnvelope ReadEnvelope(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<VaultEnvelope>(json, JsonOptions)
            ?? throw new JsonException("Vault envelope is empty.");
    }

    private static void ValidateEnvelope(VaultEnvelope envelope)
    {
        if (!string.Equals(envelope.Magic, Magic, StringComparison.Ordinal) ||
            envelope.Version != FormatVersion)
        {
            throw new JsonException("Unsupported vault format.");
        }

        _ = DecodeRequired(envelope.Nonce, "nonce");
        _ = DecodeRequired(envelope.Tag, "tag");
        _ = DecodeRequired(envelope.Ciphertext, "ciphertext");

        if (envelope.Protection == VaultProtectionMode.Password)
        {
            _ = DecodeRequired(envelope.Salt, "salt");
            if (envelope.Iterations < 100_000)
            {
                throw new JsonException("Invalid password KDF configuration.");
            }
        }
        else if (envelope.Protection is
                 VaultProtectionMode.Portable or
                 VaultProtectionMode.WindowsAccount)
        {
            _ = DecodeRequired(envelope.ProtectedKey, "protectedKey");
        }
        else
        {
            throw new JsonException("Unknown vault protection mode.");
        }
    }

    private static byte[] DecodeRequired(string? encoded, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            throw new FormatException($"Encrypted document is missing {fieldName}.");
        }

        return Convert.FromBase64String(encoded);
    }

    private static void WriteAtomic(string path, string content, bool createBackup)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new IOException(
                L.Get("service.vault.error.parentDirectoryMissing"));
        Directory.CreateDirectory(directory);

        var temporaryPath = path + ".tmp";
        var backupPath = path + ".bak";
        try
        {
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));
            if (createBackup && File.Exists(path))
            {
                File.Copy(path, backupPath, true);
            }

            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void WriteProtectionChange(string path, string content)
    {
        var backupPath = path + ".bak";
        WriteAtomic(backupPath, content, createBackup: false);
        WriteAtomic(path, content, createBackup: false);
    }

    private static void WriteNewFile(string path, string content)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new IOException(
                L.Get("service.vault.error.parentDirectoryMissing"));
        Directory.CreateDirectory(directory);

        var temporaryPath = path + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));
            File.Move(temporaryPath, path, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool HasRecoveryArtifacts(string path)
    {
        if (File.Exists(path + ".bak") ||
            File.Exists(path + ".tmp") ||
            File.Exists(path + ".bak.tmp"))
        {
            return true;
        }

        var directory = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);
        return directory is not null &&
               Directory.Exists(directory) &&
               Directory.EnumerateFiles(directory, fileName + ".tmp.*").Any();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed class VaultEnvelope
    {
        public string Magic { get; set; } = string.Empty;
        public int Version { get; set; }
        public VaultProtectionMode Protection { get; set; }
        public string? ProtectedKey { get; set; }
        public string? Salt { get; set; }
        public int Iterations { get; set; }
        public string Nonce { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Ciphertext { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
    }

    private sealed record SerializedEnvelope(string Json, DateTime UpdatedAtUtc);
}
