using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.Json;

namespace AuthenticatorDesk.Localization;

public static class L
{
    public const string SystemLanguage = "system";

    private const string EnglishLanguage = "en";
    private const int SupportedPackSchemaVersion = 1;
    private const int MaximumPackBytes = 512 * 1024;
    private const int MaximumStringCount = 5_000;
    private const int MaximumStringValueLength = 4_096;
    private const int MaximumStringKeyLength = 256;
    private const int MaximumLanguageCodeLength = 64;
    private const int MaximumLanguageNameLength = 128;

    private static readonly object SyncRoot = new();

    private static LocalizationSnapshot _snapshot = LocalizationSnapshot.Empty;
    private static IReadOnlyList<LanguageInfo> _availableLanguages =
        Array.AsReadOnly([
            new LanguageInfo(EnglishLanguage, "English", "English")
        ]);
    private static string[] _lookupLanguages = [EnglishLanguage];
    private static string _requestedLanguage = SystemLanguage;
    private static string _currentLanguage = EnglishLanguage;
    private static string? _detectedSystemLanguage;
    private static bool _initialized;

    public static event EventHandler? LanguageChanged;

    public static IReadOnlyList<LanguageInfo> AvailableLanguages
    {
        get
        {
            EnsureInitialized();
            lock (SyncRoot)
            {
                return _availableLanguages;
            }
        }
    }

    public static string CurrentLanguage
    {
        get
        {
            EnsureInitialized();
            lock (SyncRoot)
            {
                return _currentLanguage;
            }
        }
    }

    public static string RequestedLanguage
    {
        get
        {
            EnsureInitialized();
            lock (SyncRoot)
            {
                return _requestedLanguage;
            }
        }
    }

    public static void Initialize(string? preference)
    {
        var detectedSystemLanguage = DetectSystemLanguage();
        LocalizationSnapshot snapshot;
        try
        {
            snapshot = LoadSnapshot();
        }
        catch (Exception exception) when (IsRecoverablePackFailure(exception))
        {
            ReportPackFailure("Unable to initialize language packs.", exception);
            snapshot = LocalizationSnapshot.Empty;
        }

        string currentLanguage;
        lock (SyncRoot)
        {
            _detectedSystemLanguage ??= detectedSystemLanguage;
            _snapshot = snapshot;
            _availableLanguages = CreateAvailableLanguages(snapshot);
            _initialized = true;
            SelectLanguageNoLock(preference);
            currentLanguage = _currentLanguage;
        }

        ApplyUiCulture(currentLanguage);
        RaiseLanguageChanged();
    }

    public static void SetLanguage(string? preference)
    {
        string? currentLanguage = null;
        lock (SyncRoot)
        {
            if (_initialized)
            {
                SelectLanguageNoLock(preference);
                currentLanguage = _currentLanguage;
            }
        }

        if (currentLanguage is null)
        {
            Initialize(preference);
            return;
        }

        ApplyUiCulture(currentLanguage);
        RaiseLanguageChanged();
    }

