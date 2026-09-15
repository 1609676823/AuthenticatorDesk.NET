using System.Globalization;
using System.Text;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.Services;

public static class OtpAuthUriService
{
    public static IReadOnlyList<AuthenticatorEntry> Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException(
                L.Get("service.otpUri.error.uriRequired"));
        }

        var lines = value
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#'))
            .ToArray();
        if (lines.Length > 1)
        {
            var entries = new List<AuthenticatorEntry>();
            foreach (var line in lines)
            {
                entries.AddRange(Parse(line));
            }

            return entries;
        }

        value = (lines.Length == 1 ? lines[0] : value).Trim();
        if (value.StartsWith("otpauth-migration://", StringComparison.OrdinalIgnoreCase))
        {
            return ParseMigrationUri(value);
        }

        return [ParseSingle(value)];
    }

    public static AuthenticatorEntry ParseSingle(string value)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("otpauth", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException(
                L.Get("service.otpUri.error.invalidUri"));
        }

        var type = uri.Host.ToLowerInvariant();
        if (type is not ("totp" or "hotp"))
        {
            throw new FormatException(
                L.Format("service.otpUri.error.unsupportedType", uri.Host));
        }

        var query = ParseQuery(uri.Query);
        if (!query.TryGetValue("secret", out var secret) ||
            !Base32Encoding.TryDecode(secret, out _))
        {
            throw new FormatException(
                L.Get("service.otpUri.error.invalidSecretParameter"));
        }

        var label = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')).Trim();
        query.TryGetValue("issuer", out var issuer);
        issuer = issuer?.Trim() ?? string.Empty;

        var name = label;
        var colonIndex = label.IndexOf(':');
        if (colonIndex >= 0)
        {
            if (string.IsNullOrWhiteSpace(issuer))
            {
                issuer = label[..colonIndex].Trim();
            }

            name = label[(colonIndex + 1)..].Trim();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = string.IsNullOrWhiteSpace(issuer)
                ? string.Empty
                : issuer;
        }

        var kind = type == "hotp" ? AuthenticatorKind.Hotp : DetectKind(issuer, query);
        var entry = new AuthenticatorEntry
        {
            Name = name,
            Issuer = issuer,
            Secret = Base32Encoding.Normalize(secret),
            Kind = kind,
            Algorithm = ParseAlgorithm(query.GetValueOrDefault("algorithm")),
            Digits = ParseInt(query.GetValueOrDefault("digits"), kind == AuthenticatorKind.Steam ? 5 : 6, 4, 10),
            Period = ParseInt(query.GetValueOrDefault("period"), 30, 1, 3600),
            Counter = ParseLong(query.GetValueOrDefault("counter"), 0),
            TimeOffsetSeconds = ParseTimeOffset(
                query.GetValueOrDefault("timeoffset") ??
                query.GetValueOrDefault("offset")),
            Serial = query.GetValueOrDefault("serial"),
            DeviceId = query.GetValueOrDefault("deviceid")
        };

        if (query.TryGetValue("data", out var providerData) && !string.IsNullOrWhiteSpace(providerData))
        {
            entry.ProviderData["legacyData"] = providerData;
        }

        entry.ApplyProviderDefaults();
        OtpService.ValidateForCodeGeneration(entry);
        return entry;
    }

    public static string Build(AuthenticatorEntry entry, bool includeProviderData = false)
    {
        ArgumentNullException.ThrowIfNull(entry);
        OtpService.ValidateForCodeGeneration(entry);

        var type = entry.Kind == AuthenticatorKind.Hotp ? "hotp" : "totp";
        var issuer = entry.DisplayIssuer;
        var label = string.IsNullOrWhiteSpace(issuer)
            ? entry.DisplayName
            : $"{issuer}:{entry.DisplayName}";

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("secret", Base32Encoding.Normalize(entry.Secret)),
            new("issuer", issuer),
            new("algorithm", AlgorithmName(entry.Algorithm)),
            new("digits", entry.Digits.ToString(CultureInfo.InvariantCulture))
        };
        var provider = ProviderName(entry.Kind);
        if (provider is not null)
        {
            parameters.Add(new KeyValuePair<string, string>("provider", provider));
        }

        if (entry.Kind == AuthenticatorKind.Hotp)
        {
            parameters.Add(new KeyValuePair<string, string>(
                "counter",
                entry.Counter.ToString(CultureInfo.InvariantCulture)));
        }
        else
        {
            parameters.Add(new KeyValuePair<string, string>(
                "period",
                entry.Period.ToString(CultureInfo.InvariantCulture)));
        }

        if (entry.Kind == AuthenticatorKind.Steam)
        {
            parameters.Add(new KeyValuePair<string, string>("encoder", "steam"));
        }

        if (!string.IsNullOrWhiteSpace(entry.Serial))
        {
            parameters.Add(new KeyValuePair<string, string>("serial", entry.Serial.Replace("-", string.Empty)));
        }

        if (includeProviderData && !string.IsNullOrWhiteSpace(entry.DeviceId))
        {
            parameters.Add(new KeyValuePair<string, string>("deviceid", entry.DeviceId));
        }

        if (includeProviderData && entry.TimeOffsetSeconds != 0)
        {
            parameters.Add(new KeyValuePair<string, string>(
                "timeoffset",
                entry.TimeOffsetSeconds.ToString(CultureInfo.InvariantCulture)));
        }

        var query = string.Join(
            "&",
            parameters
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => $"{Escape(pair.Key)}={Escape(pair.Value)}"));

        return $"otpauth://{type}/{EscapePath(label)}?{query}";
    }

    public static void Validate(AuthenticatorEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            throw new FormatException(
                L.Get("service.otpUri.error.accountNameRequired"));
        }

        OtpService.ValidateForCodeGeneration(entry);
    }

    private static IReadOnlyList<AuthenticatorEntry> ParseMigrationUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new FormatException(
                L.Get("service.otpUri.error.invalidMigrationUri"));
        }

        var query = ParseQuery(uri.Query);
        if (!query.TryGetValue("data", out var encoded) || string.IsNullOrWhiteSpace(encoded))
        {
            throw new FormatException(
                L.Get("service.otpUri.error.missingMigrationData"));
        }

        byte[] payload;
        try
        {
            encoded = encoded
                .Replace(' ', '+')
                .Replace('-', '+')
                .Replace('_', '/');
            encoded = encoded.PadRight(
                encoded.Length + ((4 - encoded.Length % 4) % 4),
                '=');
            payload = Convert.FromBase64String(encoded);
        }
        catch (FormatException exception)
        {
            throw new FormatException(
                L.Get("service.otpUri.error.invalidMigrationBase64"),
                exception);
        }

        var entries = new List<AuthenticatorEntry>();
        try
        {
            foreach (var field in ProtoReader.Read(payload))
            {
                if (field.Number != 1 || field.WireType != 2)
                {
                    continue;
                }

                var entry = ParseMigrationEntry(field.Bytes);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
        }
        catch (OverflowException exception)
        {
            throw new FormatException(
                L.Get("service.otpUri.error.migrationNumberOutOfRange"),
                exception);
        }

        if (entries.Count == 0)
        {
            throw new FormatException(
                L.Get("service.otpUri.error.noImportableAccounts"));
        }

        return entries;
    }

    private static AuthenticatorEntry? ParseMigrationEntry(ReadOnlyMemory<byte> payload)
    {
        byte[]? secret = null;
        var name = string.Empty;
        var issuer = string.Empty;
        var algorithm = OtpAlgorithm.Sha1;
        var digits = 6;
        var kind = AuthenticatorKind.Totp;
        long counter = 0;

        foreach (var field in ProtoReader.Read(payload))
        {
            switch (field.Number)
            {
                case 1 when field.WireType == 2:
                    secret = field.Bytes.ToArray();
                    break;
                case 2 when field.WireType == 2:
                    name = Encoding.UTF8.GetString(field.Bytes.Span);
                    break;
                case 3 when field.WireType == 2:
                    issuer = Encoding.UTF8.GetString(field.Bytes.Span);
                    break;
                case 4 when field.WireType == 0:
                    algorithm = field.Varint switch
                    {
                        2 => OtpAlgorithm.Sha256,
                        3 => OtpAlgorithm.Sha512,
                        _ => OtpAlgorithm.Sha1
                    };
                    break;
                case 5 when field.WireType == 0:
                    digits = field.Varint == 2 ? 8 : 6;
                    break;
                case 6 when field.WireType == 0:
                    kind = field.Varint == 1 ? AuthenticatorKind.Hotp : AuthenticatorKind.Totp;
                    break;
                case 7 when field.WireType == 0:
                    counter = checked((long)field.Varint);
                    break;
            }
        }

        if (secret is null || secret.Length == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = string.IsNullOrWhiteSpace(issuer)
                ? string.Empty
                : issuer;
        }

        var entry = new AuthenticatorEntry
        {
            Name = name.Trim(),
            Issuer = issuer.Trim(),
            Secret = Base32Encoding.Encode(secret),
            Kind = kind,
            Algorithm = algorithm,
            Digits = digits,
            Period = 30,
            Counter = counter
        };

        entry.Kind = DetectKind(entry.Issuer, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        if (kind == AuthenticatorKind.Hotp)
        {
            entry.Kind = AuthenticatorKind.Hotp;
        }

        entry.ApplyProviderDefaults();
        OtpService.ValidateForCodeGeneration(entry);
        return entry;
    }

    private static AuthenticatorKind DetectKind(
        string? issuer,
        IReadOnlyDictionary<string, string> query)
    {
        if (TryParseProviderKind(query.GetValueOrDefault("provider"), out var explicitKind))
        {
            return explicitKind;
        }

        issuer ??= string.Empty;
        if (issuer.Contains("steam", StringComparison.OrdinalIgnoreCase) ||
            query.GetValueOrDefault("encoder")?.Equals("steam", StringComparison.OrdinalIgnoreCase) == true)
        {
            return AuthenticatorKind.Steam;
        }

        if (issuer.Contains("battle", StringComparison.OrdinalIgnoreCase) ||
            query.ContainsKey("serial") && issuer.Contains("blizzard", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticatorKind.BattleNet;
        }

        if (issuer.Contains("trion", StringComparison.OrdinalIgnoreCase) ||
            issuer.Contains("glyph", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticatorKind.Trion;
        }

        if (issuer.Contains("microsoft", StringComparison.OrdinalIgnoreCase) ||
            issuer.Contains("azure", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticatorKind.Microsoft;
        }

        if (issuer.Contains("okta", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticatorKind.OktaVerify;
        }

        if (issuer.Contains("guild wars", StringComparison.OrdinalIgnoreCase) ||
            issuer.Contains("arena.net", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticatorKind.GuildWars;
        }

        if (issuer.Contains("google", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticatorKind.Google;
        }

        return AuthenticatorKind.Totp;
    }

    private static string? ProviderName(AuthenticatorKind kind)
    {
        return kind switch
        {
            AuthenticatorKind.Google => "google",
            AuthenticatorKind.Microsoft => "microsoft",
            AuthenticatorKind.OktaVerify => "okta",
            AuthenticatorKind.GuildWars => "guildwars",
            AuthenticatorKind.Steam => "steam",
            AuthenticatorKind.BattleNet => "battlenet",
            AuthenticatorKind.Trion => "trion",
            _ => null
        };
    }

    private static bool TryParseProviderKind(string? value, out AuthenticatorKind kind)
    {
        kind = value?.Trim().ToLowerInvariant() switch
        {
            "google" => AuthenticatorKind.Google,
            "microsoft" => AuthenticatorKind.Microsoft,
            "okta" or "oktaverify" => AuthenticatorKind.OktaVerify,
            "guildwars" or "guildwars2" => AuthenticatorKind.GuildWars,
            "steam" => AuthenticatorKind.Steam,
            "battlenet" or "battle.net" or "blizzard" => AuthenticatorKind.BattleNet,
            "trion" or "glyph" => AuthenticatorKind.Trion,
            _ => AuthenticatorKind.Totp
        };
        return !string.IsNullOrWhiteSpace(value) &&
               kind != AuthenticatorKind.Totp;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            var key = Unescape(pieces[0]);
            var value = pieces.Length == 2 ? Unescape(pieces[1]) : string.Empty;
            values[key] = value;
        }

        return values;
    }

    private static string Unescape(string value)
    {
        return Uri.UnescapeDataString(value.Replace("+", " ", StringComparison.Ordinal));
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);

    private static string EscapePath(string value)
    {
        return string.Join("/", value.Split('/').Select(Uri.EscapeDataString));
    }

    private static OtpAlgorithm ParseAlgorithm(string? value)
    {
        return value?.Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant() switch
        {
            "SHA256" => OtpAlgorithm.Sha256,
            "SHA512" => OtpAlgorithm.Sha512,
            _ => OtpAlgorithm.Sha1
        };
    }

    private static string AlgorithmName(OtpAlgorithm algorithm)
    {
        return algorithm switch
        {
            OtpAlgorithm.Sha256 => "SHA256",
            OtpAlgorithm.Sha512 => "SHA512",
            _ => "SHA1"
        };
    }

    private static int ParseInt(string? value, int fallback, int minimum, int maximum)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, minimum, maximum)
            : fallback;
    }

    private static long ParseLong(string? value, long fallback)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Max(0, parsed)
            : fallback;
    }

    private static long ParseTimeOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new FormatException(
                L.Get("service.otpUri.error.invalidTimeOffset"));
        }

        return parsed;
    }

    private readonly record struct ProtoField(
        int Number,
        int WireType,
        ulong Varint,
        ReadOnlyMemory<byte> Bytes);

    private static class ProtoReader
    {
        public static IEnumerable<ProtoField> Read(ReadOnlyMemory<byte> payload)
        {
            var offset = 0;
            while (offset < payload.Length)
            {
                var key = ReadVarint(payload.Span, ref offset);
                var fieldNumber = checked((int)(key >> 3));
                var wireType = checked((int)(key & 0x07));
                if (fieldNumber <= 0)
                {
                    throw new FormatException(
                        L.Get("service.otpUri.error.invalidProtobufField"));
                }

                switch (wireType)
                {
                    case 0:
                        yield return new ProtoField(
                            fieldNumber,
                            wireType,
                            ReadVarint(payload.Span, ref offset),
                            ReadOnlyMemory<byte>.Empty);
                        break;
                    case 1:
                        EnsureAvailable(payload.Length, offset, 8);
                        yield return new ProtoField(
                            fieldNumber,
                            wireType,
                            0,
                            payload.Slice(offset, 8));
                        offset += 8;
                        break;
                    case 2:
                        var length = checked((int)ReadVarint(payload.Span, ref offset));
                        EnsureAvailable(payload.Length, offset, length);
                        yield return new ProtoField(
                            fieldNumber,
                            wireType,
                            0,
                            payload.Slice(offset, length));
                        offset += length;
                        break;
                    case 5:
                        EnsureAvailable(payload.Length, offset, 4);
                        yield return new ProtoField(
                            fieldNumber,
                            wireType,
                            0,
                            payload.Slice(offset, 4));
                        offset += 4;
                        break;
                    default:
                        throw new FormatException(
                            L.Format(
                                "service.otpUri.error.unsupportedProtobufWireType",
                                wireType));
                }
            }
        }

        private static ulong ReadVarint(ReadOnlySpan<byte> payload, ref int offset)
        {
            ulong value = 0;
            for (var shift = 0; shift < 64; shift += 7)
            {
                if (offset >= payload.Length)
                {
                    throw new FormatException(
                        L.Get("service.otpUri.error.unexpectedEnd"));
                }

                var current = payload[offset++];
                value |= (ulong)(current & 0x7f) << shift;
                if ((current & 0x80) == 0)
                {
                    return value;
                }
            }

            throw new FormatException(
                L.Get("service.otpUri.error.overlongProtobufVarint"));
        }

        private static void EnsureAvailable(int totalLength, int offset, int length)
        {
            if (length < 0 || offset < 0 || offset > totalLength - length)
            {
                throw new FormatException(
                    L.Get("service.otpUri.error.invalidFieldLength"));
            }
        }
    }
}
