using System.Globalization;
using System.Security;
using System.Text;
using System.Text.Json;

namespace AuthenticatorDesk.Localization;

public static class LanguagePreferenceStore
{
    private const int PreferenceSchemaVersion = 1;
    private const int MaximumPreferenceFileBytes = 16 * 1024;
    private const int MaximumLanguageCodeLength = 64;
    private const string PreferenceFileName = "ui-preferences.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AuthenticatorDesk",
        PreferenceFileName);

    public static string? Load()
    {
        try
        {
            var filePath = FilePath;
            if (!File.Exists(filePath))
            {
                return null;
            }

            var bytes = ReadLimited(filePath, MaximumPreferenceFileBytes);
            if (bytes is null)
            {
                return null;
            }

            var document = JsonSerializer.Deserialize<PreferenceDocument>(
                bytes,
                JsonOptions);
            if (document?.SchemaVersion != PreferenceSchemaVersion)
            {
                return null;
            }

            return NormalizePreference(document.Language);
        }
        catch (Exception exception) when (IsExpectedStorageFailure(exception))
        {
            return null;
        }
    }

    public static bool Save(string? preference)
    {
        string? temporaryPath = null;
        var saved = false;
        try
        {
            var filePath = FilePath;
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(
                directory,
                $".{PreferenceFileName}.{Guid.NewGuid():N}.tmp");
            var document = new PreferenceDocument
            {
                SchemaVersion = PreferenceSchemaVersion,
                Language = NormalizePreference(preference) ?? L.SystemLanguage
            };
            var bytes = JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);

            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, filePath, overwrite: true);
            temporaryPath = null;
            saved = true;
        }
        catch (Exception exception) when (IsExpectedStorageFailure(exception))
        {
            // Language selection is non-critical and must never block startup.
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (Exception exception) when (IsExpectedStorageFailure(exception))
                {
                    // Best effort cleanup only.
                }
            }
        }

        return saved;
    }

    private static byte[]? ReadLimited(string filePath, int maximumBytes)
    {
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        if (stream.CanSeek && stream.Length > maximumBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[4 * 1024];
        while (true)
        {
            var read = stream.Read(chunk, 0, chunk.Length);
            if (read == 0)
            {
                return buffer.ToArray();
            }

            if (buffer.Length + read > maximumBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }
    }

    private static string? NormalizePreference(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference))
        {
            return null;
        }

        var trimmed = preference.Trim();
        if (trimmed.Equals(L.SystemLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return L.SystemLanguage;
        }

        if (trimmed.Length > MaximumLanguageCodeLength ||
            trimmed.Any(char.IsControl))
        {
            return null;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(trimmed);
            return string.IsNullOrEmpty(culture.Name) ? null : culture.Name;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static bool IsExpectedStorageFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or
            SecurityException or JsonException or ArgumentException or
            NotSupportedException;

    private sealed class PreferenceDocument
    {
        public int SchemaVersion { get; set; }

        public string? Language { get; set; }
    }
}
