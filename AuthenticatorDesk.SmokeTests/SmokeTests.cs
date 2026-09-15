using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;
using AuthenticatorDesk.Services;
using AuthenticatorDesk.UI;
using AuthenticatorDesk.UI.Controls;
using AuthenticatorDesk.UI.Dialogs;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AuthenticatorDesk.SmokeTests;

public sealed class SmokeTests : IDisposable
{
    private const string TempDirectoryName = "AuthenticatorDesk-SmokeTests";
    private readonly string _testRoot;

    public SmokeTests()
    {
        L.Initialize("zh-Hans");
        _testRoot = Path.Combine(
            Path.GetTempPath(),
            TempDirectoryName,
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        if (_testRoot.StartsWith(
                Path.Combine(Path.GetTempPath(), TempDirectoryName),
                StringComparison.OrdinalIgnoreCase) &&
            Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [Fact]
    public void Base32() => RunOnStaThread(TestBase32);

    [Fact]
    public void ApplicationMetadataUsesGlobalValues()
    {
        var assembly = typeof(Program).Assembly;
        Equal(
            Program.AppVersion + ".0",
            assembly.GetName().Version?.ToString(4),
            "Assembly version uses the global application version");
        Equal(
            Program.AppVersion,
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion,
            "Informational version uses the global application version");
        Equal(
            Program.GitHubRepositoryUrl,
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "RepositoryUrl")
                .Value,
            "Repository metadata uses the global GitHub URL");
    }

    [Fact]
    public void LocalizationFallbacks() =>
        RunOnStaThread(TestLocalizationFallbacks);

    [Fact]
    public void BuiltInLanguagePacksAreComplete() =>
        RunOnStaThread(TestBuiltInLanguagePacksAreComplete);

    [Fact]
    public void RfcOtpVectors() => RunOnStaThread(TestRfcOtpVectors);

    [Fact]
    public void OtpUriRoundTrip() => RunOnStaThread(TestOtpUriRoundTrip);

    [Fact]
    public void GoogleMigrationUri() => RunOnStaThread(TestGoogleMigrationUri);

    [Fact]
    public void QrRoundTrip() => RunOnStaThread(TestQrRoundTrip);

    [Fact]
    public void LegacyWinAuthCipherFixture() =>
        RunOnStaThread(TestLegacyWinAuthCipherFixture);

    [Fact]
    public void WinAuthWindowsProtection() =>
        RunOnStaThread(TestWinAuthWindowsProtection);

    [Fact]
    public void WinAuthRoundTrip() =>
        RunOnStaThread(() => TestWinAuthRoundTrip(_testRoot));

    [Fact]
    public void WinAuthPerEntryPassword() =>
        RunOnStaThread(() => TestWinAuthPerEntryPassword(_testRoot));

    [Fact]
    public void BackupRoundTrip() =>
        RunOnStaThread(() => TestBackupRoundTrip(_testRoot));

    [Fact]
    public void PortableVaultDefaultAndFolderCopy() =>
        RunOnStaThread(() => TestPortableVaultDefaultAndFolderCopy(_testRoot));

    [Fact]
    public void InitialVaultSaveDoesNotOverwriteRace() =>
        RunOnStaThread(() => TestInitialVaultSaveDoesNotOverwriteRace(_testRoot));

    [Fact]
    public void VaultWindowsProtectionTransitions() =>
        RunOnStaThread(() => TestVaultWindowsProtectionTransitions(_testRoot));

    [Fact]
    public void VaultMissingAfterLockIsRejected() =>
        RunOnStaThread(() => TestVaultMissingAfterLockIsRejected(_testRoot));

    [Fact]
    public void PasswordRemovalUsesPortableProtection() =>
        RunOnStaThread(() => TestPasswordRemovalUsesPortableProtection(_testRoot));

    [Fact]
    public void StorageNewInstallDefaultsToProgramDirectory() =>
        RunOnStaThread(() => TestStorageNewInstallDefaultsToProgramDirectory(_testRoot));

    [Fact]
    public void StorageCompletedLegacyMigrationAllowsNewProgramDirectory() =>
        RunOnStaThread(
            () => TestStorageCompletedLegacyMigrationAllowsNewProgramDirectory(_testRoot));

    [Fact]
    public void StorageLegacyMainOverridesCompletedMigrationArchive() =>
        RunOnStaThread(
            () => TestStorageLegacyMainOverridesCompletedMigrationArchive(_testRoot));

    [Fact]
    public void StorageProgramDirectoryPriority() =>
        RunOnStaThread(() => TestStorageProgramDirectoryPriority(_testRoot));

    [Fact]
    public void StorageMatchingLegacyCopyIsArchived() =>
        RunOnStaThread(() => TestStorageMatchingLegacyCopyIsArchived(_testRoot));

    [Fact]
    public void StorageLegacyVaultMigratesToProgramDirectory() =>
        RunOnStaThread(() => TestStorageLegacyVaultMigratesToProgramDirectory(_testRoot));

    [Fact]
    public void StorageLegacyMigrationRaceRestoresSource() =>
        RunOnStaThread(() => TestStorageLegacyMigrationRaceRestoresSource(_testRoot));

    [Fact]
    public void StorageLegacyArchiveFailureRetries() =>
        RunOnStaThread(() => TestStorageLegacyArchiveFailureRetries(_testRoot));

    [Fact]
    public void StorageExplicitLocatorSkipsLegacyMigration() =>
        RunOnStaThread(() => TestStorageExplicitLocatorSkipsLegacyMigration(_testRoot));

    [Fact]
    public void StorageExplicitRelativePath() =>
        RunOnStaThread(() => TestStorageExplicitRelativePath(_testRoot));

    [Fact]
    public void StorageExplicitAbsolutePath() =>
        RunOnStaThread(() => TestStorageExplicitAbsolutePath(_testRoot));

    [Fact]
    public void StorageLocatorMissingVaultRejected() =>
        RunOnStaThread(() => TestStorageLocatorMissingVaultRejected(_testRoot));

    [Fact]
    public void StorageRecoveryArtifactRejected() =>
        RunOnStaThread(() => TestStorageRecoveryArtifactRejected(_testRoot));

    [Fact]
    public void StorageScopedUserLocator() =>
        RunOnStaThread(() => TestStorageScopedUserLocator(_testRoot));

    [Fact]
    public void DialogTopMostFollowsOwner() =>
        RunOnStaThread(TestDialogTopMostFollowsOwner);

    [Fact]
    public void ResizableWindowConfiguration() =>
        RunOnStaThread(() => TestResizableWindowConfiguration(_testRoot));

    [Fact]
    public void ResponsiveWindowDesignerCompatibility() =>
        Check(
            !typeof(ResponsiveWindow).IsAbstract,
            "ResponsiveWindow remains concrete for the inherited-form designer");

    [Fact]
    public void MainFormDesignerVisualTree() =>
        RunOnStaThread(TestMainFormDesignerVisualTree);

    [Fact]
    public void DialogDesignerVisualTrees() =>
        RunOnStaThread(TestDialogDesignerVisualTrees);

    [Fact]
    public void ApplicationBrandIcon() =>
        RunOnStaThread(() => TestApplicationBrandIcon(_testRoot));

    [Fact]
    public void WindowLayoutPreferences() =>
        RunOnStaThread(() => TestWindowLayoutPreferences(_testRoot));

    [Fact]
    public void NarrowWindowLayouts() =>
        RunOnStaThread(() => TestNarrowWindowLayouts(_testRoot));

    [Fact]
    public void LongLocalizedLabelsRemainResponsive() =>
        RunOnStaThread(() => TestLongLocalizedLabelsRemainResponsive(_testRoot));

    [Fact]
    public void EditorSelectWheelProtection() =>
        RunOnStaThread(TestEditorSelectWheelProtection);

    [Fact]
    public void SettingsSelectWheelProtection() =>
        RunOnStaThread(() => TestSettingsSelectWheelProtection(_testRoot));

    [Fact]
    public void AuthenticatorCardVisibilityToggle() =>
        RunOnStaThread(TestAuthenticatorCardVisibilityToggle);

    [Fact]
    public void MainFormBulkCodeVisibility() =>
        RunOnStaThread(() => TestMainFormBulkCodeVisibility(_testRoot));

    [Fact]
    public void AutoLockSetting() =>
        RunOnStaThread(() => TestAutoLockSetting(_testRoot));

    [Fact]
    public void ProviderSectionOrdering() =>
        RunOnStaThread(TestProviderSectionOrdering);

    [Fact]
    public void EditorAuthenticatorVerification() =>
        RunOnStaThread(TestEditorAuthenticatorVerification);

    [Fact]
    public void ResponsiveWrapPanelLayout() =>
        RunOnStaThread(TestResponsiveWrapPanelLayout);

    [Fact]
    public void OtpGenerationValidation() =>
        RunOnStaThread(TestOtpGenerationValidation);

    [Fact]
    public void DefaultTheme() => RunOnStaThread(TestDefaultTheme);

    [Fact]
    public void DeleteEntryMarshalsToUiThread() =>
        RunOnStaThread(() => TestDeleteEntryMarshalsToUiThread(_testRoot));

    [Fact]
    public void WinAuthImportPreservesLocalAlwaysOnTop() =>
        RunOnStaThread(() => TestWinAuthImportPreservesLocalAlwaysOnTop(_testRoot));

    [Fact]
    public void AlwaysOnTopControlsStaySynchronized() =>
        RunOnStaThread(() => TestAlwaysOnTopControlsStaySynchronized(_testRoot));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HeaderActionButtonLayout(bool enlargedHeaderFont) =>
        RunOnStaThread(() => TestHeaderActionButtonLayout(_testRoot, enlargedHeaderFont));

    [Fact]
    public void EditorPaletteContrast() =>
        RunOnStaThread(TestEditorPaletteContrast);

    private static void RunOnStaThread(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                test();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static void TestLocalizationFallbacks()
    {
        L.SetLanguage("zh-CN");
        Equal("zh-Hans", L.CurrentLanguage, "Simplified Chinese region maps to script pack");
        Equal("设置", L.Get("Settings.Title"), "Simplified Chinese translation is loaded");

        L.SetLanguage("zh-TW");
        Equal("zh-Hant", L.CurrentLanguage, "Traditional Chinese region maps to script pack");

        L.SetLanguage("en-US");
        Equal("en", L.CurrentLanguage, "English region falls back to English parent");
        Equal("Settings", L.Get("Settings.Title"), "English baseline is available");

        L.SetLanguage("fr-CA");
        Equal("fr", L.CurrentLanguage, "Regional language falls back to parent pack");
        Equal("Paramètres", L.Get("Settings.Title"), "Parent-language translation is used");

        L.SetLanguage("pt-PT");
        Equal(
            "en",
            L.CurrentLanguage,
            "European Portuguese does not incorrectly use Brazilian Portuguese");

        L.SetLanguage("it-IT");
        Equal("en", L.CurrentLanguage, "Unsupported language falls back to English");
        Equal(
            "Missing.Test.Key",
            L.Get("Missing.Test.Key"),
            "Unknown translation keys remain diagnosable");

        var languageDirectory = Path.Combine(AppContext.BaseDirectory, "Languages");
        Directory.CreateDirectory(languageDirectory);
        var externalEnglishPath = Path.Combine(languageDirectory, "en.json");
        var hiddenEnglishPath = Path.Combine(
            languageDirectory,
            $".en-{Guid.NewGuid():N}.json.bak");
        File.Move(externalEnglishPath, hiddenEnglishPath);
        try
        {
            L.Initialize("en");
            Equal(
                "Settings",
                L.Get("Settings.Title"),
                "Embedded English remains available without external files");
        }
        finally
        {
            File.Move(hiddenEnglishPath, externalEnglishPath);
        }

        var invalidPackPath = Path.Combine(languageDirectory, "it-IT.json");
        byte[]? originalPack = File.Exists(invalidPackPath)
            ? File.ReadAllBytes(invalidPackPath)
            : null;
        try
        {
            File.WriteAllBytes(invalidPackPath, new byte[(512 * 1024) + 1]);
            L.Initialize("it-IT");
            Equal("en", L.CurrentLanguage, "Oversized language pack is ignored");

            File.WriteAllText(invalidPackPath, "{ invalid json", new UTF8Encoding(false));
            L.Initialize("it-IT");
            Equal("en", L.CurrentLanguage, "Malformed language pack is ignored");

            File.WriteAllText(
                invalidPackPath,
                """
                {
                  "schemaVersion": 1,
                  "locale": "it-IT",
                  "name": "Italian",
                  "nativeName": "Italiano",
                  "strings": {
                    "Dashboard.ResultCount": "Totale: {1}",
                    "Dashboard.AccountSummary": "Account: {0,999}"
                  }
                }
                """,
                new UTF8Encoding(false));
            L.Initialize("it-IT");
            Equal("it-IT", L.CurrentLanguage, "Valid external language metadata is discovered");
            Equal(
                "Authenticators: 3",
                L.Format("Dashboard.ResultCount", 3),
                "Translation with mismatched placeholders falls back to English");
            Equal(
                "Accounts: 3 · Encrypted locally · Generated offline",
                L.Format("Dashboard.AccountSummary", 3),
                "Translation cannot add a potentially expensive format width");
        }
        finally
        {
            if (originalPack is null)
            {
                File.Delete(invalidPackPath);
            }
            else
            {
                File.WriteAllBytes(invalidPackPath, originalPack);
            }

            L.Initialize("zh-Hans");
        }
    }

    private static void TestBuiltInLanguagePacksAreComplete()
    {
        var expectedLocales = new[]
        {
            "en", "zh-Hans", "zh-Hant", "ja", "ko",
            "de", "fr", "es", "pt-BR", "ru"
        };
        var languageDirectory = Path.Combine(AppContext.BaseDirectory, "Languages");
        var baseline = ReadLanguageStrings(Path.Combine(languageDirectory, "en.json"));
        Check(baseline.Count >= 400, "English language baseline covers the application");
        L.Initialize("en");
        var availableLocales = L.AvailableLanguages
            .Select(language => language.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Check(
            expectedLocales.All(availableLocales.Contains),
            "Every built-in language is available to the settings selector");

        foreach (var locale in expectedLocales)
        {
            var filePath = Path.Combine(languageDirectory, locale + ".json");
            Check(File.Exists(filePath), $"{locale} language pack is published");
            var packJson = File.ReadAllText(filePath, Encoding.UTF8);
            Check(
                !packJson.Contains('\uFFFD')
                && !Regex.IsMatch(packJson, @"\?{2,}", RegexOptions.CultureInvariant),
                $"{locale} language pack contains no encoding replacement artifacts");
            using (var document = JsonDocument.Parse(
                       packJson))
            {
                Equal(
                    1,
                    document.RootElement.GetProperty("schemaVersion").GetInt32(),
                    $"{locale} uses the supported language-pack schema");
                Equal(
                    locale,
                    document.RootElement.GetProperty("locale").GetString(),
                    $"{locale} metadata matches its filename");
            }

            var strings = ReadLanguageStrings(filePath);
            Equal(
                baseline.Count,
                strings.Count,
                $"{locale} language pack has the complete key count");

            foreach (var pair in baseline)
            {
                Check(
                    strings.TryGetValue(pair.Key, out var translated),
                    $"{locale} contains key {pair.Key}");
                Check(
                    !string.IsNullOrWhiteSpace(translated),
                    $"{locale} provides a value for {pair.Key}");
                Equal(
                    PlaceholderSignature(pair.Value),
                    PlaceholderSignature(translated!),
                    $"{locale} preserves placeholders for {pair.Key}");
            }
        }
    }

    private static Dictionary<string, string> ReadLanguageStrings(string filePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(filePath, Encoding.UTF8));
        return document.RootElement
            .GetProperty("strings")
            .EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => property.Value.GetString() ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static string PlaceholderSignature(string value)
    {
        _ = CompositeFormat.Parse(value);
        return string.Join(
            ",",
            Regex.Matches(
                    value,
                    @"(?<!\{)\{(?<index>\d+)(?:[^{}]*)\}(?!\})",
                    RegexOptions.CultureInvariant)
                .Select(match => match.Value)
                .OrderBy(formatItem => formatItem, StringComparer.Ordinal));
    }

    private static void TestBase32()
    {
        var bytes = Encoding.ASCII.GetBytes("12345678901234567890");
        var encoded = Base32Encoding.Encode(bytes);
        Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", encoded, "Base32 RFC secret");
        Equal(
            Convert.ToHexString(bytes),
            Convert.ToHexString(Base32Encoding.Decode(encoded.ToLowerInvariant())),
            "Base32 case-insensitive decode");
    }

    private static void TestRfcOtpVectors()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var hotp = new[]
        {
            "755224", "287082", "359152", "969429", "338314",
            "254676", "287922", "162583", "399871", "520489"
        };
        for (var counter = 0; counter < hotp.Length; counter++)
        {
            Equal(
                hotp[counter],
                OtpService.GenerateHotp(secret, counter),
                $"RFC 4226 counter {counter}");
        }

        Equal(
            "94287082",
            OtpService.GenerateTotp(secret, 59, digits: 8, algorithm: OtpAlgorithm.Sha1),
            "RFC 6238 SHA-1");
        Equal(
            "46119246",
            OtpService.GenerateTotp(
                Encoding.ASCII.GetBytes("12345678901234567890123456789012"),
                59,
                digits: 8,
                algorithm: OtpAlgorithm.Sha256),
            "RFC 6238 SHA-256");
        Equal(
            "90693936",
            OtpService.GenerateTotp(
                Encoding.ASCII.GetBytes(
                    "1234567890123456789012345678901234567890123456789012345678901234"),
                59,
                digits: 8,
                algorithm: OtpAlgorithm.Sha512),
            "RFC 6238 SHA-512");
    }

    private static void TestOtpUriRoundTrip()
    {
        var original = SampleEntry();
        original.TimeOffsetSeconds = -42;
        var uri = OtpAuthUriService.Build(original, includeProviderData: true);
        var parsed = OtpAuthUriService.ParseSingle(uri);

        Equal(original.Name, parsed.Name, "URI account name");
        Equal(original.Issuer, parsed.Issuer, "URI issuer");
        Equal(original.Secret, parsed.Secret, "URI secret");
        Equal(original.Algorithm, parsed.Algorithm, "URI algorithm");
        Equal(original.Digits, parsed.Digits, "URI digits");
        Equal(original.Period, parsed.Period, "URI period");
        Equal(original.TimeOffsetSeconds, parsed.TimeOffsetSeconds, "URI time offset");
        Equal(
            OtpService.GetCode(original, DateTimeOffset.FromUnixTimeSeconds(59)),
            OtpService.GetCode(parsed, DateTimeOffset.FromUnixTimeSeconds(59)),
            "URI time offset preserves generated codes");

        var multiple = OtpAuthUriService.Parse(
            uri + Environment.NewLine +
            OtpAuthUriService.Build(new AuthenticatorEntry
            {
                Name = "second",
                Issuer = "Microsoft",
                Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
                Kind = AuthenticatorKind.Microsoft
            }));
        Equal(2, multiple.Count, "Multi-line URI import");
        Equal(AuthenticatorKind.Microsoft, multiple[1].Kind, "Provider detection");

        var unnamed = OtpAuthUriService.ParseSingle(
            "otpauth://totp/?secret=GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ");
        Equal(string.Empty, unnamed.Name, "URI import does not persist a localized default name");
        Equal("TOTP", unnamed.DisplayName, "Unnamed URI uses a stable display fallback");
        Check(
            OtpAuthUriService.Build(unnamed).StartsWith(
                "otpauth://totp/",
                StringComparison.Ordinal),
            "Unnamed URI remains exportable");
    }

    private static void TestQrRoundTrip()
    {
        var uri = OtpAuthUriService.Build(SampleEntry());
        using var bitmap = QrCodeService.Create(uri);
        Equal(uri, QrCodeService.Decode(bitmap), "QR encode/decode");
    }

    private static void TestGoogleMigrationUri()
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        var entryPayload = new List<byte>();
        WriteProtoBytes(entryPayload, 1, secret);
        WriteProtoBytes(entryPayload, 2, Encoding.UTF8.GetBytes("migrated@example.com"));
        WriteProtoBytes(entryPayload, 3, Encoding.UTF8.GetBytes("Microsoft"));
        WriteProtoVarint(entryPayload, 4, 2); // SHA-256
        WriteProtoVarint(entryPayload, 5, 2); // 8 digits
        WriteProtoVarint(entryPayload, 6, 2); // TOTP

        var migrationPayload = new List<byte>();
        WriteProtoBytes(migrationPayload, 1, [.. entryPayload]);
        var uri = "otpauth-migration://offline?data=" +
                  Uri.EscapeDataString(Convert.ToBase64String([.. migrationPayload]));
        var entries = OtpAuthUriService.Parse(uri);

        Equal(1, entries.Count, "Google migration entry count");
        Equal("migrated@example.com", entries[0].Name, "Google migration account name");
        Equal(AuthenticatorKind.Microsoft, entries[0].Kind, "Google migration provider");
        Equal(OtpAlgorithm.Sha256, entries[0].Algorithm, "Google migration algorithm");
        Equal(8, entries[0].Digits, "Google migration digits");

        var overflowEntry = new List<byte>();
        WriteProtoBytes(overflowEntry, 1, secret);
        WriteProtoBytes(overflowEntry, 2, Encoding.UTF8.GetBytes("overflow@example.com"));
        WriteProtoVarint(overflowEntry, 6, 1); // HOTP
        WriteProtoVarint(overflowEntry, 7, ulong.MaxValue);
        var overflowPayload = new List<byte>();
        WriteProtoBytes(overflowPayload, 1, [.. overflowEntry]);
        var overflowUri = "otpauth-migration://offline?data=" +
                          Uri.EscapeDataString(Convert.ToBase64String([.. overflowPayload]));
        var overflowRejected = false;
        try
        {
            OtpAuthUriService.Parse(overflowUri);
        }
        catch (FormatException)
        {
            overflowRejected = true;
        }

        Check(overflowRejected, "Google migration rejects overflowing counters as invalid data");

        var shortSecretEntry = new List<byte>();
        WriteProtoBytes(shortSecretEntry, 1, Encoding.ASCII.GetBytes("123456789"));
        WriteProtoBytes(shortSecretEntry, 2, Encoding.UTF8.GetBytes("short@example.com"));
        var shortSecretPayload = new List<byte>();
        WriteProtoBytes(shortSecretPayload, 1, [.. shortSecretEntry]);
        var shortSecretUri = "otpauth-migration://offline?data=" +
                             Uri.EscapeDataString(Convert.ToBase64String([.. shortSecretPayload]));
        var shortSecretRejected = false;
        try
        {
            OtpAuthUriService.Parse(shortSecretUri);
        }
        catch (FormatException)
        {
            shortSecretRejected = true;
        }

        Check(shortSecretRejected, "Google migration rejects short secrets before editor import");
    }

    private static void TestWinAuthRoundTrip(string testRoot)
    {
        var entries = new List<AuthenticatorEntry>
        {
            SampleEntry(),
            new()
            {
                Name = "Counter token",
                Issuer = "Legacy",
                Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
                Kind = AuthenticatorKind.Hotp,
                Counter = 42,
                Digits = 6,
                SortOrder = 10,
                Hotkey = "Ctrl+Alt+H",
                HotkeyAction = EntryHotkeyAction.Type
            },
            new()
            {
                Name = "Steam account",
                Issuer = "Steam",
                Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
                Kind = AuthenticatorKind.Steam,
                Digits = 5,
                DeviceId = "android:test-device",
                Serial = "US-1234-5678-9012",
                SortOrder = 20,
                ProviderData = new Dictionary<string, string>
                {
                    ["steamData"] = "{\"account_name\":\"demo\"}"
                }
            }
        };
        var settings = new AppSettings
        {
            AlwaysOnTop = true,
            MinimizeToTray = true,
            StartWithWindows = false
        };

        var plainFile = Path.Combine(testRoot, "winauth-plain.xml");
        WinAuthConfigService.Export(plainFile, entries, settings);
        var plain = WinAuthConfigService.Import(plainFile);
        Check(
            plain.Settings.AlwaysOnTop,
            "WinAuth parser preserves source always-on-top metadata");
        Equal(entries.Count, plain.Entries.Count, "WinAuth plain entry count");
        Equal(entries[1].Counter, plain.Entries[1].Counter, "WinAuth HOTP counter");
        Equal(entries[2].DeviceId, plain.Entries[2].DeviceId, "WinAuth Steam device id");
        Equal(entries[2].ProviderData["steamData"],
            plain.Entries[2].ProviderData["steamData"],
            "WinAuth Steam metadata");

        var encryptedFile = Path.Combine(testRoot, "winauth-encrypted.xml");
        WinAuthConfigService.Export(encryptedFile, entries, settings, "迁移密码-Strong-123");
        Check(
            WinAuthConfigService.Inspect(encryptedFile).RequiresPassword,
            "WinAuth password protection detected");
        var encrypted = WinAuthConfigService.Import(encryptedFile, "迁移密码-Strong-123");
        Equal(entries.Count, encrypted.Entries.Count, "WinAuth encrypted entry count");
        Equal(entries[0].Secret, encrypted.Entries[0].Secret, "WinAuth encrypted secret");

        var wrongPasswordFailed = false;
        try
        {
            _ = WinAuthConfigService.Import(encryptedFile, "wrong");
        }
        catch
        {
            wrongPasswordFailed = true;
        }

        Check(wrongPasswordFailed, "WinAuth wrong password rejected");
    }

    private static void TestLegacyWinAuthCipherFixture()
    {
        // Generated independently with the exact BouncyCastle.Crypto 1.7.0 DLL
        // referenced by WinAuth 3.5 and fixed salts. This detects accidental
        // incompatibility with WinAuth's non-standard 256-byte Blowfish key.
        const string encrypted =
            "57494E41555448331011121314151617133D5D61D5627EB25D9E56F97A0308DF6C" +
            "1012470B860D7724D10F75794A99E00001020304050607FD1EF2D60054FAB57095" +
            "FFC7142C115E2416989448A2B95E07D0E6EDDA9E3AB8";
        const string expected =
            "3C636F6E6669673E6C65676163792D666978747572653C2F636F6E6669673E";

        Equal(
            expected,
            WinAuthCryptoService.DecryptSequence(
                encrypted,
                WinAuthProtection.Password,
                "WinAuth-Compat-Password"),
            "WinAuth 3.5 legacy Blowfish fixture");
    }

    private static void TestWinAuthWindowsProtection()
    {
        const string plaintext = "00112233445566778899AABBCCDDEEFF";
        var userProtected = WinAuthCryptoService.EncryptSequence(
            plaintext,
            WinAuthProtection.WindowsUser,
            password: null);
        Equal(
            plaintext,
            WinAuthCryptoService.DecryptSequence(
                userProtected,
                WinAuthProtection.WindowsUser,
                password: null),
            "WinAuth Windows user protection");

        var combined = WinAuthCryptoService.EncryptSequence(
            plaintext,
            WinAuthProtection.Password | WinAuthProtection.WindowsUser,
            "Combined-Password-123");
        Equal(
            plaintext,
            WinAuthCryptoService.DecryptSequence(
                combined,
                WinAuthProtection.Password | WinAuthProtection.WindowsUser,
                "Combined-Password-123"),
            "WinAuth combined password and Windows protection");

        var yubiRejected = false;
        try
        {
            _ = WinAuthCryptoService.DecryptSequence(
                plaintext,
                WinAuthProtection.YubiKeySlot1,
                password: null);
        }
        catch (WinAuthCompatibilityException)
        {
            yubiRejected = true;
        }

        Check(yubiRejected, "WinAuth YubiKey protection rejected with guidance");
    }

    private static void TestBackupRoundTrip(string testRoot)
    {
        var file = Path.Combine(testRoot, "backup.authdesk");
        BackupService.Export(file, [SampleEntry()], "Backup-Password-123");
        var entries = BackupService.Import(file, "Backup-Password-123");
        Equal(1, entries.Count, "Encrypted backup entry count");
        Equal(SampleEntry().Secret, entries[0].Secret, "Encrypted backup secret");

        var rejected = false;
        try
        {
            _ = BackupService.Import(file, "wrong");
        }
        catch (VaultAuthenticationException)
        {
            rejected = true;
        }

        Check(rejected, "Encrypted backup wrong password rejected");
    }

    private static void TestWinAuthPerEntryPassword(string testRoot)
    {
        var file = Path.Combine(testRoot, "winauth-entry-password.xml");
        WinAuthConfigService.Export(
            file,
            [SampleEntry()],
            new AppSettings());

        var document = XDocument.Load(file);
        var data = document
            .Descendants()
            .First(element => element.Name.LocalName == "authenticatordata");
        var plaintext = Encoding.UTF8.GetBytes(
            data.ToString(SaveOptions.DisableFormatting));
        var encrypted = WinAuthCryptoService.EncryptSequence(
            Convert.ToHexString(plaintext),
            WinAuthProtection.Password,
            "Entry-Password-123");
        data.RemoveNodes();
        data.SetAttributeValue("encrypted", "y");
        data.Value = encrypted;
        document.Save(file);

        var info = WinAuthConfigService.Inspect(file);
        Check(info.RequiresPassword, "Per-entry WinAuth password detected");
        Check(info.HasPerAuthenticatorPasswords, "Per-entry password classification");
        Check(!info.RequiresConfigurationPassword, "No configuration password classification");

        var prompts = 0;
        var result = WinAuthConfigService.Import(
            file,
            password: null,
            name =>
            {
                prompts++;
                Equal("alice@example.com", name, "Per-entry password prompt name");
                return "Entry-Password-123";
            });
        Equal(1, prompts, "Per-entry password prompt count");
        Equal(1, result.Entries.Count, "Per-entry protected WinAuth count");
        Equal(SampleEntry().Secret, result.Entries[0].Secret, "Per-entry protected secret");
    }

    private static void TestPortableVaultDefaultAndFolderCopy(string testRoot)
    {
        var sourceDirectory = Path.Combine(testRoot, "portable-source");
        var copiedDirectory = Path.Combine(testRoot, "portable-copy");
        Directory.CreateDirectory(sourceDirectory);
        var sourceFile = Path.Combine(sourceDirectory, StorageLocationService.VaultFileName);
        var sourceProtector = new FakeWindowsAccountProtector("source-account");

        using (var vault = new VaultService(sourceFile, sourceProtector))
        {
            Equal(
                VaultProtectionMode.Portable,
                vault.InspectProtectionMode(),
                "New vault inspection defaults to portable protection");
            var payload = vault.Open();
            Equal(
                VaultProtectionMode.Portable,
                vault.ProtectionMode,
                "New vault opens with portable protection");
            payload.Entries.Add(SampleEntry());
            vault.Save(payload);
            Equal(
                VaultProtectionMode.Portable,
                vault.InspectProtectionMode(),
                "Saved new vault remains portable");
        }

        Equal(0, sourceProtector.ProtectCalls, "Portable save does not invoke Windows protection");
        Equal(0, sourceProtector.UnprotectCalls, "Portable save does not invoke Windows unprotect");

        Directory.CreateDirectory(copiedDirectory);
        foreach (var file in Directory.EnumerateFiles(sourceDirectory))
        {
            File.Copy(file, Path.Combine(copiedDirectory, Path.GetFileName(file)));
        }

        var copiedProtector = new FakeWindowsAccountProtector("different-computer");
        using var copiedVault = new VaultService(
            Path.Combine(copiedDirectory, StorageLocationService.VaultFileName),
            copiedProtector);
        var copiedPayload = copiedVault.Open();
        Equal(
            VaultProtectionMode.Portable,
            copiedVault.ProtectionMode,
            "Copied vault keeps portable protection");
        Equal(1, copiedPayload.Entries.Count, "Copied portable vault entry count");
        Equal(
            SampleEntry().Secret,
            copiedPayload.Entries[0].Secret,
            "Copied portable vault secret");
        Equal(
            0,
            copiedProtector.UnprotectCalls,
            "Copied portable vault is independent of Windows identity");
    }

    private static void TestVaultWindowsProtectionTransitions(string testRoot)
    {
        var file = Path.Combine(testRoot, "windows-transition-vault.json");
        var accountA = new FakeWindowsAccountProtector("account-a");
        var accountB = new FakeWindowsAccountProtector("account-b");

        using (var portableVault = new VaultService(file, accountA))
        {
            var payload = portableVault.Open();
            payload.Entries.Add(SampleEntry());
            portableVault.Save(payload);
            portableVault.EnableWindowsAccountProtection(payload);

            Equal(
                VaultProtectionMode.WindowsAccount,
                portableVault.ProtectionMode,
                "Portable vault switches to Windows protection");
            Equal(
                VaultProtectionMode.WindowsAccount,
                portableVault.InspectProtectionMode(),
                "Windows protection persists in the vault envelope");
            Equal(1, accountA.ProtectCalls, "Enabling Windows protection invokes protector");

            using var protectedBackup = new VaultService(file + ".bak", accountA);
            Equal(
                VaultProtectionMode.WindowsAccount,
                protectedBackup.InspectProtectionMode(),
                "Protection change rewrites backup with Windows protection");
            Equal(
                1,
                protectedBackup.Open().Entries.Count,
                "Windows-protected backup remains readable by the protected identity");
        }

        using (var otherAccountVault = new VaultService(file, accountB))
        {
            var rejected = false;
            try
            {
                _ = otherAccountVault.Open();
            }
            catch (VaultAuthenticationException)
            {
                rejected = true;
            }

            Check(rejected, "Different Windows identity cannot open protected vault");
            Equal(
                1,
                accountB.UnprotectCalls,
                "Different identity attempts Windows unprotect");
        }

        using (var accountAVault = new VaultService(file, accountA))
        {
            var payload = accountAVault.Open();
            Equal(1, payload.Entries.Count, "Windows transition preserves entries");
            accountAVault.DisableWindowsAccountProtection(payload);
            Equal(
                VaultProtectionMode.Portable,
                accountAVault.ProtectionMode,
                "Windows protection can be disabled");
            Equal(
                VaultProtectionMode.Portable,
                accountAVault.InspectProtectionMode(),
                "Disabled Windows protection persists as portable");
        }

        using var reopenedByOtherAccount = new VaultService(file, accountB);
        var portablePayload = reopenedByOtherAccount.Open();
        Equal(
            VaultProtectionMode.Portable,
            reopenedByOtherAccount.ProtectionMode,
            "Other identity opens vault after Windows protection is disabled");
        Equal(1, portablePayload.Entries.Count, "Windows-to-portable transition preserves entries");
        Equal(
            SampleEntry().Secret,
            portablePayload.Entries[0].Secret,
            "Windows-to-portable transition preserves secret");

        using var portableBackup = new VaultService(file + ".bak", accountB);
        Equal(
            VaultProtectionMode.Portable,
            portableBackup.InspectProtectionMode(),
            "Disabling Windows protection rewrites backup as portable");
        Equal(
            1,
            portableBackup.Open().Entries.Count,
            "Portable backup opens independently of Windows identity");
    }

    private static void TestInitialVaultSaveDoesNotOverwriteRace(string testRoot)
    {
        var file = Path.Combine(testRoot, "initial-save-race-vault.json");
        using var vault = new VaultService(file);
        var payload = vault.Open();
        const string competingContent = "competing vault content";
        File.WriteAllText(file, competingContent, new UTF8Encoding(false));

        var rejected = false;
        try
        {
            vault.Save(payload);
        }
        catch (IOException)
        {
            rejected = true;
        }

        Check(rejected, "Initial vault save refuses to overwrite a concurrently created file");
        Equal(
            competingContent,
            File.ReadAllText(file, Encoding.UTF8),
            "Concurrent vault file remains untouched");
    }

    private static void TestVaultMissingAfterLockIsRejected(string testRoot)
    {
        var file = Path.Combine(testRoot, "missing-after-lock-vault.json");
        using var vault = new VaultService(file);
        var payload = vault.Open();
        payload.Entries.Add(SampleEntry());
        vault.Save(payload);
        vault.Lock();

        var movedFile = file + ".detached";
        File.Move(file, movedFile);

        var rejected = false;
        try
        {
            _ = vault.Open();
        }
        catch (VaultAuthenticationException)
        {
            rejected = true;
        }

        Check(rejected, "Missing vault after lock is rejected instead of becoming an empty vault");
        Check(!File.Exists(file), "Missing vault is not silently recreated during unlock");
    }

    private static void TestPasswordRemovalUsesPortableProtection(string testRoot)
    {
        var file = Path.Combine(testRoot, "password-removal-vault.json");
        var originalProtector = new FakeWindowsAccountProtector("password-owner");

        using (var vault = new VaultService(file, originalProtector))
        {
            var payload = vault.Open();
            payload.Entries.Add(SampleEntry());
            vault.Save(payload);
            vault.ChangePassword(payload, "Vault-Password-123");
            Equal(
                VaultProtectionMode.Password,
                vault.ProtectionMode,
                "Portable vault switches to password protection");
        }

        using (var lockedVault = new VaultService(file, originalProtector))
        {
            var rejected = false;
            try
            {
                _ = lockedVault.Open("wrong");
            }
            catch (VaultAuthenticationException)
            {
                rejected = true;
            }

            Check(rejected, "Vault wrong password rejected");
            var payload = lockedVault.Open("Vault-Password-123");
            Equal(1, payload.Entries.Count, "Password-protected vault entry count");
            lockedVault.RemovePassword(payload);
            Equal(
                VaultProtectionMode.Portable,
                lockedVault.ProtectionMode,
                "Removing password returns to portable protection");
        }

        var differentAccount = new FakeWindowsAccountProtector("different-password-account");
        using var portableVault = new VaultService(file, differentAccount);
        var portablePayload = portableVault.Open();
        Equal(
            VaultProtectionMode.Portable,
            portableVault.ProtectionMode,
            "Password removal persists portable protection");
        Equal(1, portablePayload.Entries.Count, "Password removal preserves entries");
        Equal(
            0,
            differentAccount.UnprotectCalls,
            "Password removal does not silently enable Windows protection");
    }

    private static void TestStorageProgramDirectoryPriority(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-program-priority");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);

        CreatePortableVault(programDirectory, "Program vault entry");
        CreatePortableVault(legacyDirectory, "Legacy vault entry");
        var programVault = Path.Combine(
            programDirectory,
            StorageLocationService.VaultFileName);
        var legacyVault = Path.Combine(
            legacyDirectory,
            StorageLocationService.VaultFileName);
        var programContents = File.ReadAllBytes(programVault);
        var legacyContents = File.ReadAllBytes(legacyVault);

        var service = new StorageLocationService(programDirectory, legacyDirectory);
        var location = service.Resolve();

        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            location.DataDirectory,
            "Program vault takes priority over legacy vault");
        Equal(
            StorageLocationSource.ProgramDirectory,
            location.Source,
            "Program vault reports program-directory source");
        Check(location.ConfigurationFile is null, "Program directory needs no locator file");
        Equal(
            Convert.ToBase64String(programContents),
            Convert.ToBase64String(File.ReadAllBytes(programVault)),
            "Existing program vault is never overwritten by the legacy vault");
        Equal(
            Convert.ToBase64String(legacyContents),
            Convert.ToBase64String(File.ReadAllBytes(legacyVault)),
            "Choosing the program vault does not modify the legacy vault");
        using var resolvedVault = new VaultService(programVault);
        Equal(
            "Program vault entry",
            resolvedVault.Open().Entries.Single().Name,
            "Program vault remains readable after path resolution");
    }

    private static void TestStorageNewInstallDefaultsToProgramDirectory(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-new-install-default");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);

        var service = new StorageLocationService(programDirectory, legacyDirectory);
        var location = service.Resolve();

        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            location.DataDirectory,
            "New installations default to the program directory");
        Equal(
            StorageLocationSource.ProgramDirectory,
            location.Source,
            "New installation reports program-directory source");
        Check(
            !File.Exists(Path.Combine(programDirectory, StorageLocationService.VaultFileName)),
            "Resolving a new installation does not create its vault prematurely");
    }

