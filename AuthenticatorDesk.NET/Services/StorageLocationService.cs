using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthenticatorDesk.Localization;

namespace AuthenticatorDesk.Services;

public enum StorageLocationSource
{
    ProgramDirectory,
    ExplicitConfiguration,
    LegacyCompatibility
}

public sealed record StorageLocation(
    string DataDirectory,
    StorageLocationSource Source,
    string? ConfigurationFile);

public sealed class StorageLocationService
{
    public const string LocatorFileName = "AuthenticatorDesk.paths.json";
    public const string VaultFileName = "vault.json";

    private const int LocatorVersion = 1;
    private const string UserLocatorDirectoryName = "locations";

    private readonly string _programDirectory;
    private readonly string _legacyDataDirectory;

    public StorageLocationService(string programDirectory, string legacyDataDirectory)
    {
        _programDirectory = NormalizeDirectory(programDirectory);
        _legacyDataDirectory = NormalizeDirectory(legacyDataDirectory);
    }

    public string ProgramDirectory => _programDirectory;

    public string LegacyDataDirectory => _legacyDataDirectory;

    public string ProgramLocatorFile => Path.Combine(_programDirectory, LocatorFileName);

    public string UserLocatorFile => Path.Combine(
        _legacyDataDirectory,
        UserLocatorDirectoryName,
        ProgramDirectoryId(_programDirectory) + ".json");

    public StorageLocation Resolve()
    {
        if (File.Exists(ProgramLocatorFile))
        {
            var configured = ReadConfiguredLocation(ProgramLocatorFile);
            EnsureConfiguredVaultExists(configured);
            return configured;
        }

        if (File.Exists(UserLocatorFile))
        {
            var configured = ReadConfiguredLocation(UserLocatorFile);
            EnsureConfiguredVaultExists(configured);
            return configured;
        }

        var programVault = Path.Combine(_programDirectory, VaultFileName);
        var legacyVault = Path.Combine(_legacyDataDirectory, VaultFileName);
        if (File.Exists(programVault))
        {
            if (!PathsEqual(_legacyDataDirectory, _programDirectory))
            {
                TryArchiveMatchingLegacyVault(legacyVault, programVault);
            }

            return new StorageLocation(
                _programDirectory,
                StorageLocationSource.ProgramDirectory,
                null);
        }

        ThrowIfRecoveryArtifactsExist(_programDirectory);

        if (!PathsEqual(_legacyDataDirectory, _programDirectory))
        {
            var legacyMigrationCompleted =
                HasMigratedVaultArchive(_legacyDataDirectory);
            if (File.Exists(legacyVault))
            {
                MigrateLegacyVault(legacyVault, programVault);
                return new StorageLocation(
                    _programDirectory,
                    StorageLocationSource.ProgramDirectory,
                    null);
            }

            if (!legacyMigrationCompleted)
            {
                ThrowIfRecoveryArtifactsExist(_legacyDataDirectory);
            }
        }

        return new StorageLocation(
            _programDirectory,
            StorageLocationSource.ProgramDirectory,
            null);
    }

    public StorageLocation Persist(string dataDirectory)
    {
        var normalized = NormalizeDirectory(dataDirectory);
        EnsureConfiguredVaultExists(new StorageLocation(
            normalized,
            StorageLocationSource.ExplicitConfiguration,
            null));
        if (PathsEqual(normalized, _programDirectory))
        {
            DeleteLocatorIfPresent(ProgramLocatorFile);
            DeleteLocatorIfPresent(UserLocatorFile);
            return new StorageLocation(
                normalized,
                StorageLocationSource.ProgramDirectory,
                null);
        }

        var document = new LocatorDocument
        {
            Version = LocatorVersion,
            DataDirectory = ToStoredPath(normalized)
        };

        try
        {
            WriteLocator(ProgramLocatorFile, document);
            TryDeleteStaleLocator(UserLocatorFile);
            return new StorageLocation(
                normalized,
                StorageLocationSource.ExplicitConfiguration,
                ProgramLocatorFile);
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException or IOException &&
            !File.Exists(ProgramLocatorFile))
        {
            WriteLocator(UserLocatorFile, document);
            return new StorageLocation(
                normalized,
                StorageLocationSource.ExplicitConfiguration,
                UserLocatorFile);
        }
    }

