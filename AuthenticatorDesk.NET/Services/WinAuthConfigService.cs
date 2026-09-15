/*
 * This file is part of AuthenticatorDesk.
 * Modifications Copyright (C) 2026 AuthenticatorDesk contributors.
 *
 * Adapted and substantially modified for AuthenticatorDesk on 2026-07-29
 * from portions of WinAuth WinAuthConfig.cs, WinAuthAuthenticator.cs,
 * HotKey.cs, Authenticator.cs, HOTPAuthenticator.cs,
 * SteamAuthenticator.cs, BattleNetAuthenticator.cs, and
 * TrionAuthenticator.cs.
 * Copyright (C) 2011 Colin Mackie.
 * Copyright (C) 2013 Colin Mackie.
 * Copyright (C) 2015 Colin Mackie.
 * Upstream revision:
 * https://github.com/winauth/winauth/tree/c57132f57b8a90e5219c628deb591f4603f27cb0
 *
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * AuthenticatorDesk is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by the
 * Free Software Foundation, either version 3 of the License, or (at your
 * option) any later version.
 *
 * AuthenticatorDesk is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See LICENSE.txt.
 */

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.Services;

public sealed record WinAuthImportInfo(
    bool RequiresPassword,
    bool UsesWindowsProtection,
    bool UsesYubiKey,
    bool RequiresConfigurationPassword,
    bool HasPerAuthenticatorPasswords);

public sealed record WinAuthImportResult(
    IReadOnlyList<AuthenticatorEntry> Entries,
    AppSettings Settings);

public static class WinAuthConfigService
{
    public static WinAuthImportInfo Inspect(string filePath)
    {
        var document = LoadDocument(filePath);
        var protections = document
            .DescendantsAndSelf()
            .Select(element => WinAuthCryptoService.DecodeProtection(
                element.Attribute("encrypted")?.Value))
            .Aggregate(WinAuthProtection.None, (current, value) => current | value);
        var root = document.Root;
        var configurationProtection = root is null
            ? WinAuthProtection.None
            : new[] { root }
                .Concat(root.Elements().Where(element => NameIs(element, "data")))
                .Select(element => WinAuthCryptoService.DecodeProtection(
                    element.Attribute("encrypted")?.Value))
                .Aggregate(WinAuthProtection.None, (current, value) => current | value);
        var perAuthenticatorProtection = document
            .Descendants()
            .Where(element =>
                NameIs(element, "authenticatordata") ||
                NameIs(element, "authenticator"))
            .Select(element => WinAuthCryptoService.DecodeProtection(
                element.Attribute("encrypted")?.Value))
            .Aggregate(WinAuthProtection.None, (current, value) => current | value);

        return new WinAuthImportInfo(
            protections.HasFlag(WinAuthProtection.Password),
            protections.HasFlag(WinAuthProtection.WindowsUser) ||
            protections.HasFlag(WinAuthProtection.WindowsMachine),
            protections.HasFlag(WinAuthProtection.YubiKeySlot1) ||
            protections.HasFlag(WinAuthProtection.YubiKeySlot2),
            configurationProtection.HasFlag(WinAuthProtection.Password),
            perAuthenticatorProtection.HasFlag(WinAuthProtection.Password));
    }

    public static WinAuthImportResult Import(
        string filePath,
        string? password = null,
        Func<string, string?>? entryPasswordProvider = null)
    {
        var document = LoadDocument(filePath);
        var root = document.Root;
        if (root is null || !NameIs(root, "WinAuth"))
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthConfig.error.notWinAuthXml"));
        }

        root = DecryptRootIfNeeded(root, password);
        var settings = ParseSettings(root);
        var containers = new List<XElement> { root };

        foreach (var dataElement in root.Elements().Where(element => NameIs(element, "data")))
        {
            var protection = WinAuthCryptoService.DecodeProtection(
                dataElement.Attribute("encrypted")?.Value);
            if (protection == WinAuthProtection.None)
            {
                containers.Add(dataElement);
                continue;
            }

            var plaintextHex = WinAuthCryptoService.DecryptSequence(
                dataElement.Value,
                protection,
                password);
            var decrypted = LoadDocument(Convert.FromHexString(plaintextHex));
            if (decrypted.Root is not null)
            {
                containers.Add(decrypted.Root);
            }
        }

        containers.AddRange(root.Elements().Where(element => NameIs(element, "config")));