    public static string Get(string key)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(key))
        {
            return key ?? string.Empty;
        }

        LocalizationSnapshot snapshot;
        string[] lookupLanguages;
        lock (SyncRoot)
        {
            snapshot = _snapshot;
            lookupLanguages = _lookupLanguages;
        }

        foreach (var language in lookupLanguages)
        {
            if (snapshot.Packs.TryGetValue(language, out var pack) &&
                pack.Strings.TryGetValue(key, out var localized))
            {
                return localized;
            }
        }

        return snapshot.EnglishBaseline.TryGetValue(key, out var english)
            ? english
            : key;
    }

    public static string Format(string key, params object?[] args)
    {
        args ??= [];
        var localized = Get(key);
        try
        {
            return string.Format(
                CultureInfo.CurrentUICulture,
                localized,
                args);
        }
        catch (FormatException)
        {
            var english = GetEnglish(key);
            try
            {
                return string.Format(
                    CultureInfo.GetCultureInfo(EnglishLanguage),
                    english,
                    args);
            }
            catch (FormatException)
            {
                return english;
            }
        }
    }

    private static LocalizationSnapshot LoadSnapshot()
    {
        var embeddedPacks =
            new Dictionary<string, MutableLanguagePack>(
                StringComparer.OrdinalIgnoreCase);
        LoadEmbeddedPacks(embeddedPacks);

        var englishBaseline =
            new Dictionary<string, string>(StringComparer.Ordinal);
        if (embeddedPacks.TryGetValue(EnglishLanguage, out var embeddedEnglish))
        {
            foreach (var pair in embeddedEnglish.Strings)
            {
                if (TryGetPlaceholderSignature(pair.Value, out _))
                {
                    englishBaseline[pair.Key] = pair.Value;
                }
            }
        }
        else
        {
            ReportPackFailure(
                "The embedded English language baseline was not found.");
        }

        var combinedPacks =
            new Dictionary<string, MutableLanguagePack>(
                StringComparer.OrdinalIgnoreCase);
        foreach (var pair in embeddedPacks)
        {
            combinedPacks[pair.Key] = pair.Value.Clone();
        }

        var programLanguageDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Languages");
        LoadExternalPacks(combinedPacks, programLanguageDirectory);

        var userLanguageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AuthenticatorDesk",
            "Languages");
        if (!userLanguageDirectory.Equals(
                programLanguageDirectory,
                StringComparison.OrdinalIgnoreCase))
        {
            // Per-user packs load last so a user can update or complete a
            // bundled language without write access to the program directory.
            LoadExternalPacks(combinedPacks, userLanguageDirectory);
        }
        ValidatePackStrings(combinedPacks, englishBaseline);

        var packs =
            new Dictionary<string, LanguagePack>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in combinedPacks)
        {
            packs[pair.Key] = pair.Value.ToLanguagePack();
        }

        if (!packs.ContainsKey(EnglishLanguage))
        {
            packs[EnglishLanguage] = new LanguagePack(
                new LanguageInfo(EnglishLanguage, "English", "English"),
                new Dictionary<string, string>(StringComparer.Ordinal));
        }

        return new LocalizationSnapshot(packs, englishBaseline);
    }

    private static void EnsureInitialized()
    {
        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }
        }

        Initialize(preference: null);
    }

    private static void LoadEmbeddedPacks(
        Dictionary<string, MutableLanguagePack> target)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames;
        try
        {
            resourceNames = assembly.GetManifestResourceNames();
        }
        catch (Exception exception) when (IsRecoverablePackFailure(exception))
        {
            ReportPackFailure(
                "Unable to enumerate embedded language packs.",
                exception);
            return;
        }

        foreach (var resourceName in resourceNames
                     .Where(IsLanguageResourceName)
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                {
                    continue;
                }

                var bytes = ReadLimited(stream, MaximumPackBytes);
                if (bytes is null)
                {
                    ReportPackFailure(
                        $"Embedded language pack '{resourceName}' exceeds " +
                        $"{MaximumPackBytes} bytes.");
                    continue;
                }

                var pack = ParsePack(bytes, resourceName, expectedLocale: null);
                if (pack is not null)
                {
                    MergePack(target, pack);
                }
            }
            catch (Exception exception) when (IsRecoverablePackFailure(exception))
            {
                ReportPackFailure(
                    $"Unable to load embedded language pack '{resourceName}'.",
                    exception);
            }
        }
    }

    private static void LoadExternalPacks(
        Dictionary<string, MutableLanguagePack> target,
        string languageDirectory)
    {
        string[] files;
        try
        {
            if (!Directory.Exists(languageDirectory))
            {
                return;
            }

            files = Directory.GetFiles(
                languageDirectory,
                "*.json",
                SearchOption.TopDirectoryOnly);
        }
        catch (Exception exception) when (IsRecoverablePackFailure(exception))
        {
            ReportPackFailure(
                $"Unable to enumerate language directory '{languageDirectory}'.",
                exception);
            return;
        }

        foreach (var filePath in files.OrderBy(
                     path => path,
                     StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (Path.GetFileName(filePath).EndsWith(
                        ".schema.json",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var expectedLocale = NormalizeLocale(
                    Path.GetFileNameWithoutExtension(filePath));
                if (expectedLocale is null)
                {
                    ReportPackFailure(
                        $"Language pack filename '{Path.GetFileName(filePath)}' " +
                        "is not a valid BCP-47 language code.");
                    continue;
                }

                using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                var bytes = ReadLimited(stream, MaximumPackBytes);
                if (bytes is null)
                {
                    ReportPackFailure(
                        $"Language pack '{filePath}' exceeds " +
                        $"{MaximumPackBytes} bytes.");
                    continue;
                }

                var pack = ParsePack(bytes, filePath, expectedLocale);
                if (pack is not null)
                {
                    // External packs intentionally override embedded packs.
                    MergePack(target, pack);
                }
            }
            catch (Exception exception) when (IsRecoverablePackFailure(exception))
            {
                ReportPackFailure(
                    $"Unable to load language pack '{filePath}'.",
                    exception);
            }
        }
    }

    private static RawLanguagePack? ParsePack(
        byte[] bytes,
        string source,
        string? expectedLocale)
    {
        try
        {
            using var document = JsonDocument.Parse(
                bytes,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 8
                });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                ReportPackFailure(
                    $"Language pack '{source}' must contain a JSON object.");
                return null;
            }

            int? schemaVersion = null;
            string? locale = null;
            string? name = null;
            string? nativeName = null;
            JsonElement stringsElement = default;
            var hasStrings = false;
            var rootProperties = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!rootProperties.Add(property.Name))
                {
                    ReportPackFailure(
                        $"Language pack '{source}' contains duplicate root " +
                        $"property '{property.Name}'.");
                    return null;
                }

                switch (property.Name)
                {
                    case "schemaVersion":
                        if (property.Value.ValueKind != JsonValueKind.Number ||
                            !property.Value.TryGetInt32(out var parsedVersion))
                        {
                            return InvalidPack(
                                source,
                                "'schemaVersion' must be an integer.");
                        }

                        schemaVersion = parsedVersion;
                        break;

                    case "locale":
                        locale = ReadMetadataString(property.Value);
                        break;

                    case "name":
                        name = ReadMetadataString(property.Value);
                        break;

                    case "nativeName":
                        nativeName = ReadMetadataString(property.Value);
                        break;

                    case "strings":
                        stringsElement = property.Value;
                        hasStrings = true;
                        break;
                }
            }

            if (schemaVersion != SupportedPackSchemaVersion)
            {
                return InvalidPack(
                    source,
                    $"unsupported schema version '{schemaVersion?.ToString() ?? "missing"}'.");
            }

            var normalizedLocale = NormalizeLocale(locale);
            if (normalizedLocale is null)
            {
                return InvalidPack(source, "'locale' is not a valid BCP-47 code.");
            }

            if (expectedLocale is not null &&
                !normalizedLocale.Equals(
                    expectedLocale,
                    StringComparison.OrdinalIgnoreCase))
            {
                return InvalidPack(
                    source,
                    $"'locale' must match filename '{expectedLocale}'.");
            }

            name = NormalizeLanguageName(name);
            nativeName = NormalizeLanguageName(nativeName);
            if (name is null || nativeName is null)
            {
                return InvalidPack(
                    source,
                    "'name' and 'nativeName' must be non-empty strings no " +
                    $"longer than {MaximumLanguageNameLength} characters.");
            }

            if (!hasStrings ||
                stringsElement.ValueKind != JsonValueKind.Object)
            {
                return InvalidPack(source, "'strings' must be a JSON object.");
            }

            var strings = new Dictionary<string, string>(StringComparer.Ordinal);
            var invalidKeys = new HashSet<string>(StringComparer.Ordinal);
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);
            var stringCount = 0;
            foreach (var property in stringsElement.EnumerateObject())
            {
                stringCount++;
                if (stringCount > MaximumStringCount)
                {
                    return InvalidPack(
                        source,
                        $"'strings' exceeds {MaximumStringCount} entries.");
                }

                if (!seenKeys.Add(property.Name))
                {
                    return InvalidPack(
                        source,
                        $"'strings' contains duplicate key '{property.Name}'.");
                }

                if (string.IsNullOrWhiteSpace(property.Name) ||
                    property.Name.Length > MaximumStringKeyLength ||
                    property.Name.Any(char.IsControl))
                {
                    invalidKeys.Add(property.Name);
                    continue;
                }

                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    invalidKeys.Add(property.Name);
                    continue;
                }

                var value = property.Value.GetString() ?? string.Empty;
                if (value.Length > MaximumStringValueLength)
                {
                    invalidKeys.Add(property.Name);
                    continue;
                }

                strings[property.Name] = value;
            }

            return new RawLanguagePack(
                new LanguageInfo(normalizedLocale, name, nativeName),
                strings,
                invalidKeys);
        }
        catch (JsonException exception)
        {
            ReportPackFailure(
                $"Language pack '{source}' contains invalid JSON.",
                exception);
            return null;
        }
    }

    private static void MergePack(
        Dictionary<string, MutableLanguagePack> target,
        RawLanguagePack rawPack)
    {
        if (!target.TryGetValue(rawPack.Info.Code, out var existing))
        {
            existing = new MutableLanguagePack(rawPack.Info);
            target[rawPack.Info.Code] = existing;
        }

        existing.Info = rawPack.Info;
        foreach (var invalidKey in rawPack.InvalidKeys)
        {
            existing.Strings.Remove(invalidKey);
        }

        foreach (var pair in rawPack.Strings)
        {
            existing.Strings[pair.Key] = pair.Value;
        }
    }

    private static void ValidatePackStrings(
        Dictionary<string, MutableLanguagePack> packs,
        IReadOnlyDictionary<string, string> englishBaseline)
    {
        foreach (var pack in packs.Values)
        {
            foreach (var key in pack.Strings.Keys.ToArray())
            {
                var translated = pack.Strings[key];
                if (!TryGetPlaceholderSignature(translated, out var translatedSignature))
                {
                    pack.Strings.Remove(key);
                    continue;
                }

                if (!englishBaseline.TryGetValue(key, out var english) ||
                    !TryGetPlaceholderSignature(english, out var englishSignature) ||
                    !PlaceholderSignaturesEqual(
                        englishSignature,
                        translatedSignature))
                {
                    // A malformed translation falls through to the next
                    // language in the chain, ending at embedded English.
                    pack.Strings.Remove(key);
                }
            }
        }
    }

    private static bool TryGetPlaceholderSignature(
        string value,
        out Dictionary<string, int> signature)
    {
        signature = new Dictionary<string, int>(StringComparer.Ordinal);
        try
        {
            _ = CompositeFormat.Parse(value);
        }
        catch (FormatException)
        {
            return false;
        }

        var index = 0;
        while (index < value.Length)
        {
            if (value[index] == '{')
            {
                var placeholderStart = index;
                if (index + 1 < value.Length && value[index + 1] == '{')
                {
                    index += 2;
                    continue;
                }

                index++;
                while (index < value.Length && char.IsWhiteSpace(value[index]))
                {
                    index++;
                }

                var numberStart = index;
                while (index < value.Length && char.IsAsciiDigit(value[index]))
                {
                    index++;
                }

                if (numberStart == index ||
                    !int.TryParse(
                        value.AsSpan(numberStart, index - numberStart),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out _))
                {
                    return false;
                }

                var inFormatSpecifier = false;
                var closed = false;
                while (index < value.Length)
                {
                    if (value[index] == ':' && !inFormatSpecifier)
                    {
                        inFormatSpecifier = true;
                        index++;
                        continue;
                    }

                    if (inFormatSpecifier &&
                        value[index] == '{' &&
                        index + 1 < value.Length &&
                        value[index + 1] == '{')
                    {
                        index += 2;
                        continue;
                    }

                    if (inFormatSpecifier &&
                        value[index] == '}' &&
                        index + 1 < value.Length &&
                        value[index + 1] == '}')
                    {
                        index += 2;
                        continue;
                    }

                    if (value[index] == '}')
                    {
                        index++;
                        closed = true;
                        break;
                    }

                    index++;
                }

                if (!closed)
                {
                    return false;
                }

                // Keep the complete format item, not only its argument index.
                // This permits reordering but prevents an external pack from
                // introducing an unbounded alignment or format width.
                var formatItem = value[
                    placeholderStart..index];
                signature[formatItem] =
                    signature.GetValueOrDefault(formatItem) + 1;

                continue;
            }

            if (value[index] == '}' &&
                index + 1 < value.Length &&
                value[index + 1] == '}')
            {
                index += 2;
                continue;
            }

            index++;
        }

        return true;
    }

    private static bool PlaceholderSignaturesEqual(
        IReadOnlyDictionary<string, int> left,
        IReadOnlyDictionary<string, int> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var count) ||
                count != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static void SelectLanguageNoLock(string? preference)
    {
        _requestedLanguage = NormalizeRequestedLanguage(preference);
        var effectiveLanguage = _requestedLanguage == SystemLanguage
            ? _detectedSystemLanguage ?? EnglishLanguage
            : _requestedLanguage;
        var candidates = BuildFallbackChain(effectiveLanguage);
        var lookupLanguages = new List<string>();
        foreach (var candidate in candidates)
        {
            if (_snapshot.Packs.TryGetValue(candidate, out var pack) &&
                !lookupLanguages.Contains(
                    pack.Info.Code,
                    StringComparer.OrdinalIgnoreCase))
            {
                lookupLanguages.Add(pack.Info.Code);
            }
        }

        if (!lookupLanguages.Contains(
                EnglishLanguage,
                StringComparer.OrdinalIgnoreCase))
        {
            lookupLanguages.Add(EnglishLanguage);
        }

        _lookupLanguages = lookupLanguages.ToArray();
        _currentLanguage = _lookupLanguages[0];
    }

    private static IReadOnlyList<LanguageInfo> CreateAvailableLanguages(
        LocalizationSnapshot snapshot)
    {
        var languages = snapshot.Packs.Values
            .Select(pack => pack.Info)
            .OrderBy(
                info => info.Code.Equals(
                    EnglishLanguage,
                    StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1)
            .ThenBy(info => info.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return Array.AsReadOnly(languages);
    }

    private static IReadOnlyList<string> BuildFallbackChain(string language)
    {
        var normalized = NormalizeLocale(language) ?? EnglishLanguage;
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value) && seen.Add(value))
            {
                result.Add(value);
            }
        }

        Add(normalized);
        Add(GetChineseScriptFallback(normalized));

        try
        {
            var parent = CultureInfo.GetCultureInfo(normalized).Parent;
            while (!string.IsNullOrEmpty(parent.Name))
            {
                Add(parent.Name);
                parent = parent.Parent;
            }
        }
        catch (CultureNotFoundException)
        {
            // NormalizeLocale already rejected invalid cultures.
        }

        Add(EnglishLanguage);
        return result;
    }

    private static string? GetChineseScriptFallback(string language)
    {
        var subtags = language.Split('-');
        if (subtags.Length == 0 ||
            !subtags[0].Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (var subtag in subtags.Skip(1))
        {
            if (subtag.Equals("Hans", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-Hans";
            }

            if (subtag.Equals("Hant", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-Hant";
            }
        }

        foreach (var subtag in subtags.Skip(1))
        {
            if (subtag.Equals("TW", StringComparison.OrdinalIgnoreCase) ||
                subtag.Equals("HK", StringComparison.OrdinalIgnoreCase) ||
                subtag.Equals("MO", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-Hant";
            }

            if (subtag.Equals("CN", StringComparison.OrdinalIgnoreCase) ||
                subtag.Equals("SG", StringComparison.OrdinalIgnoreCase) ||
                subtag.Equals("MY", StringComparison.OrdinalIgnoreCase))
            {
                return "zh-Hans";
            }
        }

        return "zh-Hans";
    }

    private static string NormalizeRequestedLanguage(string? preference)
    {
        if (string.IsNullOrWhiteSpace(preference) ||
            preference.Trim().Equals(
                SystemLanguage,
                StringComparison.OrdinalIgnoreCase))
        {
            return SystemLanguage;
        }

        return NormalizeLocale(preference) ?? SystemLanguage;
    }

    private static string? NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return null;
        }

        var trimmed = locale.Trim();
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

    private static string DetectSystemLanguage() =>
        NormalizeLocale(CultureInfo.CurrentUICulture.Name) ?? EnglishLanguage;

    private static string GetEnglish(string key)
    {
        LocalizationSnapshot snapshot;
        lock (SyncRoot)
        {
            snapshot = _snapshot;
        }

        if (snapshot.Packs.TryGetValue(EnglishLanguage, out var englishPack) &&
            englishPack.Strings.TryGetValue(key, out var externalOrEmbedded))
        {
            return externalOrEmbedded;
        }

        return snapshot.EnglishBaseline.TryGetValue(key, out var english)
            ? english
            : key;
    }

    private static void ApplyUiCulture(string language)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.GetCultureInfo(EnglishLanguage);
        }

        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        try
        {
            AntdUI.Localization.SetLanguage(culture.Name);
        }
        catch (Exception exception)
        {
            ReportPackFailure(
                $"Unable to apply AntdUI language '{culture.Name}'.",
                exception);
        }
    }

    private static void RaiseLanguageChanged()
    {
        var handlers = LanguageChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler handler in handlers.GetInvocationList())
        {
            try
            {
                handler(null, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                Trace.TraceError(
                    $"A language change handler failed: {exception}");
            }
        }
    }

    private static byte[]? ReadLimited(Stream stream, int maximumBytes)
    {
        if (stream.CanSeek && stream.Length > maximumBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
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

    private static string? ReadMetadataString(JsonElement element) =>
        element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static string? NormalizeLanguageName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= MaximumLanguageNameLength &&
               !trimmed.Any(char.IsControl)
            ? trimmed
            : null;
    }

    private static bool IsLanguageResourceName(string resourceName)
    {
        var normalized = resourceName.Replace('\\', '.').Replace('/', '.');
        return normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
               (normalized.StartsWith(
                    "Languages.",
                    StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains(
                    ".Languages.",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static RawLanguagePack? InvalidPack(
        string source,
        string reason)
    {
        ReportPackFailure($"Language pack '{source}' is invalid: {reason}");
        return null;
    }

    private static bool IsRecoverablePackFailure(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or
            SecurityException or JsonException or ArgumentException or
            NotSupportedException or InvalidDataException;

    private static void ReportPackFailure(
        string message,
        Exception? exception = null)
    {
        try
        {
            Trace.TraceWarning(
                exception is null ? message : $"{message} {exception.Message}");
        }
        catch
        {
            // Diagnostics must not make localization a startup dependency.
        }
    }

    private sealed record RawLanguagePack(
        LanguageInfo Info,
        Dictionary<string, string> Strings,
        HashSet<string> InvalidKeys);

    private sealed record LanguagePack(
        LanguageInfo Info,
        IReadOnlyDictionary<string, string> Strings);

    private sealed class MutableLanguagePack
    {
        public MutableLanguagePack(LanguageInfo info)
        {
            Info = info;
        }

        public LanguageInfo Info { get; set; }

        public Dictionary<string, string> Strings { get; } =
            new(StringComparer.Ordinal);

        public MutableLanguagePack Clone()
        {
            var clone = new MutableLanguagePack(Info);
            foreach (var pair in Strings)
            {
                clone.Strings[pair.Key] = pair.Value;
            }

            return clone;
        }

        public LanguagePack ToLanguagePack() =>
            new(
                Info,
                new Dictionary<string, string>(
                    Strings,
                    StringComparer.Ordinal));
    }

    private sealed record LocalizationSnapshot(
        IReadOnlyDictionary<string, LanguagePack> Packs,
        IReadOnlyDictionary<string, string> EnglishBaseline)
    {
        public static LocalizationSnapshot Empty { get; } =
            new(
                new Dictionary<string, LanguagePack>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    [EnglishLanguage] = new(
                        new LanguageInfo(
                            EnglishLanguage,
                            "English",
                            "English"),
                        new Dictionary<string, string>(StringComparer.Ordinal))
                },
                new Dictionary<string, string>(StringComparer.Ordinal));
    }
}