    public static string NormalizeDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                L.Get("service.storage.error.dataDirectoryRequired"),
                nameof(path));
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim()));
    }

    public static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            NormalizeDirectory(left),
            NormalizeDirectory(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private StorageLocation ReadConfiguredLocation(string filePath)
    {
        LocatorDocument document;
        try
        {
            document = JsonSerializer.Deserialize<LocatorDocument>(
                    File.ReadAllText(filePath, Encoding.UTF8))
                ?? throw new JsonException(
                    L.Get("service.storage.error.pathConfigurationEmpty"));
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            throw new InvalidDataException(
                L.Format(
                    "service.storage.error.pathConfigurationUnreadable",
                    filePath),
                exception);
        }

        if (document.Version != LocatorVersion ||
            string.IsNullOrWhiteSpace(document.DataDirectory))
        {
            throw new InvalidDataException(
                L.Format(
                    "service.storage.error.pathConfigurationInvalidFormat",
                    filePath));
        }

        try
        {
            var configured = document.DataDirectory.Trim();
            var resolved = Path.IsPathRooted(configured)
                ? NormalizeDirectory(configured)
                : NormalizeDirectory(Path.Combine(_programDirectory, configured));
            return new StorageLocation(
                resolved,
                StorageLocationSource.ExplicitConfiguration,
                filePath);
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidDataException(
                L.Format(
                    "service.storage.error.pathConfigurationInvalidPath",
                    filePath),
                exception);
        }
    }

    private string ToStoredPath(string dataDirectory)
    {
        var relative = Path.GetRelativePath(_programDirectory, dataDirectory);
        var parentPrefix = ".." + Path.DirectorySeparatorChar;
        var alternateParentPrefix = ".." + Path.AltDirectorySeparatorChar;
        if (!Path.IsPathRooted(relative) &&
            relative != ".." &&
            !relative.StartsWith(parentPrefix, StringComparison.Ordinal) &&
            !relative.StartsWith(alternateParentPrefix, StringComparison.Ordinal))
        {
            return relative;
        }

        return dataDirectory;
    }

    private static void WriteLocator(string filePath, LocatorDocument document)
    {
        var directory = Path.GetDirectoryName(filePath)
            ?? throw new IOException(
                L.Get("service.storage.error.configurationParentMissing"));
        Directory.CreateDirectory(directory);

        var temporaryPath = filePath + ".tmp";
        var json = JsonSerializer.Serialize(
            document,
            new JsonSerializerOptions { WriteIndented = true });
        try
        {
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            File.Move(temporaryPath, filePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void DeleteLocatorIfPresent(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static void TryDeleteStaleLocator(string filePath)
    {
        try
        {
            DeleteLocatorIfPresent(filePath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // The program-side locator has priority over this scoped fallback.
        }
    }

    private static void MigrateLegacyVault(string sourcePath, string targetPath)
    {
        CopyVaultWithoutOverwrite(sourcePath, targetPath);

        try
        {
            ArchiveLegacyVault(sourcePath);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // The durable program copy is already committed. Keep using it and
            // retry archiving the byte-identical legacy source on the next start.
        }
    }

    private static void TryArchiveMatchingLegacyVault(
        string legacyVault,
        string programVault)
    {
        if (!File.Exists(legacyVault))
        {
            return;
        }

        try
        {
            if (FilesHaveSameContents(legacyVault, programVault))
            {
                ArchiveLegacyVault(legacyVault);
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // The program vault is already usable. Leave the legacy source untouched
            // and retry this best-effort cleanup on the next start.
        }
    }

    private static void ArchiveLegacyVault(string sourcePath)
    {
        var sourceDirectory = Path.GetDirectoryName(sourcePath)
            ?? throw new IOException(
                L.Get("service.storage.error.legacyVaultParentMissing"));
        var archivePath = Path.Combine(
            sourceDirectory,
            $"{VaultFileName}.migrated-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.bak");
        File.Move(sourcePath, archivePath, overwrite: false);
    }

    private static bool FilesHaveSameContents(string leftPath, string rightPath)
    {
        if (new FileInfo(leftPath).Length != new FileInfo(rightPath).Length)
        {
            return false;
        }

        using var left = new FileStream(
            leftPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        using var right = new FileStream(
            rightPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        var leftHash = SHA256.HashData(left);
        var rightHash = SHA256.HashData(right);
        return CryptographicOperations.FixedTimeEquals(leftHash, rightHash);
    }

    private static void CopyVaultWithoutOverwrite(string sourcePath, string targetPath)
    {
        var targetDirectory = Path.GetDirectoryName(targetPath)
            ?? throw new IOException(
                L.Get("service.storage.error.targetVaultParentMissing"));
        Directory.CreateDirectory(targetDirectory);

        var temporaryPath = targetPath + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            using (var source = new FileStream(
                       sourcePath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read))
            using (var target = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                source.CopyTo(target);
                target.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, targetPath, overwrite: false);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException(
                L.Format(
                    "service.storage.error.copyToApplicationDirectoryFailed",
                    sourcePath),
                exception);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void EnsureConfiguredVaultExists(StorageLocation location)
    {
        var vaultFile = Path.Combine(location.DataDirectory, VaultFileName);
        if (!File.Exists(vaultFile))
        {
            throw new InvalidDataException(
                L.Format(
                    "service.storage.error.configuredVaultMissing",
                    location.DataDirectory));
        }
    }

    private static void ThrowIfRecoveryArtifactsExist(string dataDirectory)
    {
        var vaultFile = Path.Combine(dataDirectory, VaultFileName);
        if (File.Exists(vaultFile + ".bak") ||
            File.Exists(vaultFile + ".tmp") ||
            File.Exists(vaultFile + ".bak.tmp") ||
            Directory.Exists(dataDirectory) &&
            (Directory.EnumerateFiles(
                 dataDirectory,
                 VaultFileName + ".tmp.*").Any() ||
             Directory.EnumerateFiles(
                 dataDirectory,
                 VaultFileName + ".migrated-*.bak").Any()))
        {
            throw new InvalidDataException(
                L.Format(
                    "service.storage.error.recoveryArtifactsWithoutVault",
                    dataDirectory));
        }
    }

    private static bool HasMigratedVaultArchive(string dataDirectory)
    {
        return Directory.Exists(dataDirectory) &&
               Directory.EnumerateFiles(
                       dataDirectory,
                       VaultFileName + ".migrated-*.bak")
                   .Any();
    }

    private static string ProgramDirectoryId(string programDirectory)
    {
        var normalizedIdentity = NormalizeDirectory(programDirectory).ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedIdentity));
        return Convert.ToHexString(hash.AsSpan(0, 12)).ToLowerInvariant();
    }

    private sealed class LocatorDocument
    {
        public int Version { get; set; }

        public string DataDirectory { get; set; } = string.Empty;
    }
}