        var entries = new List<AuthenticatorEntry>();
        foreach (var card in containers
                     .SelectMany(container => container.Elements())
                     .Where(element => NameIs(element, "WinAuthAuthenticator")))
        {
            entries.Add(ParseCard(
                card,
                password,
                entries.Count,
                entryPasswordProvider));
        }

        foreach (var legacy in containers
                     .SelectMany(container => container.Elements())
                     .Where(element => NameIs(element, "authenticator")))
        {
            entries.Add(ParseLegacyAuthenticator(
                legacy,
                password,
                entries.Count,
                entryPasswordProvider));
        }

        if (entries.Count == 0)
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthConfig.error.noAuthenticators"));
        }

        return new WinAuthImportResult(entries, settings);
    }

    public static void Export(
        string filePath,
        IEnumerable<AuthenticatorEntry> entries,
        AppSettings settings,
        string? password = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(settings);

        var cards = entries
            .OrderBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.CreatedAtUtc)
            .Select(CreateCard)
            .ToArray();

        var root = new XElement(
            "WinAuth",
            new XAttribute("version", "3.5"),
            new XElement("alwaysontop", settings.AlwaysOnTop),
            new XElement("usetrayicon", settings.MinimizeToTray || settings.CloseToTray),
            new XElement("notifyaction", "Notification"),
            new XElement("startwithwindows", settings.StartWithWindows),
            new XElement("autosize", false),
            new XElement("left", settings.WindowLeft),
            new XElement("top", settings.WindowTop),
            new XElement("width", settings.WindowWidth),
            new XElement("height", settings.WindowHeight));

        if (string.IsNullOrEmpty(password))
        {
            root.Add(cards);
        }
        else
        {
            var config = new XElement("config", cards);
            var plaintext = SerializeElement(config, includeBom: true);
            var encrypted = WinAuthCryptoService.EncryptSequence(
                Convert.ToHexString(plaintext),
                WinAuthProtection.Password,
                password);
            var checksum = Convert.ToHexString(SHA1.HashData(Convert.FromHexString(encrypted)));
            root.Add(new XElement(
                "data",
                new XAttribute("encrypted", "y"),
                new XAttribute("sha1", checksum),
                encrypted));
        }

        WriteAtomic(filePath, new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root));
    }

    private static XElement DecryptRootIfNeeded(XElement root, string? password)
    {
        var protection = WinAuthCryptoService.DecodeProtection(root.Attribute("encrypted")?.Value);
        if (protection == WinAuthProtection.None)
        {
            return root;
        }

        var plaintextHex = WinAuthCryptoService.DecryptSequence(root.Value, protection, password);
        var decrypted = LoadDocument(Convert.FromHexString(plaintextHex));
        if (decrypted.Root is null || !NameIs(decrypted.Root, "WinAuth"))
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthConfig.error.invalidDecryptedStructure"));
        }

        return decrypted.Root;
    }

    private static AuthenticatorEntry ParseCard(
        XElement card,
        string? password,
        int sortOrder,
        Func<string, string?>? entryPasswordProvider)
    {
        var typeName = card.Attribute("type")?.Value ?? string.Empty;
        var sourceName = ElementValue(card, "name")?.Trim();
        var promptName = string.IsNullOrWhiteSpace(sourceName)
            ? L.Format(
                "service.winAuthConfig.default.authenticatorName",
                sortOrder + 1)
            : sourceName;
        var data = card.Elements().FirstOrDefault(element => NameIs(element, "authenticatordata"))
                   ?? card.Elements().FirstOrDefault(element => NameIs(element, "authenticator"))
                   ?? throw new WinAuthCompatibilityException(
                       L.Get("service.winAuthConfig.error.missingKeyData"));

        data = DecryptAuthenticatorData(
            data,
            password,
            promptName,
            entryPasswordProvider);
        var entry = ParseAuthenticatorData(data, typeName, sortOrder);

        if (Guid.TryParse(card.Attribute("id")?.Value, out var id))
        {
            entry.Id = id;
        }

        entry.Name = sourceName ?? string.Empty;
        entry.AutoRefresh = ElementBool(card, "autorefresh", true);
        entry.CopyOnReveal = ElementBool(card, "copyoncode", false);
        entry.HideSerial = ElementBool(card, "hideserial", true);
        entry.CreatedAtUtc = ParseCreated(ElementValue(card, "created"));
        entry.ModifiedAtUtc = entry.CreatedAtUtc;

        var skin = ElementValue(card, "skin");
        if (!string.IsNullOrWhiteSpace(skin))
        {
            entry.ProviderData["winAuthSkin"] = skin;
        }

        var hotkey = card.Elements().FirstOrDefault(element => NameIs(element, "hotkey"));
        if (hotkey is not null)
        {
            ParseHotkey(hotkey, entry);
        }

        entry.ApplyProviderDefaults();
        OtpService.ValidateForCodeGeneration(entry);
        return entry;
    }

    private static AuthenticatorEntry ParseLegacyAuthenticator(
        XElement authenticator,
        string? password,
        int sortOrder,
        Func<string, string?>? entryPasswordProvider)
    {
        var typeName = authenticator.Attribute("type")?.Value ?? string.Empty;
        var data = DecryptAuthenticatorData(
            authenticator,
            password,
            L.Format(
                "service.winAuthConfig.default.authenticatorName",
                sortOrder + 1),
            entryPasswordProvider);
        var entry = ParseAuthenticatorData(data, typeName, sortOrder);
        entry.Name = entry.DisplayIssuer;
        entry.ApplyProviderDefaults();
        OtpService.ValidateForCodeGeneration(entry);
        return entry;
    }

    private static XElement DecryptAuthenticatorData(
        XElement data,
        string? password,
        string displayName,
        Func<string, string?>? entryPasswordProvider)
    {
        var protection = WinAuthCryptoService.DecodeProtection(data.Attribute("encrypted")?.Value);
        if (protection == WinAuthProtection.None)
        {
            return data;
        }

        string plaintextHex;
        try
        {
            plaintextHex = WinAuthCryptoService.DecryptSequence(
                data.Value,
                protection,
                password);
        }
        catch (Exception exception) when (
            exception is WinAuthCompatibilityException or WinAuthPasswordRequiredException &&
            protection.HasFlag(WinAuthProtection.Password) &&
            entryPasswordProvider is not null)
        {
            var entryPassword = entryPasswordProvider(displayName);
            if (entryPassword is null)
            {
                throw new OperationCanceledException(
                    L.Get("service.winAuthConfig.error.entryPasswordCanceled"));
            }

            plaintextHex = WinAuthCryptoService.DecryptSequence(
                data.Value,
                protection,
                entryPassword);
        }

        var decrypted = LoadDocument(Convert.FromHexString(plaintextHex));
        return decrypted.Root
               ?? throw new WinAuthCompatibilityException(
                   L.Get("service.winAuthConfig.error.emptyDecryptedAuthenticator"));
    }

    private static AuthenticatorEntry ParseAuthenticatorData(
        XElement data,
        string typeName,
        int sortOrder)
    {
        var secretData = ElementValue(data, "secretdata")
                         ?? throw new WinAuthCompatibilityException(
                             L.Get("service.winAuthConfig.error.missingSecretData"));
        var kind = ParseKind(typeName);
        var parts = secretData.Split('|');

        byte[] secret;
        var digits = 6;
        var period = 30;
        var algorithm = OtpAlgorithm.Sha1;

        var baseParts = parts[0].Split('\t');
        if (kind == AuthenticatorKind.BattleNet &&
            baseParts.Length == 1 &&
            parts.Length == 1 &&
            baseParts[0].Length > 40)
        {
            secret = Convert.FromHexString(baseParts[0][..40]);
            parts = [baseParts[0][..40], baseParts[0][40..]];
        }
        else
        {
            secret = Convert.FromHexString(baseParts[0]);
            if (baseParts.Length > 1)
            {
                digits = ParseInt(baseParts[1], digits);
            }

            if (baseParts.Length > 2 &&
                Enum.TryParse<OtpAlgorithm>(NormalizeAlgorithm(baseParts[2]), true, out var parsedAlgorithm))
            {
                algorithm = parsedAlgorithm;
            }

            if (baseParts.Length > 3)
            {
                period = ParseInt(baseParts[3], period);
            }
        }

        var entry = new AuthenticatorEntry
        {
            Kind = kind,
            Issuer = DefaultIssuer(kind),
            Secret = Base32Encoding.Encode(secret),
            Digits = digits,
            Period = period,
            Algorithm = algorithm,
            SortOrder = sortOrder,
            TimeOffsetSeconds = ParseLong(ElementValue(data, "servertimediff"), 0) / 1_000
        };

        switch (kind)
        {
            case AuthenticatorKind.Hotp:
                entry.Counter = parts.Length > 1 ? ParseLong(parts[1], 0) : 0;
                break;
            case AuthenticatorKind.BattleNet:
                entry.Serial = parts.Length > 2 && parts.Length == 3
                    ? DecodeUtf8HexOrRaw(parts[2])
                    : parts.Length > 1
                        ? DecodeUtf8HexOrRaw(parts[1])
                        : null;
                break;
            case AuthenticatorKind.Steam:
                entry.Serial = parts.Length > 1 ? DecodeUtf8HexOrRaw(parts[1]) : null;
                entry.DeviceId = parts.Length > 2 ? DecodeUtf8HexOrRaw(parts[2]) : null;
                if (parts.Length > 3)
                {
                    entry.ProviderData["steamData"] = DecodeUtf8HexOrRaw(parts[3]);
                }

                if (parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4]))
                {
                    entry.ProviderData["steamSessionData"] = DecodeUtf8HexOrRaw(parts[4]);
                }

                break;
            case AuthenticatorKind.Trion:
                if (parts.Length == 3 && parts[1].Contains('-', StringComparison.Ordinal))
                {
                    entry.Serial = parts[1];
                    entry.DeviceId = parts[2];
                }
                else
                {
                    entry.Serial = parts.Length > 1 ? DecodeUtf8HexOrRaw(parts[1]) : null;
                    entry.DeviceId = parts.Length > 2 ? DecodeUtf8HexOrRaw(parts[2]) : null;
                }

                break;
        }

        entry.ProviderData["winAuthType"] = typeName;
        return entry;
    }

    private static AppSettings ParseSettings(XElement root)
    {
        return new AppSettings
        {
            AlwaysOnTop = ElementBool(root, "alwaysontop", false),
            MinimizeToTray = ElementBool(root, "usetrayicon", true),
            StartWithWindows = ElementBool(root, "startwithwindows", false),
            WindowLeft = ParseInt(ElementValue(root, "left"), -1),
            WindowTop = ParseInt(ElementValue(root, "top"), -1),
            WindowWidth = Math.Max(
                360,
                ParseInt(
                    ElementValue(root, "width"),
                    AppSettings.DefaultWindowWidth)),
            WindowHeight = Math.Max(
                480,
                ParseInt(
                    ElementValue(root, "height"),
                    AppSettings.DefaultWindowHeight))
        };
    }

    private static XElement CreateCard(AuthenticatorEntry entry)
    {
        var card = new XElement(
            "WinAuthAuthenticator",
            new XAttribute("id", entry.Id),
            new XAttribute("type", WinAuthTypeName(entry.Kind)),
            new XElement("name", entry.DisplayName),
            new XElement(
                "created",
                new DateTimeOffset(
                        entry.CreatedAtUtc.Kind == DateTimeKind.Utc
                            ? entry.CreatedAtUtc
                            : entry.CreatedAtUtc.ToUniversalTime())
                    .ToUnixTimeMilliseconds()),
            new XElement("autorefresh", entry.Kind != AuthenticatorKind.Hotp && entry.AutoRefresh),
            new XElement("allowcopy", true),
            new XElement("copyoncode", entry.CopyOnReveal),
            new XElement("hideserial", entry.HideSerial),
            new XElement(
                "skin",
                entry.ProviderData.GetValueOrDefault("winAuthSkin") ?? string.Empty));

        if (!string.IsNullOrWhiteSpace(entry.Hotkey))
        {
            var hotkey = CreateHotkey(entry);
            if (hotkey is not null)
            {
                card.Add(hotkey);
            }
        }

        card.Add(new XElement(
            "authenticatordata",
            new XElement("servertimediff", checked(entry.TimeOffsetSeconds * 1_000)),
            new XElement("lastservertime", 0),
            new XElement("secretdata", BuildSecretData(entry))));
        return card;
    }

    private static string BuildSecretData(AuthenticatorEntry entry)
    {
        var secretHex = Convert.ToHexString(Base32Encoding.Decode(entry.Secret));
        var value = string.Join(
            '\t',
            secretHex,
            entry.Digits.ToString(CultureInfo.InvariantCulture),
            AlgorithmName(entry.Algorithm),
            entry.Period.ToString(CultureInfo.InvariantCulture));

        return entry.Kind switch
        {
            AuthenticatorKind.Hotp => value + "|" + entry.Counter.ToString(CultureInfo.InvariantCulture),
            AuthenticatorKind.BattleNet => value + "|" + EncodeUtf8(entry.Serial),
            AuthenticatorKind.Trion => value + "|" + EncodeUtf8(entry.Serial) + "|" + EncodeUtf8(entry.DeviceId),
            AuthenticatorKind.Steam => value + "|" + EncodeUtf8(entry.Serial) + "|" +
                                       EncodeUtf8(entry.DeviceId) + "|" +
                                       EncodeUtf8(entry.ProviderData.GetValueOrDefault("steamData")) + "|" +
                                       EncodeUtf8(entry.ProviderData.GetValueOrDefault("steamSessionData")),
            _ => value
        };
    }

    private static void ParseHotkey(XElement hotkey, AuthenticatorEntry entry)
    {
        try
        {
            var modifierBytes = Convert.FromHexString(ElementValue(hotkey, "modifiers") ?? string.Empty);
            var keyBytes = Convert.FromHexString(ElementValue(hotkey, "key") ?? string.Empty);
            if (modifierBytes.Length < 4 || keyBytes.Length < 2)
            {
                return;
            }

            var modifiers = BinaryPrimitives.ReadUInt32LittleEndian(modifierBytes);
            var key = BinaryPrimitives.ReadUInt16LittleEndian(keyBytes);
            var parts = new List<string>(4);
            if ((modifiers & 2) != 0)
            {
                parts.Add("Ctrl");
            }

            if ((modifiers & 1) != 0)
            {
                parts.Add("Alt");
            }

            if ((modifiers & 4) != 0)
            {
                parts.Add("Shift");
            }

            if ((modifiers & 8) != 0)
            {
                parts.Add("Win");
            }

            parts.Add(((Keys)key).ToString());
            entry.Hotkey = string.Join('+', parts);
            entry.HotkeyAction = (ElementValue(hotkey, "action") ?? string.Empty).ToLowerInvariant() switch
            {
                "inject" => EntryHotkeyAction.Type,
                "copy" => EntryHotkeyAction.Copy,
                "notify" => EntryHotkeyAction.Show,
                _ => EntryHotkeyAction.Show
            };

            var advanced = ElementValue(hotkey, "advanced");
            if (!string.IsNullOrWhiteSpace(advanced))
            {
                entry.ProviderData["winAuthHotkeyScript"] = advanced;
            }
        }
        catch (FormatException)
        {
            // Ignore malformed legacy hotkeys without losing the authenticator.
        }
    }

    private static XElement? CreateHotkey(AuthenticatorEntry entry)
    {
        if (!HotkeyService.TryParse(entry.Hotkey, out var modifiers, out var key))
        {
            return null;
        }

        Span<byte> modifierBytes = stackalloc byte[4];
        Span<byte> keyBytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt32LittleEndian(modifierBytes, (uint)modifiers);
        BinaryPrimitives.WriteUInt16LittleEndian(keyBytes, (ushort)key);

        var hotkey = new XElement(
            "hotkey",
            new XElement("modifiers", Convert.ToHexString(modifierBytes)),
            new XElement("key", Convert.ToHexString(keyBytes)),
            new XElement(
                "action",
                entry.HotkeyAction switch
                {
                    EntryHotkeyAction.Type => "Inject",
                    EntryHotkeyAction.Copy => "Copy",
                    _ => "Notify"
                }));

        if (entry.ProviderData.TryGetValue("winAuthHotkeyScript", out var advanced) &&
            !string.IsNullOrWhiteSpace(advanced))
        {
            hotkey.Add(new XElement("advanced", advanced));
        }

        return hotkey;
    }

    private static AuthenticatorKind ParseKind(string typeName)
    {
        typeName = typeName.ToLowerInvariant();
        if (typeName.Contains("hotp"))
        {
            return AuthenticatorKind.Hotp;
        }

        if (typeName.Contains("steam"))
        {
            return AuthenticatorKind.Steam;
        }

        if (typeName.Contains("battlenet"))
        {
            return AuthenticatorKind.BattleNet;
        }

        if (typeName.Contains("trion"))
        {
            return AuthenticatorKind.Trion;
        }

        if (typeName.Contains("guildwars"))
        {
            return AuthenticatorKind.GuildWars;
        }

        if (typeName.Contains("microsoft"))
        {
            return AuthenticatorKind.Microsoft;
        }

        if (typeName.Contains("okta"))
        {
            return AuthenticatorKind.OktaVerify;
        }

        if (typeName.Contains("google"))
        {
            return AuthenticatorKind.Google;
        }

        return AuthenticatorKind.Totp;
    }

    private static string WinAuthTypeName(AuthenticatorKind kind)
    {
        return kind switch
        {
            AuthenticatorKind.Hotp => "WinAuth.HOTPAuthenticator",
            AuthenticatorKind.Steam => "WinAuth.SteamAuthenticator",
            AuthenticatorKind.BattleNet => "WinAuth.BattleNetAuthenticator",
            AuthenticatorKind.Trion => "WinAuth.TrionAuthenticator",
            AuthenticatorKind.GuildWars => "WinAuth.GuildWarsAuthenticator",
            AuthenticatorKind.Microsoft => "WinAuth.MicrosoftAuthenticator",
            AuthenticatorKind.OktaVerify => "WinAuth.OktaVerifyAuthenticator",
            _ => "WinAuth.GoogleAuthenticator"
        };
    }

    private static string DefaultIssuer(AuthenticatorKind kind)
    {
        return kind switch
        {
            AuthenticatorKind.Google => "Google",
            AuthenticatorKind.Microsoft => "Microsoft",
            AuthenticatorKind.OktaVerify => "Okta Verify",
            AuthenticatorKind.GuildWars => "Guild Wars 2",
            AuthenticatorKind.Steam => "Steam",
            AuthenticatorKind.BattleNet => "Battle.net",
            AuthenticatorKind.Trion => "Trion / Glyph",
            AuthenticatorKind.Hotp => "HOTP",
            _ => string.Empty
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

    private static string NormalizeAlgorithm(string value)
    {
        return value.Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant() switch
        {
            "sha256" => nameof(OtpAlgorithm.Sha256),
            "sha512" => nameof(OtpAlgorithm.Sha512),
            _ => nameof(OtpAlgorithm.Sha1)
        };
    }

    private static string EncodeUtf8(string? value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : Convert.ToHexString(Encoding.UTF8.GetBytes(value));
    }

    private static string DecodeUtf8HexOrRaw(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromHexString(value));
        }
        catch (FormatException)
        {
            return value;
        }
    }

    private static DateTime ParseCreated(string? value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime
            : DateTime.UtcNow;
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static long ParseLong(string? value, long fallback)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static string? ElementValue(XElement parent, string name)
    {
        return parent.Elements().FirstOrDefault(element => NameIs(element, name))?.Value;
    }

    private static bool ElementBool(XElement parent, string name, bool fallback)
    {
        return bool.TryParse(ElementValue(parent, name), out var parsed) ? parsed : fallback;
    }

    private static bool NameIs(XElement element, string name)
    {
        return element.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase);
    }

    private static XDocument LoadDocument(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return LoadDocument(stream);
    }

    private static XDocument LoadDocument(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, false);
        return LoadDocument(stream);
    }

    private static XDocument LoadDocument(Stream stream)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 32 * 1024 * 1024,
            IgnoreComments = true
        };
        using var reader = XmlReader.Create(stream, settings);
        try
        {
            return XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthConfig.error.invalidXml"),
                exception);
        }
    }

    private static byte[] SerializeElement(XElement element, bool includeBom)
    {
        using var stream = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(includeBom),
            Indent = true,
            OmitXmlDeclaration = true,
            CloseOutput = false
        };
        using (var writer = XmlWriter.Create(stream, settings))
        {
            element.WriteTo(writer);
        }

        return stream.ToArray();
    }

    private static void WriteAtomic(string filePath, XDocument document)
    {
        var fullPath = Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(fullPath)
                        ?? throw new IOException(
                            L.Get("service.winAuthConfig.error.invalidExportPath"));
        Directory.CreateDirectory(directory);

        var temporaryPath = fullPath + ".tmp";
        var backupPath = fullPath + ".bak";
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            NewLineChars = Environment.NewLine
        };
        using (var writer = XmlWriter.Create(temporaryPath, settings))
        {
            document.Save(writer);
        }

        if (File.Exists(fullPath))
        {
            File.Copy(fullPath, backupPath, true);
        }

        File.Move(temporaryPath, fullPath, true);
    }
}

internal static class XElementTraversalExtensions
{
    public static IEnumerable<XElement> DescendantsAndSelf(this XDocument document)
    {
        return document.Root?.DescendantsAndSelf() ?? [];
    }
}