    private static void TestStorageCompletedLegacyMigrationAllowsNewProgramDirectory(
        string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-completed-legacy-migration");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        var legacyBackup = Path.Combine(
            legacyDirectory,
            StorageLocationService.VaultFileName + ".bak");
        var migratedArchive = Path.Combine(
            legacyDirectory,
            StorageLocationService.VaultFileName + ".migrated-complete.bak");
        File.WriteAllText(legacyBackup, "old-backup", new UTF8Encoding(false));
        File.WriteAllText(migratedArchive, "migrated-backup", new UTF8Encoding(false));

        var location = new StorageLocationService(
            programDirectory,
            legacyDirectory).Resolve();

        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            location.DataDirectory,
            "Completed legacy migration lets another empty copy use its program directory");
        Equal(
            StorageLocationSource.ProgramDirectory,
            location.Source,
            "Completed legacy migration resolves as a program-directory default");
        Check(
            !File.Exists(Path.Combine(programDirectory, StorageLocationService.VaultFileName)),
            "Path resolution leaves initial vault creation to normal startup");
        Equal(
            "old-backup",
            File.ReadAllText(legacyBackup, Encoding.UTF8),
            "Completed migration does not modify the ordinary legacy backup");
        Equal(
            "migrated-backup",
            File.ReadAllText(migratedArchive, Encoding.UTF8),
            "Completed migration does not modify the named recovery archive");
    }

    private static void TestStorageLegacyMainOverridesCompletedMigrationArchive(
        string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-legacy-main-after-migration");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        CreatePortableVault(legacyDirectory, "Current legacy main entry");
        var legacyVault = Path.Combine(
            legacyDirectory,
            StorageLocationService.VaultFileName);
        var programVault = Path.Combine(
            programDirectory,
            StorageLocationService.VaultFileName);
        var legacyContents = File.ReadAllBytes(legacyVault);
        File.WriteAllText(
            Path.Combine(
                legacyDirectory,
                StorageLocationService.VaultFileName + ".migrated-older.bak"),
            "older-migration",
            new UTF8Encoding(false));

        var location = new StorageLocationService(
            programDirectory,
            legacyDirectory).Resolve();

        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            location.DataDirectory,
            "An active legacy main vault takes priority over an older migration archive");
        Equal(
            Convert.ToBase64String(legacyContents),
            Convert.ToBase64String(File.ReadAllBytes(programVault)),
            "The active legacy main vault is copied without modification");
        using var migratedVault = new VaultService(programVault);
        Equal(
            "Current legacy main entry",
            migratedVault.Open().Entries.Single().Name,
            "The active legacy main vault remains readable after migration");
        Check(
            !File.Exists(legacyVault),
            "The migrated active legacy main name is archived after copying");
    }

    private static void TestStorageMatchingLegacyCopyIsArchived(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-matching-legacy-cleanup");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        CreatePortableVault(legacyDirectory, "Interrupted migration entry");
        var legacyVault = Path.Combine(
            legacyDirectory,
            StorageLocationService.VaultFileName);
        var programVault = Path.Combine(
            programDirectory,
            StorageLocationService.VaultFileName);
        var originalContents = File.ReadAllBytes(legacyVault);
        File.Copy(legacyVault, programVault, overwrite: false);

        var location = new StorageLocationService(
            programDirectory,
            legacyDirectory).Resolve();

        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            location.DataDirectory,
            "An interrupted migration still resolves to its committed program vault");
        Check(
            !File.Exists(legacyVault),
            "A matching legacy source is archived on the next start");
        var archivedLegacyVault = Directory.GetFiles(
            legacyDirectory,
            StorageLocationService.VaultFileName + ".migrated-*.bak").Single();
        Equal(
            Convert.ToBase64String(originalContents),
            Convert.ToBase64String(File.ReadAllBytes(archivedLegacyVault)),
            "Interrupted migration cleanup preserves the legacy recovery copy");
        Equal(
            Convert.ToBase64String(originalContents),
            Convert.ToBase64String(File.ReadAllBytes(programVault)),
            "Interrupted migration cleanup does not modify the program vault");
    }

    private static void TestStorageLegacyVaultMigratesToProgramDirectory(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-legacy-migration");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        CreatePortableVault(legacyDirectory, "Migrated legacy entry");
        var legacyVault = Path.Combine(
            legacyDirectory,
            StorageLocationService.VaultFileName);
        var programVault = Path.Combine(
            programDirectory,
            StorageLocationService.VaultFileName);
        var legacyContents = File.ReadAllBytes(legacyVault);

        var service = new StorageLocationService(programDirectory, legacyDirectory);
        var location = service.Resolve();

        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            location.DataDirectory,
            "Existing legacy vault migrates to the program directory");
        Equal(
            StorageLocationSource.ProgramDirectory,
            location.Source,
            "Migrated legacy vault reports program-directory source");
        Equal(
            Convert.ToBase64String(legacyContents),
            Convert.ToBase64String(File.ReadAllBytes(programVault)),
            "Legacy migration preserves the exact vault contents");
        Check(
            !File.Exists(legacyVault),
            "Legacy migration removes the active legacy vault name");
        var archivedLegacyVault = Directory.GetFiles(
            legacyDirectory,
            StorageLocationService.VaultFileName + ".migrated-*.bak").Single();
        Equal(
            Convert.ToBase64String(legacyContents),
            Convert.ToBase64String(File.ReadAllBytes(archivedLegacyVault)),
            "Legacy migration retains the source vault as a named recovery copy");
        Check(location.ConfigurationFile is null, "Default program directory needs no locator file");
        using (var migratedVault = new VaultService(programVault))
        {
            Equal(
                "Migrated legacy entry",
                migratedVault.Open().Entries.Single().Name,
                "Migrated program vault remains readable");
        }

        var migratedContents = File.ReadAllBytes(programVault);
        var reopened = new StorageLocationService(programDirectory, legacyDirectory).Resolve();
        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            reopened.DataDirectory,
            "Subsequent starts continue using the migrated program vault");
        Equal(
            Convert.ToBase64String(migratedContents),
            Convert.ToBase64String(File.ReadAllBytes(programVault)),
            "Subsequent starts do not overwrite the migrated program vault");

        File.Move(programVault, programVault + ".lost", overwrite: false);
        var afterProgramVaultLoss = new StorageLocationService(
            programDirectory,
            legacyDirectory).Resolve();
        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            afterProgramVaultLoss.DataDirectory,
            "A migrated legacy archive does not show a path-selection prompt");
        Check(
            !File.Exists(programVault),
            "Archived legacy data is not copied again before normal startup creates a new vault");
    }

    private static void TestStorageExplicitLocatorSkipsLegacyMigration(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-explicit-before-legacy");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        var customDirectory = Path.Combine(caseRoot, "custom");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        Directory.CreateDirectory(customDirectory);
        TouchVault(legacyDirectory);
        TouchVault(customDirectory);

        var service = new StorageLocationService(programDirectory, legacyDirectory);
        WriteStorageLocator(service.ProgramLocatorFile, customDirectory);
        var location = service.Resolve();

        Equal(
            StorageLocationService.NormalizeDirectory(customDirectory),
            location.DataDirectory,
            "Explicit custom directory remains higher priority than legacy migration");
        Equal(
            StorageLocationSource.ExplicitConfiguration,
            location.Source,
            "Explicit custom directory retains its source");
        Check(
            !File.Exists(Path.Combine(programDirectory, StorageLocationService.VaultFileName)),
            "Resolving an explicit custom directory does not copy the legacy vault");
    }

    private static void TestStorageLegacyMigrationRaceRestoresSource(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-legacy-migration-race");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        CreatePortableVault(legacyDirectory, "Race source entry");
        var sourcePath = Path.Combine(
            legacyDirectory,
            StorageLocationService.VaultFileName);
        var targetPath = Path.Combine(
            programDirectory,
            StorageLocationService.VaultFileName);
        var sourceContents = File.ReadAllBytes(sourcePath);
        const string targetSentinel = "target-created-by-another-process";
        File.WriteAllText(targetPath, targetSentinel, new UTF8Encoding(false));

        var migrate = typeof(StorageLocationService).GetMethod(
            "MigrateLegacyVault",
            BindingFlags.Static | BindingFlags.NonPublic);
        Check(migrate is not null, "Legacy migration helper is available for race testing");
        var rejected = false;
        try
        {
            migrate!.Invoke(null, [sourcePath, targetPath]);
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is IOException)
        {
            rejected = true;
        }

        Check(rejected, "Legacy migration refuses a target created by another process");
        Equal(
            targetSentinel,
            File.ReadAllText(targetPath, Encoding.UTF8),
            "Legacy migration race does not overwrite the competing target");
        Equal(
            Convert.ToBase64String(sourceContents),
            Convert.ToBase64String(File.ReadAllBytes(sourcePath)),
            "Failed migration restores the active legacy source name and contents");
        Equal(
            0,
            Directory.GetFiles(
                legacyDirectory,
                StorageLocationService.VaultFileName + ".migrated-*.bak").Length,
            "Failed migration does not leave a migrated archive after rollback");
        Equal(
            0,
            Directory.GetFiles(
                programDirectory,
                StorageLocationService.VaultFileName + ".tmp.*").Length,
            "Failed migration cleans its temporary program files");
    }

    private static void TestStorageLegacyArchiveFailureRetries(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-legacy-archive-retry");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        CreatePortableVault(legacyDirectory, "Archive retry entry");
        var sourcePath = Path.Combine(
            legacyDirectory,
            StorageLocationService.VaultFileName);
        var targetPath = Path.Combine(
            programDirectory,
            StorageLocationService.VaultFileName);
        var migrate = typeof(StorageLocationService).GetMethod(
            "MigrateLegacyVault",
            BindingFlags.Static | BindingFlags.NonPublic);
        Check(migrate is not null, "Legacy migration helper is available for archive retry testing");

        using (new FileStream(
                   sourcePath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.Read))
        {
            migrate!.Invoke(null, [sourcePath, targetPath]);
            Check(
                File.Exists(targetPath),
                "Archive sharing failure still leaves a committed program vault");
            Check(
                File.Exists(sourcePath),
                "Archive sharing failure leaves the legacy source intact");
        }

        var location = new StorageLocationService(
            programDirectory,
            legacyDirectory).Resolve();
        Equal(
            StorageLocationService.NormalizeDirectory(programDirectory),
            location.DataDirectory,
            "Archive cleanup failure does not redirect away from the program vault");
        Check(
            !File.Exists(sourcePath),
            "Next start retries and completes the matching legacy archive");
        Equal(
            1,
            Directory.GetFiles(
                legacyDirectory,
                StorageLocationService.VaultFileName + ".migrated-*.bak").Length,
            "Retried archive leaves exactly one legacy recovery copy");
    }

    private static void TestStorageExplicitRelativePath(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-relative-location");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        var targetDirectory = Path.Combine(programDirectory, "profiles", "数据 目录");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        Directory.CreateDirectory(targetDirectory);
        TouchVault(targetDirectory);

        var service = new StorageLocationService(programDirectory, legacyDirectory);
        var persisted = service.Persist(targetDirectory);
        Equal(
            StorageLocationSource.ExplicitConfiguration,
            persisted.Source,
            "Nested custom directory is persisted explicitly");

        var storedPath = ReadStoredDataDirectory(service.ProgramLocatorFile);
        Check(
            !Path.IsPathRooted(storedPath),
            "Custom directory inside program folder is stored as a relative path");

        var reopenedService = new StorageLocationService(programDirectory, legacyDirectory);
        var resolved = reopenedService.Resolve();
        Equal(
            StorageLocationService.NormalizeDirectory(targetDirectory),
            resolved.DataDirectory,
            "Relative custom path resolves against program directory");
        Equal(
            StorageLocationSource.ExplicitConfiguration,
            resolved.Source,
            "Relative custom path reports explicit source");
        Equal(
            reopenedService.ProgramLocatorFile,
            resolved.ConfigurationFile,
            "Relative custom path records its program-side locator");
    }

    private static void TestStorageExplicitAbsolutePath(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-absolute-location");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        var targetDirectory = Path.Combine(caseRoot, "external-data");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        Directory.CreateDirectory(targetDirectory);
        TouchVault(targetDirectory);

        var service = new StorageLocationService(programDirectory, legacyDirectory);
        var persisted = service.Persist(targetDirectory);
        Equal(
            StorageLocationSource.ExplicitConfiguration,
            persisted.Source,
            "External custom directory is persisted explicitly");

        var storedPath = ReadStoredDataDirectory(service.ProgramLocatorFile);
        Check(
            Path.IsPathRooted(storedPath),
            "Custom directory outside program folder is stored as an absolute path");
        Equal(
            StorageLocationService.NormalizeDirectory(targetDirectory),
            StorageLocationService.NormalizeDirectory(storedPath),
            "Absolute locator stores the selected custom directory");

        var reopenedService = new StorageLocationService(programDirectory, legacyDirectory);
        var resolved = reopenedService.Resolve();
        Equal(
            StorageLocationService.NormalizeDirectory(targetDirectory),
            resolved.DataDirectory,
            "Absolute custom path resolves from program-side locator");
        Equal(
            StorageLocationSource.ExplicitConfiguration,
            resolved.Source,
            "Absolute custom path reports explicit source");
        Equal(
            reopenedService.ProgramLocatorFile,
            resolved.ConfigurationFile,
            "Absolute custom path records its program-side locator");
    }

    private static void TestStorageLocatorMissingVaultRejected(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-missing-vault");
        var programDirectory = Path.Combine(caseRoot, "program");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        var missingTargetDirectory = Path.Combine(programDirectory, "missing-data");
        Directory.CreateDirectory(programDirectory);
        Directory.CreateDirectory(legacyDirectory);
        Directory.CreateDirectory(missingTargetDirectory);
        TouchVault(programDirectory);
        TouchVault(legacyDirectory);

        var service = new StorageLocationService(programDirectory, legacyDirectory);
        WriteStorageLocator(service.ProgramLocatorFile, "missing-data");

        var rejected = false;
        try
        {
            _ = service.Resolve();
        }
        catch (InvalidDataException)
        {
            rejected = true;
        }

        Check(
            rejected,
            "Configured directory without vault is rejected instead of silently falling back");
    }

    private static void TestStorageRecoveryArtifactRejected(string testRoot)
    {
        var artifactSuffixes = new[]
        {
            ".bak",
            ".tmp",
            ".bak.tmp",
            ".tmp.recovery",
            ".migrated-recovery.bak"
        };
        foreach (var artifactSuffix in artifactSuffixes)
        {
            var caseRoot = Path.Combine(
                testRoot,
                "storage-recovery-artifact-" + artifactSuffix.TrimStart('.').Replace('.', '-'));
            var programDirectory = Path.Combine(caseRoot, "program");
            var legacyDirectory = Path.Combine(caseRoot, "legacy");
            Directory.CreateDirectory(programDirectory);
            Directory.CreateDirectory(legacyDirectory);
            CreatePortableVault(legacyDirectory, "Legacy recovery entry");
            var legacyVault = Path.Combine(
                legacyDirectory,
                StorageLocationService.VaultFileName);
            var legacyContents = File.ReadAllBytes(legacyVault);
            var artifactPath = Path.Combine(
                programDirectory,
                StorageLocationService.VaultFileName + artifactSuffix);
            File.WriteAllText(artifactPath, "recovery", new UTF8Encoding(false));

            var rejected = false;
            try
            {
                _ = new StorageLocationService(programDirectory, legacyDirectory).Resolve();
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Check(
                rejected,
                $"Orphaned recovery artifact {artifactSuffix} blocks legacy migration");
            Check(
                !File.Exists(Path.Combine(programDirectory, StorageLocationService.VaultFileName)),
                $"Recovery artifact {artifactSuffix} does not allow a replacement main vault");
            Equal(
                "recovery",
                File.ReadAllText(artifactPath, Encoding.UTF8),
                $"Recovery artifact {artifactSuffix} is not overwritten");
            Equal(
                Convert.ToBase64String(legacyContents),
                Convert.ToBase64String(File.ReadAllBytes(legacyVault)),
                $"Rejected migration for {artifactSuffix} leaves the legacy vault unchanged");

            var legacyCaseRoot = Path.Combine(
                testRoot,
                "storage-legacy-recovery-" + artifactSuffix.TrimStart('.').Replace('.', '-'));
            var emptyProgramDirectory = Path.Combine(legacyCaseRoot, "program");
            var recoveryLegacyDirectory = Path.Combine(legacyCaseRoot, "legacy");
            Directory.CreateDirectory(emptyProgramDirectory);
            Directory.CreateDirectory(recoveryLegacyDirectory);
            var legacyArtifactPath = Path.Combine(
                recoveryLegacyDirectory,
                StorageLocationService.VaultFileName + artifactSuffix);
            File.WriteAllText(
                legacyArtifactPath,
                "legacy-recovery",
                new UTF8Encoding(false));

            var legacyRecoveryRejected = false;
            StorageLocation? legacyRecoveryLocation = null;
            try
            {
                legacyRecoveryLocation = new StorageLocationService(
                    emptyProgramDirectory,
                    recoveryLegacyDirectory).Resolve();
            }
            catch (InvalidDataException)
            {
                legacyRecoveryRejected = true;
            }

            if (artifactSuffix.StartsWith(".migrated-", StringComparison.Ordinal))
            {
                Check(
                    !legacyRecoveryRejected,
                    "A completed legacy migration archive does not show a recovery prompt");
                Equal(
                    StorageLocationService.NormalizeDirectory(emptyProgramDirectory),
                    legacyRecoveryLocation!.DataDirectory,
                    "A completed legacy migration archive falls back to the program directory");
            }
            else
            {
                Check(
                    legacyRecoveryRejected,
                    $"Legacy recovery artifact {artifactSuffix} blocks a new empty program vault");
            }

            Check(
                !File.Exists(Path.Combine(
                    emptyProgramDirectory,
                    StorageLocationService.VaultFileName)),
                $"Legacy recovery artifact {artifactSuffix} is not replaced by an empty vault");
        }
    }

    private static void TestStorageScopedUserLocator(string testRoot)
    {
        var caseRoot = Path.Combine(testRoot, "storage-scoped-user-locator");
        var legacyDirectory = Path.Combine(caseRoot, "legacy");
        var programA = Path.Combine(caseRoot, "program-a");
        var programB = Path.Combine(caseRoot, "program-b");
        var customA = Path.Combine(caseRoot, "custom-a");
        Directory.CreateDirectory(legacyDirectory);
        Directory.CreateDirectory(programA);
        Directory.CreateDirectory(programB);
        Directory.CreateDirectory(customA);
        TouchVault(programA);
        TouchVault(programB);
        TouchVault(customA);

        var serviceA = new StorageLocationService(programA, legacyDirectory);
        var serviceB = new StorageLocationService(programB, legacyDirectory);
        Check(
            !string.Equals(
                serviceA.UserLocatorFile,
                serviceB.UserLocatorFile,
                StringComparison.OrdinalIgnoreCase),
            "User locator is scoped to a specific program directory");

        WriteStorageLocator(serviceA.UserLocatorFile, customA);
        Equal(
            StorageLocationService.NormalizeDirectory(customA),
            serviceA.Resolve().DataDirectory,
            "Scoped user locator overrides the retained source vault for its program copy");
        Equal(
            StorageLocationService.NormalizeDirectory(programB),
            serviceB.Resolve().DataDirectory,
            "Scoped user locator does not redirect another program copy");
    }

    private static void TestDialogTopMostFollowsOwner()
    {
        using var owner = new Form
        {
            Size = new Size(900, 700),
            TopMost = true
        };
        using var dialog = new Form();

        DialogSizing.FitToOwner(
            dialog,
            owner,
            new Size(720, 600),
            new Size(340, 500));
        Check(dialog.TopMost, "Dialog inherits topmost owner");

        owner.TopMost = false;
        DialogSizing.FitToOwner(
            dialog,
            owner,
            new Size(720, 600),
            new Size(340, 500));
        Check(!dialog.TopMost, "Dialog clears topmost for normal owner");
    }

    private static void TestMainFormDesignerVisualTree()
    {
        L.Initialize("zh-Hans");
        Equal(
            typeof(MainFormVisualBase),
            typeof(MainForm).BaseType,
            "MainForm uses the compiled visual designer base directly");
        Check(
            !typeof(MainFormVisualBase).IsAbstract,
            "MainForm visual designer base is concrete");
        Check(
            typeof(MainFormVisualBase).GetConstructor(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                binder: null,
                Type.EmptyTypes,
                modifiers: null) is not null,
            "MainForm visual designer base has a parameterless constructor");

        using var preview = new MainFormDesignerProbe();
        preview.PerformLayout();
        var controls = DescendantControls(preview).ToArray();
        Check(
            controls.Length >= 40,
            "MainForm designer preview creates the complete static control tree");

        foreach (var requiredName in new[]
                 {
                     "titleBar",
                     "sidebar",
                     "dashboardHero",
                     "searchInput",
                     "addButton",
                     "toggleAllCodesButton",
                     "filterAllButton",
                     "emptyState"
                 })
        {
            var control = controls.SingleOrDefault(candidate =>
                candidate.Name == requiredName);
            Check(
                control is not null &&
                control.Width > 0 &&
                control.Height > 0,
                $"MainForm designer preview lays out {requiredName}");
        }

        foreach (var (controlName, key) in new[]
                 {
                     ("topMostCheckBox", "Main.AlwaysOnTop.Label"),
                     ("brandSubtitle", "Main.Brand.Subtitle"),
                     ("vaultStatus", "Vault.Status.Portable"),
                     ("navVault", "Main.Nav.MyAuthenticators"),
                     ("navImport", "Main.Nav.ImportAndMigration"),
                     ("navExport", "Main.Nav.ExportAndBackup"),
                     ("navSettings", "Common.Settings"),
                     ("heroEyebrow", "Dashboard.Eyebrow"),
                     ("heroTitle", "Dashboard.Title"),
                     ("heroSubtitle", "Dashboard.GetStarted"),
                     ("addButton", "Dashboard.AddAuthenticator"),
                     ("importButton", "Common.Import"),
                     ("toggleAllCodesButton", "Dashboard.HideAllCodes"),
                     ("filterAllButton", "Dashboard.Filter.All"),
                     ("filterFavoriteButton", "Dashboard.Filter.Favorites"),
                     ("filterTimeButton", "Dashboard.Filter.TimeBased"),
                     ("filterCounterButton", "Dashboard.Filter.CounterBased"),
                     ("emptyTitle", "Dashboard.Empty.Title"),
                     ("emptyText", "Dashboard.Empty.Description"),
                     ("emptyAddButton", "Dashboard.Empty.AddFirst"),
                     ("lockTitle", "Vault.Locked.Title"),
                     ("lockDescription", "Vault.Locked.Description"),
                     ("unlockButton", "Vault.Unlock")
                 })
        {
            var control = controls.Single(candidate =>
                candidate.Name == controlName);
            Equal(
                L.Get(key),
                control.Text,
                $"MainForm designer preview binds {controlName} to {key}");
        }
        var resultLabel = controls.Single(control =>
            control.Name == "resultLabel");
        Equal(
            L.Format("Dashboard.ResultCount", 0),
            resultLabel.Text,
            "MainForm designer preview localizes the result count");

        var titleBar = controls
            .OfType<AntdUI.PageHeader>()
            .Single(control => control.Name == "titleBar");
        Equal(
            L.Get("Main.Header.Subtitle"),
            titleBar.SubText,
            "MainForm designer preview localizes the header subtitle");
        var searchInput = controls
            .OfType<AntdUI.Input>()
            .Single(control => control.Name == "searchInput");
        Equal(
            L.Get("Dashboard.SearchPlaceholder"),
            searchInput.PlaceholderText,
            "MainForm designer preview localizes the search placeholder");
        var topMostCheckBox = controls
            .OfType<AntdUI.Checkbox>()
            .Single(control => control.Name == "topMostCheckBox");
        Equal(
            L.Get("Main.AlwaysOnTop.Label"),
            topMostCheckBox.AccessibleName,
            "MainForm designer preview localizes the top-most accessible name");
        Equal(
            L.Get("Main.AlwaysOnTop.Description"),
            topMostCheckBox.AccessibleDescription,
            "MainForm designer preview localizes the top-most accessible description");
        foreach (var (controlName, key) in new[]
                 {
                     ("themeButton", "Main.Theme.ToggleTooltip"),
                     ("aboutButton", "About.Tooltip"),
                     ("lockButton", "Main.Vault.LockTooltip")
                 })
        {
            var button = controls
                .OfType<AntdUI.Button>()
                .Single(control => control.Name == controlName);
            Equal(
                L.Get(key),
                button.Tag as string,
                $"MainForm designer preview localizes the {controlName} tooltip");
        }
    }

    private static void TestDialogDesignerVisualTrees()
    {
        L.Initialize("zh-Hans");
        var dialogs = new[]
        {
            new DesignerDialogCase(
                typeof(AboutLicensesDialog),
                typeof(AboutLicensesDialogVisualBase),
                static () => new AboutLicensesDialogDesignerProbe(),
                10,
                [
                    "About.Title",
                    "Common.Done",
                    "About.ProjectRepository",
                    "About.ThirdPartyNotices",
                    "About.ViewLicense"
                ]),
            new DesignerDialogCase(
                typeof(EntryEditorForm),
                typeof(EntryEditorFormVisualBase),
                static () => new EntryEditorFormDesignerProbe(),
                40,
                [
                    "EntryEditor.Title.Add",
                    "Common.Add",
                    "Common.Cancel",
                    "EntryEditor.Verification.Verify"
                ]),
            new DesignerDialogCase(
                typeof(PasswordDialog),
                typeof(PasswordDialogVisualBase),
                static () => new PasswordDialogDesignerProbe(),
                8,
                [
                    "Settings.VaultProtection.SetPassword",
                    "Settings.VaultProtection.PasswordPrompt",
                    "Common.Confirm",
                    "Common.Cancel"
                ]),
            new DesignerDialogCase(
                typeof(QrExportDialog),
                typeof(QrExportDialogVisualBase),
                static () => new QrExportDialogDesignerProbe(),
                10,
                [
                    "QrExportDialog.Title",
                    "QrExportDialog.UriLabel",
                    "QrExportDialog.SecretWarning",
                    "Common.Done",
                    "QrExportDialog.SaveQr",
                    "QrExportDialog.CopyUri"
                ]),
            new DesignerDialogCase(
                typeof(SensitiveValueDialog),
                typeof(SensitiveValueDialogVisualBase),
                static () => new SensitiveValueDialogDesignerProbe(),
                9,
                [
                    "Secret.Title",
                    "Secret.Warning",
                    "Common.Done",
                    "Common.Copy",
                    "Common.Show"
                ]),
            new DesignerDialogCase(
                typeof(TextImportDialog),
                typeof(TextImportDialogVisualBase),
                static () => new TextImportDialogDesignerProbe(),
                9,
                [
                    "TextImportDialog.Title",
                    "TextImportDialog.Hint",
                    "TextImportDialog.Import",
                    "Common.Cancel",
                    "TextImportDialog.Paste"
                ])
        };

        var expectedRuntimeForms = dialogs
            .Select(dialog => dialog.RuntimeType)
            .Append(typeof(MainForm))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        var actualRuntimeForms = typeof(MainForm).Assembly
            .GetTypes()
            .Where(type =>
                type.IsSealed &&
                !type.IsAbstract &&
                typeof(Form).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Equal(
            string.Join("|", expectedRuntimeForms.Select(type => type.FullName)),
            string.Join("|", actualRuntimeForms.Select(type => type.FullName)),
            "Designer coverage includes every concrete application form");
        Check(
            typeof(EntryEditorForm).GetProperty(
                nameof(EntryEditorForm.Result),
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.DeclaredOnly)?.PropertyType ==
            typeof(AuthenticatorEntry),
            "Entry editor keeps its declared public Result API");

        foreach (var dialog in dialogs)
        {
            Equal(
                dialog.VisualBaseType,
                dialog.RuntimeType.BaseType,
                $"{dialog.RuntimeType.Name} uses its compiled visual base directly");
            Check(
                dialog.VisualBaseType.IsPublic &&
                !dialog.VisualBaseType.IsAbstract,
                $"{dialog.VisualBaseType.Name} is public and concrete");
            Check(
                dialog.VisualBaseType.GetConstructor(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    Type.EmptyTypes,
                    modifiers: null) is not null,
                $"{dialog.VisualBaseType.Name} has a parameterless constructor");

            using var preview = dialog.CreatePreview();
            preview.PerformLayout();
            var controls = DescendantControls(preview).ToArray();
            Check(
                controls.Length >= dialog.MinimumControlCount,
                $"{dialog.RuntimeType.Name} designer preview creates its visual tree");
            var header = controls
                .OfType<AntdUI.PageHeader>()
                .SingleOrDefault();
            Check(
                header is not null &&
                header.Width > 0 &&
                header.Height > 0,
                $"{dialog.RuntimeType.Name} designer preview lays out its header");
            Check(
                controls.OfType<AntdUI.Button>().Any(button =>
                    button.Width > 0 &&
                    button.Height > 0),
                $"{dialog.RuntimeType.Name} designer preview lays out its actions");

            var previewTexts = controls
                .Select(control => control.Text)
                .Append(preview.Text)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var key in dialog.RequiredLocalizedTextKeys)
            {
                Check(
                    previewTexts.Contains(L.Get(key)),
                    $"{dialog.RuntimeType.Name} designer preview localizes {key}");
            }

            if (preview is AboutLicensesDialogDesignerProbe)
            {
                Equal(
                    L.Get("About.Title"),
                    preview.Text,
                    "About designer preview localizes the window title");
                var aboutHeader = controls
                    .OfType<AntdUI.PageHeader>()
                    .Single(control => control.Name == "header");
                Equal(
                    L.Get("About.Title"),
                    aboutHeader.Text,
                    "About designer preview localizes the header");
                var versionLabel = controls
                    .OfType<AntdUI.Label>()
                    .Single(control => control.Name == "versionLabel");
                var productLabel = controls
                    .OfType<AntdUI.Label>()
                    .Single(control => control.Name == "productLabel");
                Equal(
                    Program.AppName,
                    productLabel.Text,
                    "About designer preview uses the global application name");
                Equal(
                    L.Format("About.Version", Program.AppVersion),
                    versionLabel.Text,
                    "About designer preview localizes the version");
                var legalSummary = controls
                    .OfType<RichTextBox>()
                    .Single(control => control.Name == "legalSummary");
                Equal(
                    string.Join(
                            Environment.NewLine + Environment.NewLine,
                            L.Get("About.ProjectCopyright"),
                            L.Get("About.ModificationNotice"),
                            L.Get("About.WinAuthAttribution"),
                            L.Get("About.LicenseSummary"),
                            L.Get("About.NoWarranty"),
                            L.Get("About.NonAffiliation"))
                        .Replace("\r\n", "\n", StringComparison.Ordinal),
                    legalSummary.Text.Replace(
                        "\r\n",
                        "\n",
                        StringComparison.Ordinal),
                    "About designer preview localizes the legal summary");
                foreach (var (controlName, key) in new[]
                         {
                             ("closeButton", "Common.Done"),
                             ("repositoryButton", "About.ProjectRepository"),
                             ("noticesButton", "About.ThirdPartyNotices"),
                             ("licenseButton", "About.ViewLicense")
                         })
                {
                    var button = controls
                        .OfType<AntdUI.Button>()
                        .Single(control => control.Name == controlName);
                    Equal(
                        L.Get(key),
                        button.Text,
                        $"About designer preview binds {controlName} to {key}");
                }
            }
            else if (preview is PasswordDialogDesignerProbe)
            {
                Equal(
                    L.Get("Settings.VaultProtection.SetPassword"),
                    preview.Text,
                    "Password designer preview localizes the window title");
                var passwordHeader = controls
                    .OfType<AntdUI.PageHeader>()
                    .Single();
                Equal(
                    L.Get("Settings.VaultProtection.SetPassword"),
                    passwordHeader.Text,
                    "Password designer preview localizes the header");
                var description = controls
                    .OfType<AntdUI.Label>()
                    .Single(label => !string.IsNullOrEmpty(label.Text));
                Equal(
                    L.Get("Settings.VaultProtection.PasswordPrompt"),
                    description.Text,
                    "Password designer preview localizes the description");
                var passwordButtonTexts = controls
                    .OfType<AntdUI.Button>()
                    .Select(button => button.Text)
                    .OrderBy(text => text, StringComparer.Ordinal)
                    .ToArray();
                Equal(
                    string.Join(
                        "|",
                        new[]
                        {
                            L.Get("Common.Confirm"),
                            L.Get("Common.Cancel")
                        }.OrderBy(text => text, StringComparer.Ordinal)),
                    string.Join("|", passwordButtonTexts),
                    "Password designer preview localizes both actions");
                var inputs = controls.OfType<AntdUI.Input>().ToArray();
                Check(
                    inputs.Any(input =>
                        input.PlaceholderText ==
                        L.Get("PasswordDialog.PasswordPlaceholder")),
                    "Password designer preview localizes the password placeholder");
                Check(
                    inputs.Any(input =>
                        input.PlaceholderText ==
                        L.Get("PasswordDialog.ConfirmPasswordPlaceholder")),
                    "Password designer preview localizes the confirmation placeholder");
            }
            else if (preview is SensitiveValueDialogDesignerProbe)
            {
                var sensitiveHeader = controls
                    .OfType<AntdUI.PageHeader>()
                    .Single();
                Equal(
                    L.Get("Secret.Title"),
                    preview.Text,
                    "Sensitive-value designer preview localizes the window title");
                Equal(
                    L.Get("Secret.Title"),
                    sensitiveHeader.Text,
                    "Sensitive-value designer preview localizes the header");
                Equal(
                    L.Get("SensitiveValueDialog.Subtitle"),
                    sensitiveHeader.SubText,
                    "Sensitive-value designer preview localizes the subtitle");
                foreach (var (controlName, key) in new[]
                         {
                             ("descriptionLabel", "Secret.Warning"),
                             ("closeButton", "Common.Done"),
                             ("copyButton", "Common.Copy"),
                             ("revealButton", "Common.Show")
                         })
                {
                    var control = controls.Single(candidate =>
                        candidate.Name == controlName);
                    Equal(
                        L.Get(key),
                        control.Text,
                        $"Sensitive-value designer preview binds {controlName} to {key}");
                }
            }
        }
    }

    private static void TestResizableWindowConfiguration(string testRoot)
    {
        using var vault = new VaultService(Path.Combine(testRoot, "responsive-window-vault.json"));
        var payload = vault.Open();
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;

        var editorConstructor = typeof(EntryEditorForm).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(AuthenticatorEntry), typeof(bool)],
            modifiers: null);
        var passwordConstructor = typeof(PasswordDialog).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(string), typeof(string), typeof(bool), typeof(string)],
            modifiers: null);
        Check(
            editorConstructor is not null && passwordConstructor is not null,
            "Responsive dialog constructors are available");

        using var main = new MainForm(vault, payload, startMinimized: false);
        using var editor = (EntryEditorForm)editorConstructor!.Invoke(
            [SampleEntry(), true]);
        using var password = (PasswordDialog)passwordConstructor!.Invoke(
            ["密码", "说明", false, "确认"]);
        using var textImport = new TextImportDialog();
        using var sensitive = new SensitiveValueDialog(
            "敏感值",
            "说明",
            "sensitive-value");
        using var qr = new QrExportDialog(SampleEntry());
        var windows = new ResponsiveWindow[]
        {
            main,
            editor,
            password,
            textImport,
            sensitive,
            qr
        };

        foreach (var window in windows)
        {
            Check(window.Resizable, $"{window.GetType().Name} explicitly supports resizing");
            Check(window.EnableHitTest, $"{window.GetType().Name} enables edge hit testing");
            Equal(
                FormBorderStyle.None,
                window.FormBorderStyle,
                $"{window.GetType().Name} keeps borderless chrome");
            var header = DescendantControls(window)
                .OfType<AntdUI.PageHeader>()
                .FirstOrDefault();
            Check(
                header is not null && header.DragMove,
                $"{window.GetType().Name} title bar supports dragging");
        }

        Check(
            typeof(MainForm).GetMethod(
                "PreFilterMessage",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly) is null,
            "Main window does not shadow AntdUI edge-resize message filtering");
        Check(
            typeof(MainForm).GetMethod(
                "OnPreFilterMessage",
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly) is not null,
            "Main window tracks activity through the AntdUI message hook");
        var useMessageFilter = typeof(MainForm).GetProperty(
            "UseMessageFilter",
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly);
        Check(
            useMessageFilter is not null &&
            useMessageFilter.GetValue(main) is true,
            "Main window keeps activity tracking active while maximized");

        Equal(
            1200,
            WindowDpi.Convert(1800, 144, WindowDpi.LogicalDpi),
            "Saved window sizes convert from device pixels to logical pixels");
        Equal(
            1800,
            WindowDpi.Convert(1200, WindowDpi.LogicalDpi, 144),
            "Saved logical window sizes convert to the target monitor DPI");

        var modal = DialogSizing.MakeResizable(
            new AntdUI.Modal.Config(main, "响应式确认", "内容"));
        Check(modal.Draggable, "Confirmation windows support dragging");
        Check(modal.Resizable, "Confirmation windows support resizing");
        Equal(
            new Size(320, 180),
            modal.MinimumSize,
            "Confirmation windows keep a usable minimum size");
    }

    private static void TestApplicationBrandIcon(string testRoot)
    {
        using var expectedWindowIcon = ApplicationIconProvider.Create(
            SystemInformation.IconSize);
        using var expectedTrayIcon = ApplicationIconProvider.Create(
            SystemInformation.SmallIconSize);
        using var vault = new VaultService(
            Path.Combine(testRoot, "application-icon-vault.json"));
        var payload = vault.Open();
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;

        using var form = new MainForm(vault, payload, startMinimized: false);
        Check(form.Icon is not null, "Main window has a branded application icon");
        Equal(
            IconFingerprint(expectedWindowIcon),
            IconFingerprint(form.Icon!),
            "Main window uses the embedded application icon");

        var trayIcon = (NotifyIcon)typeof(MainForm)
            .GetField("_trayIcon", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        Check(trayIcon.Icon is not null, "Tray notification has a branded icon");
        Equal(
            IconFingerprint(expectedTrayIcon),
            IconFingerprint(trayIcon.Icon!),
            "Tray notification uses the embedded small application icon");
    }

    private static void TestWindowLayoutPreferences(string testRoot)
    {
        using var vault = new VaultService(
            Path.Combine(testRoot, "window-layout-preferences-vault.json"));
        var payload = vault.Open();
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;
        payload.Settings.RememberWindowLayout = false;
        payload.Settings.WindowLeft = 117;
        payload.Settings.WindowTop = 219;
        payload.Settings.WindowWidth = 701;
        payload.Settings.WindowHeight = 611;

        using var form = new MainForm(vault, payload, startMinimized: false)
        {
            ShowInTaskbar = false,
            Opacity = 0
        };
        form.Show();
        Application.DoEvents();
        var initialScreen = Screen.FromControl(form);
        var initialWorkingArea = initialScreen.WorkingArea;
        form.ScreenRectangle = new Rectangle(
            initialWorkingArea.Left + 41,
            initialWorkingArea.Top + 53,
            Math.Min(
                WindowDpi.Convert(820, WindowDpi.LogicalDpi, form.DeviceDpi),
                initialWorkingArea.Width),
            Math.Min(
                WindowDpi.Convert(640, WindowDpi.LogicalDpi, form.DeviceDpi),
                initialWorkingArea.Height));
        Application.DoEvents();
        var captureWindowBounds = typeof(MainForm).GetMethod(
            "CaptureWindowBounds",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var setRememberWindowLayout = typeof(MainForm).GetMethod(
            "SetRememberWindowLayout",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var applyDefaultWindowLayout = typeof(MainForm).GetMethod(
            "ApplyDefaultWindowLayout",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var buildSettings = typeof(MainForm).GetMethod(
            "BuildSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(
            captureWindowBounds is not null &&
            setRememberWindowLayout is not null &&
            applyDefaultWindowLayout is not null &&
            buildSettings is not null,
            "Window layout preference actions are available");

        captureWindowBounds!.Invoke(form, null);
        Equal(
            117,
            payload.Settings.WindowLeft,
            "Disabled window layout memory does not capture window movement");
        Equal(
            701,
            payload.Settings.WindowWidth,
            "Disabled window layout memory does not capture window resizing");

        setRememberWindowLayout!.Invoke(form, [true]);
        Check(
            payload.Settings.RememberWindowLayout,
            "Window layout memory can be enabled");
        Equal(
            form.Left,
            payload.Settings.WindowLeft,
            "Enabling window layout memory captures the current position");
        Equal(
            form.Width,
            payload.Settings.WindowWidth,
            "Enabling window layout memory captures the current size");

        setRememberWindowLayout.Invoke(form, [false]);
        Check(
            !payload.Settings.RememberWindowLayout,
            "Window layout memory can be disabled");
        payload.Settings.WindowLeft = 333;
        payload.Settings.WindowTop = 444;
        payload.Settings.WindowWidth = 555;
        payload.Settings.WindowHeight = 666;
        payload.Settings.WindowDpi = 144;
        payload.Settings.WindowMaximized = true;
        var resetScreen = Screen.FromControl(form);
        var resetWorkingArea = resetScreen.WorkingArea;
        var expectedResetWidth = Math.Min(
            WindowDpi.Convert(
                AppSettings.DefaultWindowWidth,
                WindowDpi.LogicalDpi,
                form.DeviceDpi),
            resetWorkingArea.Width);
        var expectedResetHeight = Math.Min(
            WindowDpi.Convert(
                AppSettings.DefaultWindowHeight,
                WindowDpi.LogicalDpi,
                form.DeviceDpi),
            resetWorkingArea.Height);
        var expectedResetBounds = new Rectangle(
            resetWorkingArea.Left +
            ((resetWorkingArea.Width - expectedResetWidth) / 2),
            resetWorkingArea.Top +
            ((resetWorkingArea.Height - expectedResetHeight) / 2),
            expectedResetWidth,
            expectedResetHeight);
        applyDefaultWindowLayout!.Invoke(form, null);
        Application.DoEvents();
        Equal(-1, payload.Settings.WindowLeft, "Window layout reset clears the saved left edge");
        Equal(-1, payload.Settings.WindowTop, "Window layout reset clears the saved top edge");
        Equal(
            AppSettings.DefaultWindowWidth,
            payload.Settings.WindowWidth,
            "Window layout reset restores the default width");
        Equal(
            AppSettings.DefaultWindowHeight,
            payload.Settings.WindowHeight,
            "Window layout reset restores the default height");
        Equal(0, payload.Settings.WindowDpi, "Window layout reset clears the saved DPI");
        Check(
            !payload.Settings.WindowMaximized,
            "Window layout reset clears the maximized state");
        Equal(
            FormWindowState.Normal,
            form.WindowState,
            "Window layout reset returns the window to its normal state");
        Equal(
            expectedResetBounds,
            form.ScreenRectangle,
            "Window layout reset restores and centers the default bounds at the current DPI");

        buildSettings!.Invoke(form, null);
        var settingsText = DescendantControls(form)
            .Select(control => control.Text)
            .ToHashSet(StringComparer.Ordinal);
        Check(
            settingsText.Contains("记住窗口布局"),
            "Settings expose the window layout memory switch");
        Check(
            settingsText.Contains("重置布局"),
            "Settings expose the window layout reset action");
    }

    private static void TestNarrowWindowLayouts(string testRoot)
    {
        using var vault = new VaultService(Path.Combine(testRoot, "narrow-layout-vault.json"));
        var payload = vault.Open();
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;

        using var main = new MainForm(vault, payload, startMinimized: false)
        {
            Size = new Size(360, 540),
            ShowInTaskbar = false,
            Opacity = 0,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32_000, -32_000)
        };
        main.Show();
        Application.DoEvents();
        int ScaleForWindow(int logicalPixels) => Math.Max(
            1,
            (int)Math.Round(logicalPixels * (main.DeviceDpi / 96F)));
        main.Size = new Size(ScaleForWindow(360), ScaleForWindow(540));
        Application.DoEvents();

        var layoutShell = typeof(MainForm).GetMethod(
            "LayoutShell",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var buildSettings = typeof(MainForm).GetMethod(
            "BuildSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(
            layoutShell is not null && buildSettings is not null,
            "Responsive main-window layout actions are available");
        layoutShell!.Invoke(main, null);

        var emptyState = (Control)typeof(MainForm)
            .GetField("_emptyState", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(main)!;
        var emptyAddButton = (Control)typeof(MainForm)
            .GetField("_emptyAddButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(main)!;
        Check(
            emptyAddButton.Visible && IsInside(emptyAddButton, emptyState),
            "Minimum-height empty state keeps its add button visible");
        var searchInput = (Control)typeof(MainForm)
            .GetField("_searchInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(main)!;
        var toggleAllCodesButton = (Control)typeof(MainForm)
            .GetField(
                "_toggleAllCodesButton",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(main)!;
        var filterBar = (Control)typeof(MainForm)
            .GetField("_filterBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(main)!;
        Check(
            IsInside(searchInput, filterBar) &&
            IsInside(toggleAllCodesButton, filterBar) &&
            searchInput.Right < toggleAllCodesButton.Left,
            "Narrow dashboard keeps search and bulk visibility controls separated");

        var contentHost = (Control)typeof(MainForm)
            .GetField("_contentHost", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(main)!;
        main.Width = ScaleForWindow(873);
        Application.DoEvents();
        var narrowContentWidth = contentHost.Width;
        main.Width = ScaleForWindow(874);
        Application.DoEvents();
        Check(
            contentHost.Width >= ScaleForWindow(650) &&
            contentHost.Width <= narrowContentWidth,
            "Desktop sidebar breakpoint preserves a usable content width");

        main.Size = new Size(ScaleForWindow(360), ScaleForWindow(540));
        buildSettings!.Invoke(main, null);
        layoutShell.Invoke(main, null);
        Application.DoEvents();
        var settingsFlow = (FlowLayoutPanel)typeof(MainForm)
            .GetField("_settingsFlow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(main)!;
        var sections = settingsFlow.Controls
            .Cast<Control>()
            .Where(control => Equals(control.Tag, "settings-section"))
            .ToArray();
        Check(sections.Length >= 4, "Responsive settings sections are built");
        Check(
            sections.All(section =>
                section.Left >= settingsFlow.Padding.Left &&
                section.Right <= settingsFlow.ClientSize.Width -
                settingsFlow.Padding.Right),
            "Narrow settings sections remain inside the horizontal viewport");
        Check(
            !settingsFlow.HorizontalScroll.Visible,
            "Narrow settings page does not require horizontal scrolling");
        var settingRows = sections
            .SelectMany(section => section.Controls.Cast<Control>())
            .Where(control => control.GetType() == typeof(Panel))
            .ToArray();
        Check(
            settingRows.Any(row => row.Height > 72),
            "Narrow settings rows stack labels and editors");
        Check(
            sections.All(section =>
                section.Controls
                    .Cast<Control>()
                    .Where(control => control.GetType() == typeof(Panel))
                    .All(row => row.Bottom <= section.ClientSize.Height)),
            "Stacked settings rows remain inside their sections");
        foreach (var logicalWidth in new[] { 600, 608, 616 })
        {
            main.Size = new Size(
                ScaleForWindow(logicalWidth),
                ScaleForWindow(540));
            layoutShell.Invoke(main, null);
            Application.DoEvents();
            Check(
                settingRows.All(row =>
                    row.Controls
                        .Cast<Control>()
                        .All(control => control.Bottom <= row.ClientSize.Height)),
                $"Settings editors remain inside their rows at {logicalWidth}px");
            Check(
                sections.All(section =>
                    section.Controls
                        .Cast<Control>()
                        .Where(control => control.GetType() == typeof(Panel))
                        .All(row => row.Bottom <= section.ClientSize.Height)),
                $"Settings rows remain inside their sections at {logicalWidth}px");
        }

        const string responsiveSensitiveValue =
            "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";
        using var sensitive = new SensitiveValueDialog(
            "敏感值",
            "这是需要在窄窗口中完整显示的说明。",
            responsiveSensitiveValue,
            initiallyRevealed: true)
        {
            ShowInTaskbar = false,
            Opacity = 0,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-31_000, -31_000)
        };
        sensitive.Show();
        sensitive.Size = sensitive.MinimumSize;
        Application.DoEvents();
        var sensitiveValueLabel = (AntdUI.Label)typeof(SensitiveValueDialog)
            .GetField("_valueLabel", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(sensitive)!;
        var wrappedSensitiveValue = sensitiveValueLabel.Text ?? string.Empty;
        Equal(
            responsiveSensitiveValue,
            wrappedSensitiveValue.Replace(Environment.NewLine, string.Empty),
            "Sensitive value wrapping preserves the complete value");
        Check(
            wrappedSensitiveValue.Contains(Environment.NewLine),
            "A long sensitive value wraps at the minimum dialog width");
        CheckButtonsInside(
            sensitive,
            ["隐藏", "复制", "完成"],
            "Sensitive value dialog");

        using var qr = new QrExportDialog(SampleEntry())
        {
            ShowInTaskbar = false,
            Opacity = 0,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-30_000, -30_000)
        };
        qr.Show();
        qr.Size = qr.MinimumSize;
        Application.DoEvents();
        CheckButtonsInside(
            qr,
            ["复制 URI", "保存二维码", "完成"],
            "QR export dialog");

        using var textImport = new TextImportDialog
        {
            ShowInTaskbar = false,
            Opacity = 0,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-29_000, -29_000)
        };
        textImport.Show();
        textImport.Size = textImport.MinimumSize;
        Application.DoEvents();
        CheckButtonsInside(
            textImport,
            ["粘贴", "取消", "解析并导入"],
            "Text import dialog");

        main.Close();
    }

    private static void TestLongLocalizedLabelsRemainResponsive(string testRoot)
    {
        L.Initialize("de");
        try
        {
            using var vault = new VaultService(
                Path.Combine(testRoot, "localized-layout-vault.json"));
            var payload = vault.Open();
            payload.Settings.AutoLockMinutes = 0;
            payload.Settings.MinimizeToTray = false;
            payload.Settings.CloseToTray = false;

            using var form = new MainForm(vault, payload, startMinimized: false)
            {
                Size = new Size(1080, 720),
                ShowInTaskbar = false,
                Opacity = 0,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-28_000, -28_000)
            };
            form.Show();
            Application.DoEvents();

            var layoutShell = typeof(MainForm).GetMethod(
                "LayoutShell",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var buildSettings = typeof(MainForm).GetMethod(
                "BuildSettings",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Check(
                layoutShell is not null && buildSettings is not null,
                "Localized responsive layout actions are available");
            layoutShell!.Invoke(form, null);

            var filterButtons = new[]
            {
                "_filterAllButton",
                "_filterFavoriteButton",
                "_filterTimeButton",
                "_filterCounterButton"
            }.Select(field => (AntdUI.Button)typeof(MainForm)
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(form)!)
                .ToArray();
            Check(
                filterButtons.All(button =>
                    button.Width >=
                    TextRenderer.MeasureText(button.Text, button.Font).Width),
                "Desktop filter buttons fit longer translated labels");

            var topMost = (AntdUI.Checkbox)typeof(MainForm)
                .GetField("_topMostCheckBox", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(form)!;
            Check(
                topMost.Width >= TextRenderer.MeasureText(topMost.Text, topMost.Font).Width,
                "Header checkbox grows for longer translated labels");

            form.Size = new Size(600, 540);
            buildSettings!.Invoke(form, null);
            layoutShell.Invoke(form, null);
            Application.DoEvents();
            var settingsFlow = (FlowLayoutPanel)typeof(MainForm)
                .GetField("_settingsFlow", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(form)!;
            var sections = settingsFlow.Controls
                .Cast<Control>()
                .Where(control => Equals(control.Tag, "settings-section"))
                .ToArray();
            Check(
                sections.All(section =>
                    section.Left >= settingsFlow.Padding.Left &&
                    section.Right <= settingsFlow.ClientSize.Width -
                    settingsFlow.Padding.Right),
                "Translated settings sections stay inside the viewport");
            Check(
                !settingsFlow.HorizontalScroll.Visible,
                "Long translated settings do not add horizontal scrolling");
        }
        finally
        {
            L.Initialize("zh-Hans");
        }
    }

    private static void TestEditorSelectWheelProtection()
    {
        var factory = typeof(EntryEditorForm).GetMethod(
            "CreateSelect",
            BindingFlags.NonPublic | BindingFlags.Static);
        Check(factory is not null, "Editor select factory is available");

        using var select = (AntdUI.Select?)factory!.Invoke(
            null,
            [new object[] { "First", "Second" }]);
        Check(select is not null, "Editor select factory creates a select");
        Check(
            select!.WheelModifyEnabled == false,
            "Editor select ignores wheel value changes");
    }

    private static void TestSettingsSelectWheelProtection(string testRoot)
    {
        using var vault = new VaultService(
            Path.Combine(testRoot, "settings-select-wheel-vault.json"));
        var payload = vault.Open();
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;

        using var form = new MainForm(vault, payload, startMinimized: false);
        var buildSettings = typeof(MainForm).GetMethod(
            "BuildSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(buildSettings is not null, "Settings builder is available");
        buildSettings!.Invoke(form, null);

        var selects = DescendantControls(form)
            .OfType<AntdUI.Select>()
            .ToArray();
        Check(selects.Length > 0, "Settings expose a theme select");
        Check(
            selects.All(select => select.WheelModifyEnabled == false),
            "Settings selects ignore wheel value changes");
    }

    private static void TestAuthenticatorCardVisibilityToggle()
    {
        using var card = new AuthenticatorCard(SampleEntry());
        var codeLabel = (AntdUI.Label)typeof(AuthenticatorCard)
            .GetField("_codeLabel", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(card)!;
        var revealButton = (AntdUI.Button)typeof(AuthenticatorCard)
            .GetField("_revealButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(card)!;

        Check(!card.CodeHidden, "Code is visible when hide-by-default is disabled");
        Equal(
            OtpService.FormatCode(card.CurrentCode),
            codeLabel.Text,
            "Visible card renders the current code");
        Equal(
            "EyeInvisibleOutlined",
            revealButton.IconSvg,
            "Visible card offers the hide action");

        var clickButton = typeof(Control).GetMethod(
            "OnClick",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        clickButton.Invoke(revealButton, [EventArgs.Empty]);
        Check(card.CodeHidden, "Card records its hidden state");
        Equal("•••• ••••", codeLabel.Text, "Hidden card masks every code digit");
        Equal(
            "EyeOutlined",
            revealButton.IconSvg,
            "Hidden card offers the show action");

        clickButton.Invoke(revealButton, [EventArgs.Empty]);
        Check(!card.CodeHidden, "Second button click shows the hidden code");
        Equal(
            OtpService.FormatCode(card.CurrentCode),
            codeLabel.Text,
            "Second toggle restores the current code");

        card.GlobalHideCodes = true;
        Check(card.CodeHidden, "Enabling hide-by-default resets the card to hidden");
        Check(
            card.ToggleCodeVisibility(),
            "A hidden-by-default card can still be shown explicitly");
        card.RefreshCode(DateTimeOffset.UtcNow.AddMinutes(1));
        Check(
            !card.CodeHidden,
            "A code refresh preserves the user's explicit visibility choice");
        card.HideNow();
        Check(card.CodeHidden, "Explicit hide immediately masks a shown code");

        var hotpEntry = SampleEntry();
        hotpEntry.Kind = AuthenticatorKind.Hotp;
        hotpEntry.Counter = 45;
        using var hotpCard = new AuthenticatorCard(hotpEntry);
        Check(
            DescendantControls(hotpCard)
                .OfType<AntdUI.Button>()
                .All(button => button.Text != L.Get("AuthenticatorCard.GenerateNext")),
            "HOTP card does not show the generate-next action in its main layout");
        Check(
            hotpCard.BuildContextMenuItems().Any(item =>
                item is AntdUI.ContextMenuStripItem
                {
                    Tag: AuthenticatorCardAction.AdvanceCounter
                }),
            "HOTP generate-next action remains available in the more menu");
    }

    private static void TestMainFormBulkCodeVisibility(string testRoot)
    {
        using var vault = new VaultService(
            Path.Combine(testRoot, "bulk-code-visibility-vault.json"));
        var payload = vault.Open();
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;
        var first = SampleEntry();
        first.Name = "first.account@example.test";
        var second = SampleEntry();
        second.Name = "second.account@example.test";
        second.SortOrder = 10;
        payload.Entries.AddRange([first, second]);
        vault.Save(payload);

        using var form = new MainForm(vault, payload, startMinimized: false)
        {
            Size = new Size(1080, 720),
            ShowInTaskbar = false,
            Opacity = 0,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32_000, -32_000)
        };
        form.Show();
        Application.DoEvents();

        var toggleAllButton = (AntdUI.Button)typeof(MainFormVisualBase)
            .GetField(
                "_toggleAllCodesButton",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        var searchInput = (AntdUI.Input)typeof(MainFormVisualBase)
            .GetField("_searchInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        var cardFlow = (FlowLayoutPanel)typeof(MainFormVisualBase)
            .GetField("_cardFlow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        var clickButton = typeof(Control).GetMethod(
            "OnClick",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        Check(toggleAllButton.Enabled, "Bulk visibility button is interactive when entries exist");
        CheckButtonInteractionContrast(
            toggleAllButton,
            "Dark bulk visibility button");
        var applyTheme = typeof(MainForm).GetMethod(
            "ApplyTheme",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(applyTheme is not null, "Main-window theme action is available");
        ThemePaletteService.Apply(AppThemeMode.Light);
        applyTheme!.Invoke(form, null);
        CheckButtonInteractionContrast(
            toggleAllButton,
            "Light bulk visibility button");
        ThemePaletteService.Apply(AppThemeMode.Dark);
        applyTheme.Invoke(form, null);

        Equal(
            L.Get("Dashboard.HideAllCodes"),
            toggleAllButton.Text,
            "Bulk visibility button initially offers to hide all codes");
        searchInput.Text = first.Name;
        Application.DoEvents();
        Equal(
            1,
            cardFlow.Controls.OfType<AuthenticatorCard>().Count(),
            "Search filter leaves one rendered card");

        clickButton.Invoke(toggleAllButton, [EventArgs.Empty]);
        searchInput.Text = string.Empty;
        Application.DoEvents();
        var hiddenCards = cardFlow.Controls.OfType<AuthenticatorCard>().ToArray();
        Equal(2, hiddenCards.Length, "Clearing search restores both cards");
        Check(
            hiddenCards.All(card => card.CodeHidden),
            "Hide-all applies to entries that were outside the active filter");
        Equal(
            L.Get("Dashboard.ShowAllCodes"),
            toggleAllButton.Text,
            "Bulk visibility button switches to the show-all action");

        clickButton.Invoke(toggleAllButton, [EventArgs.Empty]);
        Check(
            cardFlow.Controls.OfType<AuthenticatorCard>().All(card => !card.CodeHidden),
            "Show-all reveals every card");

        var firstCard = cardFlow.Controls
            .OfType<AuthenticatorCard>()
            .Single(card => card.Entry.Id == first.Id);
        var firstRevealButton = (AntdUI.Button)typeof(AuthenticatorCard)
            .GetField("_revealButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(firstCard)!;
        clickButton.Invoke(firstRevealButton, [EventArgs.Empty]);
        searchInput.Text = "no-match";
        searchInput.Text = string.Empty;
        Application.DoEvents();
        Check(
            cardFlow.Controls
                .OfType<AuthenticatorCard>()
                .Single(card => card.Entry.Id == first.Id)
                .CodeHidden,
            "Individual visibility choice survives card-list rebuilds");
    }

    private static void TestAutoLockSetting(string testRoot)
    {
        var vaultPath = Path.Combine(testRoot, "auto-lock-setting-vault.json");
        using var vault = new VaultService(vaultPath);
        var payload = vault.Open();
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;

        using var form = new MainForm(vault, payload, startMinimized: false);
        var buildSettings = typeof(MainForm).GetMethod(
            "BuildSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(buildSettings is not null, "Settings builder is available");
        buildSettings!.Invoke(form, null);

        var autoLockInput = DescendantControls(form)
            .OfType<AntdUI.Input>()
            .Single(input => input.Name == "autoLockMinutesInput");
        Equal(
            AppSettings.DefaultAutoLockMinutes.ToString(),
            autoLockInput.Text,
            "Settings show the default auto-lock interval");
        Equal(
            L.Get("Common.Minutes"),
            autoLockInput.SuffixText,
            "Auto-lock setting identifies its unit");
        var clipboardInput = DescendantControls(form)
            .OfType<AntdUI.Input>()
            .Single(input => input.Name == "clipboardClearSecondsInput");
        Equal(
            AppSettings.DefaultClipboardClearSeconds.ToString(),
            clipboardInput.Text,
            "Settings show zero as the default clipboard-clear delay");

        autoLockInput.Text = "37";
        typeof(Control).GetMethod(
                "OnLostFocus",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(autoLockInput, [EventArgs.Empty]);
        Equal(37, payload.Settings.AutoLockMinutes, "Custom auto-lock interval is applied");

        using (var reopenedVault = new VaultService(vaultPath))
        {
            var persisted = reopenedVault.Open();
            Equal(
                37,
                persisted.Settings.AutoLockMinutes,
                "Custom auto-lock interval is persisted immediately");
        }

        autoLockInput.Text = "2000";
        typeof(Control).GetMethod(
                "OnLostFocus",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(autoLockInput, [EventArgs.Empty]);
        Equal(
            AppSettings.MaximumAutoLockMinutes,
            payload.Settings.AutoLockMinutes,
            "Auto-lock interval is capped at the supported maximum");

        autoLockInput.Text = "-1";
        typeof(Control).GetMethod(
                "OnLostFocus",
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(autoLockInput, [EventArgs.Empty]);
        Equal(
            AppSettings.MinimumAutoLockMinutes,
            payload.Settings.AutoLockMinutes,
            "Zero disables auto-lock and negative input is normalized to zero");

        var importedPayload = new VaultPayload
        {
            Settings = new AppSettings { AutoLockMinutes = int.MaxValue }
        };
        importedPayload.Normalize();
        Equal(
            AppSettings.MaximumAutoLockMinutes,
            importedPayload.Settings.AutoLockMinutes,
            "Loaded auto-lock settings are normalized to the supported range");
    }

    private static void TestProviderSectionOrdering()
    {
        var constructor = typeof(EntryEditorForm).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(AuthenticatorEntry), typeof(bool)],
            modifiers: null);
        Check(constructor is not null, "Editor constructor is available");

        using var editor = (EntryEditorForm)constructor!.Invoke(
            [new AuthenticatorEntry(), true]);
        editor.ShowInTaskbar = false;
        editor.Opacity = 0;
        editor.StartPosition = FormStartPosition.Manual;
        editor.Location = new Point(-32_000, -32_000);
        editor.Show();
        Application.DoEvents();

        var kindSelect = (AntdUI.Select)typeof(EntryEditorForm)
            .GetField("_kindSelect", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var providerSection = (Control)typeof(EntryEditorForm)
            .GetField("_providerSection", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var serialRow = (Control)typeof(EntryEditorForm)
            .GetField("_serialRow", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var issuerInput = (AntdUI.Input)typeof(EntryEditorForm)
            .GetField("_issuerInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var nameInput = (AntdUI.Input)typeof(EntryEditorForm)
            .GetField("_nameInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var secretInput = (AntdUI.Input)typeof(EntryEditorForm)
            .GetField("_secretInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var serialInput = (AntdUI.Input)typeof(EntryEditorForm)
            .GetField("_serialInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var deviceIdInput = (AntdUI.Input)typeof(EntryEditorForm)
            .GetField("_deviceIdInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var steamDataInput = (AntdUI.Input)typeof(EntryEditorForm)
            .GetField("_steamDataInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        kindSelect.SelectedIndex = 6; // Steam Guard
        Application.DoEvents();
        Check(providerSection.Visible, "Steam provider section is visible");
        Equal("Steam", issuerInput.Text, "Steam selection fills its default issuer");
        Check(
            providerSection.Top < serialRow.Top,
            "Provider section precedes provider fields");

        var steamSessionOnly = new AuthenticatorEntry { Kind = AuthenticatorKind.Steam };
        steamSessionOnly.ProviderData["steamSessionData"] = "session-only";
        steamDataInput.Text = string.Empty;
        var populate = typeof(EntryEditorForm).GetMethod(
            "PopulateEntryFromInputs",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(populate is not null, "Editor population action is available");
        populate!.Invoke(editor, [steamSessionOnly]);
        Equal(
            "session-only",
            steamSessionOnly.ProviderData.GetValueOrDefault("steamSessionData"),
            "Saving Steam preserves hidden session-only migration data");

        serialInput.Text = "US-1234-5678-9012";
        deviceIdInput.Text = "device-123";
        steamDataInput.Text = "{\"steam\":\"data\"}";
        kindSelect.SelectedIndex = 7; // Battle.net
        Application.DoEvents();
        Equal(
            "Battle.net",
            issuerInput.Text,
            "Changing provider replaces an automatically filled issuer");

        nameInput.Text = "battle@example.com";
        secretInput.Text = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        var populated = new AuthenticatorEntry();
        populated.ProviderData["steamSessionData"] = "stale-session";
        populate.Invoke(editor, [populated]);
        Equal(AuthenticatorKind.BattleNet, populated.Kind, "Editor keeps selected provider kind");
        Equal(
            "US-1234-5678-9012",
            populated.Serial,
            "Battle.net keeps its applicable serial");
        Check(populated.DeviceId is null, "Battle.net clears hidden device data");
        Check(
            !populated.ProviderData.ContainsKey("steamData") &&
            !populated.ProviderData.ContainsKey("steamSessionData"),
            "Non-Steam providers clear hidden Steam data");
        var roundTripped = OtpAuthUriService.ParseSingle(OtpAuthUriService.Build(populated));
        Equal(
            AuthenticatorKind.BattleNet,
            roundTripped.Kind,
            "Provider URI round-trip keeps the selected provider kind");

        issuerInput.Text = "Custom provider";
        kindSelect.SelectedIndex = 8; // Trion / Glyph
        Application.DoEvents();
        Equal(
            "Custom provider",
            issuerInput.Text,
            "Changing provider preserves a user-entered issuer");
        var customProvider = new AuthenticatorEntry();
        populate.Invoke(editor, [customProvider]);
        var customRoundTrip = OtpAuthUriService.ParseSingle(
            OtpAuthUriService.Build(customProvider));
        Equal(
            AuthenticatorKind.Trion,
            customRoundTrip.Kind,
            "Explicit URI provider marker preserves a custom Trion issuer");
        var providerInstant = DateTimeOffset.FromUnixTimeSeconds(59);
        Equal(
            OtpService.GetCode(customProvider, providerInstant),
            OtpService.GetCode(customRoundTrip, providerInstant),
            "Provider URI round-trip preserves its code algorithm");

        kindSelect.SelectedIndex = 0; // Generic TOTP
        Application.DoEvents();
        Check(!providerSection.Visible, "Generic TOTP provider section is hidden");
        populate.Invoke(editor, [populated]);
        Check(populated.Serial is null, "Generic TOTP clears hidden provider serial data");
        Check(populated.DeviceId is null, "Generic TOTP clears hidden provider device data");
        editor.Close();
    }

    private static void TestEditorAuthenticatorVerification()
    {
        var constructor = typeof(EntryEditorForm).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(AuthenticatorEntry), typeof(bool)],
            modifiers: null);
        Check(constructor is not null, "Editor constructor is available for verification");

        var source = new AuthenticatorEntry
        {
            Name = string.Empty,
            Issuer = "HOTP test",
            Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            Kind = AuthenticatorKind.Hotp,
            Counter = 0,
            TimeOffsetSeconds = 60
        };
        using var editor = (EntryEditorForm)constructor!.Invoke([source, true]);
        editor.ShowInTaskbar = false;
        editor.Opacity = 0;
        editor.StartPosition = FormStartPosition.Manual;
        editor.Location = new Point(-32_000, -32_000);
        editor.ClientSize = new Size(340, 500);
        editor.Show();
        Application.DoEvents();

        var verify = typeof(EntryEditorForm).GetMethod(
            "VerifyAuthenticator",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(verify is not null, "Editor verification action is available");
        verify!.Invoke(editor, null);
        Application.DoEvents();

        var codeLabel = (AntdUI.Label)typeof(EntryEditorForm)
            .GetField("_verificationCodeLabel", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var statusLabel = (AntdUI.Label)typeof(EntryEditorForm)
            .GetField("_verificationStatusLabel", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var timer = (System.Windows.Forms.Timer)typeof(EntryEditorForm)
            .GetField("_verificationTimer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var counterInput = (AntdUI.Input)typeof(EntryEditorForm)
            .GetField("_counterInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var algorithmSelect = (AntdUI.Select)typeof(EntryEditorForm)
            .GetField("_algorithmSelect", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var verificationPanel = (Control)typeof(EntryEditorForm)
            .GetField("_verificationPanel", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        var workingCopy = (AuthenticatorEntry)typeof(EntryEditorForm)
            .GetField("_workingCopy", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;

        Equal("755 224", codeLabel.Text, "Editor HOTP verification code");
        Check(
            (statusLabel.Text ?? string.Empty).Contains(
                "预览不会推进计数器",
                StringComparison.Ordinal),
            "Editor explains HOTP verification side effect");
        Equal(0L, source.Counter, "Editor verification preserves source HOTP counter");
        Equal(0L, workingCopy.Counter, "Editor verification preserves working HOTP counter");
        Check(!timer.Enabled, "HOTP verification does not start refresh timer");
        Check(editor.Visible, "Verification does not close editor");
        Check(
            verificationPanel.Top < algorithmSelect.Parent!.Top,
            "Editor verification precedes OTP parameters");
        Check(
            verificationPanel.Controls.Cast<Control>().All(control =>
                control.Left >= 0 &&
                control.Top >= 0 &&
                control.Right <= verificationPanel.ClientSize.Width &&
                control.Bottom <= verificationPanel.ClientSize.Height),
            "Verification panel remains inside narrow editor bounds");
        var tenDigitWidth = TextRenderer.MeasureText(
            "12345 67890",
            codeLabel.Font,
            new Size(int.MaxValue, codeLabel.Height),
            TextFormatFlags.NoPadding).Width;
        Check(
            tenDigitWidth <= codeLabel.ClientSize.Width,
            "Narrow verification panel can display a formatted 10-digit code");

        counterInput.Text = "1";
        Application.DoEvents();
        Equal("参数已更改", codeLabel.Text, "OTP parameter changes invalidate verification");

        var secretInput = (AntdUI.Input)typeof(EntryEditorForm)
            .GetField("_secretInput", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor)!;
        secretInput.Text = "ABC";
        verify.Invoke(editor, null);
        Application.DoEvents();
        Equal("验证失败", codeLabel.Text, "Invalid secret fails editor verification");
        Check(!timer.Enabled, "Invalid verification does not start refresh timer");

        var applyImported = typeof(EntryEditorForm).GetMethod(
            "ApplyImportedEntry",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(applyImported is not null, "Editor URI import action is available");
        applyImported!.Invoke(
            editor,
            [
                new AuthenticatorEntry
                {
                    Name = "replacement",
                    Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
                    Kind = AuthenticatorKind.Totp,
                    Digits = 8,
                    Period = 30,
                    TimeOffsetSeconds = 0
                },
                false
            ]);
        Equal(
            0L,
            workingCopy.TimeOffsetSeconds,
            "Replacing an imported entry clears its legacy time offset");
        verify.Invoke(editor, null);
        Check(timer.Enabled, "TOTP verification starts the preview refresh timer");
        var refreshPreview = typeof(EntryEditorForm).GetMethod(
            "RefreshVerificationPreview",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(refreshPreview is not null, "Editor verification refresh action is available");
        refreshPreview!.Invoke(editor, [DateTimeOffset.FromUnixTimeSeconds(59)]);
        Equal("9428 7082", codeLabel.Text, "Editor TOTP verification refresh uses the selected parameters");
        editor.Close();
        editor.Dispose();
        Check(!timer.Enabled, "Closing the editor stops the verification timer");
    }

    private static void TestResponsiveWrapPanelLayout()
    {
        using var panel = new ResponsiveWrapPanel
        {
            Size = new Size(260, 1),
            Padding = new Padding(14, 10, 14, 10),
            MinimumItemWidth = 220,
            ItemHeight = 30,
            MaximumColumns = 2,
            HorizontalSpacing = 12,
            VerticalSpacing = 6
        };
        panel.Controls.AddRange(
        [
            new Label(),
            new Label(),
            new Label(),
            new Label()
        ]);
        panel.PerformLayout();

        Equal(158, panel.Height, "Responsive options narrow height");
        Check(
            panel.Controls.Cast<Control>().All(control =>
                control.Right <= panel.ClientSize.Width - panel.Padding.Right &&
                control.Bottom <= panel.ClientSize.Height - panel.Padding.Bottom),
            "Responsive options remain inside narrow bounds");

        panel.Width = 640;
        panel.PerformLayout();
        Equal(86, panel.Height, "Responsive options wide height");
        Equal(
            panel.Controls[0].Top,
            panel.Controls[1].Top,
            "Responsive options use two wide columns");
    }

    private static void TestEditorPaletteContrast()
    {
        var light = ThemePaletteService.Apply(AppThemeMode.Light);
        Check(
            ColorDistance(light.EditorCanvas, light.Canvas) >= 18,
            "Light editor canvas has visible contrast");
        Check(
            ColorDistance(light.EditorSurface, light.Surface) >= 18,
            "Light editor surface has visible contrast");
        Check(
            ColorDistance(light.EditorChrome, light.Surface) >= 18,
            "Light editor chrome has visible contrast");
        Check(
            ColorDistance(light.EditorBorder, light.Border) >= 18,
            "Light editor border has visible contrast");

        var dark = ThemePaletteService.Apply(AppThemeMode.Dark);
        Check(
            ColorDistance(dark.EditorCanvas, dark.Canvas) >= 18,
            "Dark editor canvas has visible contrast");
        Check(
            ColorDistance(dark.EditorSurface, dark.Surface) >= 18,
            "Dark editor surface has visible contrast");
        Check(
            ColorDistance(dark.EditorChrome, dark.Surface) >= 18,
            "Dark editor chrome has visible contrast");
        Check(
            ColorDistance(dark.EditorBorder, dark.Border) >= 18,
            "Dark editor border has visible contrast");

        ThemePaletteService.Apply(AppThemeMode.System);
    }

    private static void TestOtpGenerationValidation()
    {
        var entry = new AuthenticatorEntry
        {
            Name = string.Empty,
            Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            Kind = AuthenticatorKind.Totp,
            Digits = 6,
            Period = 30
        };
        OtpService.ValidateForCodeGeneration(entry);
        Check(true, "OTP generation validation does not require an account name");

        entry.Name = "Custom parameters";
        entry.Digits = 4;
        entry.Period = 1;
        entry.ApplyProviderDefaults();
        Equal(4, entry.Digits, "Generic provider defaults preserve valid 4-digit codes");
        Equal(1, entry.Period, "Generic provider defaults preserve valid 1-second periods");
        var parsed = OtpAuthUriService.ParseSingle(OtpAuthUriService.Build(entry));
        Equal(4, parsed.Digits, "OTP URI round-trip preserves valid custom digits");
        Equal(1, parsed.Period, "OTP URI round-trip preserves valid custom periods");

        entry.Kind = AuthenticatorKind.Hotp;
        entry.Digits = 4;
        entry.ApplyProviderDefaults();
        Equal(4, entry.Digits, "HOTP defaults preserve valid 4-digit codes");

        entry.Secret = "JBSWY3DP";
        var shortSecretRejected = false;
        try
        {
            OtpService.ValidateForCodeGeneration(entry);
        }
        catch (FormatException)
        {
            shortSecretRejected = true;
        }

        Check(shortSecretRejected, "OTP generation validation rejects short secrets");
    }

    private static void TestDefaultTheme()
    {
        var settings = new AppSettings();
        Equal(AppThemeMode.Dark, settings.Theme, "New settings default to dark theme");
        Equal(
            0,
            settings.ClipboardClearSeconds,
            "New settings leave copied codes on the clipboard by default");
        Check(!settings.AlwaysOnTop, "New settings do not default to always on top");
        Check(
            !settings.RememberWindowLayout,
            "New settings do not remember the window layout by default");
        var dark = ThemePaletteService.Apply(AppThemeMode.Dark);
        Check(dark.Dark, "Dark theme palette is active");
        Check(
            ThemePaletteService.AdaptAccent(Color.FromArgb(91, 83, 255)) !=
            Color.FromArgb(91, 83, 255),
            "Dark theme raises accent visibility");
    }

    private static void TestDeleteEntryMarshalsToUiThread(string testRoot)
    {
        var previousCrossThreadCheck = Control.CheckForIllegalCrossThreadCalls;
        Control.CheckForIllegalCrossThreadCalls = true;
        try
        {
            var vaultPath = Path.Combine(testRoot, "delete-entry-thread-vault.json");
            using var vault = new VaultService(vaultPath);
            var payload = vault.Open();
            payload.Settings.AutoLockMinutes = 0;
            payload.Settings.MinimizeToTray = false;
            payload.Settings.CloseToTray = false;
            var entry = SampleEntry();
            payload.Entries.Add(entry);
            vault.Save(payload);

            using var form = new MainForm(vault, payload, startMinimized: false)
            {
                ShowInTaskbar = false,
                Opacity = 0,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-32_000, -32_000)
            };
            form.Show();
            Application.DoEvents();

            var deleteEntry = typeof(MainForm).GetMethod(
                "DeleteEntry",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Check(deleteEntry is not null, "Delete entry action is available");

            var deletion = Task.Run(() => deleteEntry!.Invoke(form, [entry]));
            var deadline = Environment.TickCount64 + 5_000;
            while (!deletion.IsCompleted && Environment.TickCount64 < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }

            Check(deletion.IsCompleted, "Delete entry returns from the UI thread");
            deletion.GetAwaiter().GetResult();
            Application.DoEvents();

            Check(
                payload.Entries.All(item => item.Id != entry.Id),
                "Delete entry removes the payload item from a background confirmation callback");
            var cardFlow = (FlowLayoutPanel)typeof(MainForm)
                .GetField("_cardFlow", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(form)!;
            Equal(
                0,
                cardFlow.Controls.OfType<AuthenticatorCard>().Count(),
                "Delete entry rebuilds cards on the UI thread");

            using var reopenedVault = new VaultService(vaultPath);
            var persisted = reopenedVault.Open();
            Check(
                persisted.Entries.All(item => item.Id != entry.Id),
                "Delete entry persists the removal from a background confirmation callback");
        }
        finally
        {
            Control.CheckForIllegalCrossThreadCalls = previousCrossThreadCheck;
        }
    }

    private static void TestWinAuthImportPreservesLocalAlwaysOnTop(string testRoot)
    {
        using var vault = new VaultService(Path.Combine(testRoot, "import-settings-vault.json"));
        var payload = vault.Open();
        payload.Settings.AlwaysOnTop = false;
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;

        using var form = new MainForm(vault, payload, startMinimized: false);
        var applyImportedSettings = typeof(MainForm).GetMethod(
            "ApplyImportedWinAuthSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(applyImportedSettings is not null, "WinAuth settings import action is available");
        applyImportedSettings!.Invoke(
            form,
            [
                new AppSettings
                {
                    AlwaysOnTop = true,
                    MinimizeToTray = true,
                    StartWithWindows = false
                }
            ]);

        Check(
            !payload.Settings.AlwaysOnTop,
            "WinAuth import does not enable the local always-on-top preference");
        Check(!form.TopMost, "WinAuth import does not force the main window on top");
    }

    private static void TestAlwaysOnTopControlsStaySynchronized(string testRoot)
    {
        using var vault = new VaultService(Path.Combine(testRoot, "topmost-controls-vault.json"));
        var payload = vault.Open();
        payload.Settings.AlwaysOnTop = false;
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;

        using var form = new MainForm(vault, payload, startMinimized: false);
        var buildSettings = typeof(MainForm).GetMethod(
            "BuildSettings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(buildSettings is not null, "Settings builder is available");
        buildSettings!.Invoke(form, null);

        var headerCheckBox = (AntdUI.Checkbox)typeof(MainForm)
            .GetField("_topMostCheckBox", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        var settingsSwitch = (AntdUI.Switch)typeof(MainForm)
            .GetField("_settingsTopMostSwitch", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;

        Check(!headerCheckBox.Checked, "Header always-on-top checkbox defaults to off");
        Check(!settingsSwitch.Checked, "Settings always-on-top switch defaults to off");
        Check(!form.TopMost, "Main window defaults to normal z-order");

        headerCheckBox.Checked = true;
        Application.DoEvents();
        Check(payload.Settings.AlwaysOnTop, "Header checkbox updates the saved preference");
        Check(settingsSwitch.Checked, "Header checkbox updates the settings switch");
        Check(form.TopMost, "Header checkbox immediately enables always on top");

        settingsSwitch.Checked = false;
        Application.DoEvents();
        Check(!payload.Settings.AlwaysOnTop, "Settings switch clears the saved preference");
        Check(!headerCheckBox.Checked, "Settings switch updates the header checkbox");
        Check(!form.TopMost, "Settings switch immediately disables always on top");

        var lockVault = typeof(MainForm).GetMethod(
            "LockVault",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var unlockVault = typeof(MainForm).GetMethod(
            "UnlockVault",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(lockVault is not null && unlockVault is not null, "Vault lock actions are available");

        headerCheckBox.Checked = true;
        lockVault!.Invoke(form, null);
        headerCheckBox.Checked = false;
        Check(!form.TopMost, "Locked window can be removed from always-on-top");
        Check(
            (bool)unlockVault!.Invoke(form, null)!,
            "Windows-protected vault unlocks after changing always on top");

        var unlockedPayload = (VaultPayload)typeof(MainForm)
            .GetField("_payload", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        Check(
            !unlockedPayload.Settings.AlwaysOnTop,
            "Always-on-top choice made while locked survives unlock");
        Check(!headerCheckBox.Checked, "Header checkbox stays synchronized after unlock");
        Check(!settingsSwitch.Checked, "Settings switch stays synchronized after unlock");
        Check(!form.TopMost, "Window z-order stays synchronized after unlock");
    }

    private static void TestHeaderActionButtonLayout(string testRoot, bool enlargedHeaderFont)
    {
        using var vault = new VaultService(Path.Combine(testRoot, "header-layout-vault.json"));
        var payload = vault.Open();
        payload.Settings.AutoLockMinutes = 0;
        payload.Settings.MinimizeToTray = false;
        payload.Settings.CloseToTray = false;

        using var form = new MainForm(vault, payload, startMinimized: false)
        {
            Size = new Size(1_080, 720),
            ShowInTaskbar = false,
            Opacity = 0,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32_000, -32_000)
        };
        form.Show();
        Application.DoEvents();
        int ScaleForWindow(int pixels) => WindowDpi.Convert(
            pixels, WindowDpi.LogicalDpi, form.DeviceDpi);
        int LogicalPixels(int pixels) => WindowDpi.Convert(
            pixels, form.DeviceDpi, WindowDpi.LogicalDpi);
        form.Size = new Size(ScaleForWindow(1_080), ScaleForWindow(720));
        Application.DoEvents();
        var layout = typeof(MainForm).GetMethod(
            "LayoutShell",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(layout is not null, "Main window layout action is available");
        layout!.Invoke(form, null);

        var titleBar = (AntdUI.PageHeader)typeof(MainForm)
            .GetField("_titleBar", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        var themeButton = (AntdUI.Button)typeof(MainForm)
            .GetField("_themeButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        var lockButton = (AntdUI.Button)typeof(MainForm)
            .GetField("_lockButton", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        var topMostCheckBox = (AntdUI.Checkbox)typeof(MainForm)
            .GetField("_topMostCheckBox", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(form)!;
        using var headerFont = new Font(
            topMostCheckBox.Font.FontFamily,
            enlargedHeaderFont ? 18F : topMostCheckBox.Font.Size,
            topMostCheckBox.Font.Style,
            topMostCheckBox.Font.Unit);
        topMostCheckBox.Font = headerFont;
        var refreshHeaderActions = typeof(MainForm).GetMethod(
            "RefreshHeaderActionsAfterShown",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(
            refreshHeaderActions is not null,
            "Header actions provide an initial-paint refresh");
        refreshHeaderActions!.Invoke(form, null);
        Application.DoEvents();

        Equal(DockStyle.None, themeButton.Dock, "Theme button does not fill the title bar");
        Equal(DockStyle.None, lockButton.Dock, "Lock button does not fill the title bar");
        Equal(
            new Size(20, 20),
            themeButton.IconSize,
            "Theme button uses a legible fixed icon size");
        Equal(
            new Size(20, 20),
            lockButton.IconSize,
            "Header action icons use a consistent fixed size");
        Equal(
            "SunOutlined",
            themeButton.IconSvg,
            "Theme button keeps the light-theme base icon");
        Equal(
            "MoonOutlined",
            themeButton.ToggleIconSvg,
            "Theme button provides a dark-theme toggle icon");
        Check(themeButton.Toggle, "Dark theme selects the moon toggle icon");
        var darkThemeIconBounds = RenderedInkBounds(themeButton);
        Check(
            darkThemeIconBounds.Width >= 16 && darkThemeIconBounds.Height >= 16,
            "Dark theme icon renders at a legible size");
        var toggleTheme = typeof(MainForm).GetMethod(
            "ToggleTheme",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Check(toggleTheme is not null, "Theme toggle action is available");
        toggleTheme!.Invoke(form, null);
        Equal(
            "SunOutlined",
            themeButton.IconSvg,
            "Light theme keeps the sun base icon");
        Check(!themeButton.Toggle, "Light theme selects the sun base icon");
        Equal(
            new Size(20, 20),
            themeButton.IconSize,
            "Light theme preserves the legible icon size");
        var lightThemeIconBounds = RenderedInkBounds(themeButton);
        Check(
            lightThemeIconBounds.Width >= 16 && lightThemeIconBounds.Height >= 16,
            "Light theme icon renders at a legible size");
        toggleTheme.Invoke(form, null);
        Check(themeButton.Toggle, "Dark theme restores the moon toggle icon");
        Equal(
            DockStyle.None,
            topMostCheckBox.Dock,
            "Always-on-top checkbox does not fill the title bar");
        Check(
            themeButton.Top > 0 && lockButton.Top > 0,
            "Header actions preserve the top window border");
        Check(
            themeButton.Bottom < titleBar.ClientSize.Height &&
            lockButton.Bottom < titleBar.ClientSize.Height &&
            topMostCheckBox.Bottom < titleBar.ClientSize.Height,
            "Header actions preserve the title bar divider");
        Equal(
            titleBar.DisplayRectangle.Right,
            lockButton.Right,
            "Header actions stop before the system buttons");
        Equal(lockButton.Left, themeButton.Right, "Header actions remain adjacent");
        Check(topMostCheckBox.Visible, "Always-on-top checkbox is visible at desktop width");
        Equal("窗口置顶", topMostCheckBox.Text, "Always-on-top checkbox has a visible label");
        Equal(
            themeButton.Left - ScaleForWindow(8),
            topMostCheckBox.Right,
            "Always-on-top checkbox is separated from header buttons");

        // The header reserves 464 logical pixels for its other content. The checkbox
        // needs its actual rendered width; fallback fonts can exceed the 96px minimum.
        var topMostBreakpoint = 464 + LogicalPixels(topMostCheckBox.Width);
        Check(
            topMostCheckBox.Width >= TextRenderer.MeasureText(
                topMostCheckBox.Text, topMostCheckBox.Font).Width,
            "Always-on-top checkbox fits its rendered label");
        if (enlargedHeaderFont)
        {
            Check(topMostBreakpoint > 560, "Larger labels require more header space");
        }

        var widths = new[]
        {
            700, topMostBreakpoint + 1, topMostBreakpoint, topMostBreakpoint - 1,
            560, 559, 460, 459, 360
        }.Distinct().OrderDescending();
        foreach (var width in widths)
        {
            form.Size = new Size(ScaleForWindow(width), ScaleForWindow(720));
            Application.DoEvents();
            layout.Invoke(form, null);

            Equal(
                LogicalPixels(titleBar.Width) >= topMostBreakpoint,
                topMostCheckBox.Visible,
                $"Always-on-top checkbox visibility follows available width at {width}");
            Equal(
                LogicalPixels(titleBar.Width) >= 460,
                themeButton.Visible,
                $"Theme button visibility follows available width at {width}");
            Equal(
                titleBar.DisplayRectangle.Right,
                lockButton.Right,
                $"Lock button stays before system buttons at {width}");
            if (topMostCheckBox.Visible)
            {
                Check(
                    topMostCheckBox.Left >= 0 &&
                    topMostCheckBox.Top > 0 &&
                    topMostCheckBox.Bottom < titleBar.ClientSize.Height,
                    $"Always-on-top checkbox stays inside the title bar at {width}");
                Equal(
                    (themeButton.Visible ? themeButton.Left : lockButton.Left) - ScaleForWindow(8),
                    topMostCheckBox.Right,
                    $"Always-on-top checkbox preserves action spacing at {width}");
            }
        }
    }

    private static Rectangle RenderedInkBounds(Control control)
    {
        using var bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, control.ClientRectangle);
        var background = bitmap.GetPixel(0, 0);
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = -1;
        var bottom = -1;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (ColorDistance(bitmap.GetPixel(x, y), background) < 18)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        return right >= left && bottom >= top
            ? Rectangle.FromLTRB(left, top, right + 1, bottom + 1)
            : Rectangle.Empty;
    }

    private static IEnumerable<Control> DescendantControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in DescendantControls(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool IsInside(Control child, Control ancestor)
    {
        var childBounds = child.RectangleToScreen(child.ClientRectangle);
        var ancestorBounds = ancestor.RectangleToScreen(ancestor.ClientRectangle);
        return ancestorBounds.Contains(childBounds);
    }

    private static void CheckButtonsInside(
        Form dialog,
        IEnumerable<string> buttonTexts,
        string name)
    {
        var expected = buttonTexts.ToArray();
        var buttons = DescendantControls(dialog)
            .OfType<AntdUI.Button>()
            .Where(button => expected.Contains(button.Text, StringComparer.Ordinal))
            .ToArray();
        Equal(expected.Length, buttons.Length, $"{name} exposes all actions");
        Check(
            buttons.All(button =>
                button.Visible &&
                button.Width > 0 &&
                button.Height > 0 &&
                IsInside(button, dialog)),
            $"{name} keeps all actions inside minimum bounds");
    }

    private static double ColorDistance(Color left, Color right)
    {
        var red = left.R - right.R;
        var green = left.G - right.G;
        var blue = left.B - right.B;
        return Math.Sqrt((red * red) + (green * green) + (blue * blue));
    }

    private static void CheckButtonInteractionContrast(
        AntdUI.Button button,
        string name)
    {
        Check(
            button.BackHover.HasValue &&
            button.ForeHover.HasValue &&
            ContrastRatio(button.BackHover.Value, button.ForeHover.Value) >= 4.5D,
            $"{name} has readable hover text");
        Check(
            button.BackActive.HasValue &&
            button.ForeActive.HasValue &&
            ContrastRatio(button.BackActive.Value, button.ForeActive.Value) >= 4.5D,
            $"{name} has readable pressed text");
        Check(
            button.ToggleBackHover.HasValue &&
            button.ToggleForeHover.HasValue &&
            ContrastRatio(
                button.ToggleBackHover.Value,
                button.ToggleForeHover.Value) >= 4.5D,
            $"{name} has readable selected-hover text");
        Check(
            !button.AutoToggle && !button.Toggle,
            $"{name} does not enter an unintended selected state");
    }

    private static double ContrastRatio(Color left, Color right)
    {
        static double RelativeLuminance(Color color)
        {
            static double Channel(byte value)
            {
                var component = value / 255D;
                return component <= 0.04045D
                    ? component / 12.92D
                    : Math.Pow((component + 0.055D) / 1.055D, 2.4D);
            }

            return (0.2126D * Channel(color.R)) +
                   (0.7152D * Channel(color.G)) +
                   (0.0722D * Channel(color.B));
        }

        var leftLuminance = RelativeLuminance(left);
        var rightLuminance = RelativeLuminance(right);
        return (Math.Max(leftLuminance, rightLuminance) + 0.05D) /
               (Math.Min(leftLuminance, rightLuminance) + 0.05D);
    }

    private static void TouchVault(string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, StorageLocationService.VaultFileName),
            "{}",
            new UTF8Encoding(false));
    }

    private static void CreatePortableVault(string directory, string entryName)
    {
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, StorageLocationService.VaultFileName);
        using var vault = new VaultService(filePath);
        var payload = vault.Open();
        var entry = SampleEntry();
        entry.Name = entryName;
        payload.Entries.Add(entry);
        vault.Save(payload);
    }

    private static void WriteStorageLocator(string filePath, string dataDirectory)
    {
        Directory.CreateDirectory(
            Path.GetDirectoryName(filePath) ??
            throw new InvalidOperationException("Storage locator is missing a parent directory."));
        var json = JsonSerializer.Serialize(new
        {
            Version = 1,
            DataDirectory = dataDirectory
        });
        File.WriteAllText(filePath, json, new UTF8Encoding(false));
    }

    private static string ReadStoredDataDirectory(string filePath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(filePath, Encoding.UTF8));
        return document.RootElement.GetProperty("DataDirectory").GetString()
            ?? throw new InvalidDataException("Storage locator data directory is missing.");
    }

    private static AuthenticatorEntry SampleEntry()
    {
        return new AuthenticatorEntry
        {
            Name = "alice@example.com",
            Issuer = "Example Cloud",
            Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            Kind = AuthenticatorKind.Totp,
            Algorithm = OtpAlgorithm.Sha256,
            Digits = 8,
            Period = 45,
            Favorite = true,
            SortOrder = 0
        };
    }

    private static string IconFingerprint(Icon icon)
    {
        using var bitmap = icon.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return Convert.ToHexString(SHA256.HashData(stream.ToArray()));
    }

    private static void Check(bool condition, string name)
    {
        Assert.True(condition, $"Check failed: {name}");
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        Assert.True(
            EqualityComparer<T>.Default.Equals(expected, actual),
            $"Check failed: {name}. Expected '{expected}', actual '{actual}'.");
    }

    private static void WriteProtoBytes(List<byte> output, int field, byte[] value)
    {
        WriteVarint(output, (ulong)(field << 3 | 2));
        WriteVarint(output, (ulong)value.Length);
        output.AddRange(value);
    }

    private static void WriteProtoVarint(List<byte> output, int field, ulong value)
    {
        WriteVarint(output, (ulong)(field << 3));
        WriteVarint(output, value);
    }

    private static void WriteVarint(List<byte> output, ulong value)
    {
        while (value >= 0x80)
        {
            output.Add((byte)(value | 0x80));
            value >>= 7;
        }

        output.Add((byte)value);
    }

    private sealed class MainFormDesignerProbe : MainFormVisualBase
    {
    }

    private sealed class AboutLicensesDialogDesignerProbe :
        AboutLicensesDialogVisualBase
    {
    }

    private sealed class EntryEditorFormDesignerProbe :
        EntryEditorFormVisualBase
    {
    }

    private sealed class PasswordDialogDesignerProbe :
        PasswordDialogVisualBase
    {
    }

    private sealed class QrExportDialogDesignerProbe :
        QrExportDialogVisualBase
    {
    }

    private sealed class SensitiveValueDialogDesignerProbe :
        SensitiveValueDialogVisualBase
    {
    }

    private sealed class TextImportDialogDesignerProbe :
        TextImportDialogVisualBase
    {
    }

    private sealed record DesignerDialogCase(
        Type RuntimeType,
        Type VisualBaseType,
        Func<Form> CreatePreview,
        int MinimumControlCount,
        string[] RequiredLocalizedTextKeys);

    private sealed class FakeWindowsAccountProtector : IWindowsAccountProtector
    {
        private const int IdentityMarkerSize = 32;
        private readonly byte[] _identityMarker;

        public FakeWindowsAccountProtector(string identity)
        {
            _identityMarker = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        }

        public int ProtectCalls { get; private set; }

        public int UnprotectCalls { get; private set; }

        public byte[] Protect(byte[] plaintext)
        {
            ArgumentNullException.ThrowIfNull(plaintext);
            ProtectCalls++;

            var protectedData = new byte[IdentityMarkerSize + plaintext.Length];
            _identityMarker.CopyTo(protectedData, 0);
            plaintext.CopyTo(protectedData, IdentityMarkerSize);
            return protectedData;
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            ArgumentNullException.ThrowIfNull(protectedData);
            UnprotectCalls++;

            if (protectedData.Length < IdentityMarkerSize ||
                !CryptographicOperations.FixedTimeEquals(
                    protectedData.AsSpan(0, IdentityMarkerSize),
                    _identityMarker))
            {
                throw new CryptographicException(
                    "The protected data belongs to another simulated Windows identity.");
            }

            return protectedData[IdentityMarkerSize..];
        }
    }
}
