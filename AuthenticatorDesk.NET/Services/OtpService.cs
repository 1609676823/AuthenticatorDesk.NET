/*
 * This file is part of AuthenticatorDesk.
 * Modifications Copyright (C) 2026 AuthenticatorDesk contributors.
 *
 * Provider-specific portions were adapted and substantially modified for
 * AuthenticatorDesk on 2026-07-29 from WinAuth:
 *   Authenticator.cs, Copyright (C) 2011 Colin Mackie;
 *   BattleNetAuthenticator.cs and TrionAuthenticator.cs,
 *     Copyright (C) 2013 Colin Mackie; and
 *   SteamAuthenticator.cs, Copyright (C) 2015 Colin Mackie.
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
using AuthenticatorDesk.Localization;
using AuthenticatorDesk.Models;

namespace AuthenticatorDesk.Services;

public static class OtpService
{
    private const string SteamAlphabet = "23456789BCDFGHJKMNPQRTVWXY";

    public static void ValidateForCodeGeneration(AuthenticatorEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (!Base32Encoding.TryDecode(entry.Secret, out var secret) || secret.Length < 10)
        {
            throw new FormatException(
                L.Get("service.otp.error.secretTooShort"));
        }

        if (entry.Kind != AuthenticatorKind.Steam && entry.Digits is < 4 or > 10)
        {
            throw new FormatException(
                L.Get("service.otp.error.invalidDigitCount"));
        }

        if (entry.Kind != AuthenticatorKind.Hotp && entry.Period is < 1 or > 3600)
        {
            throw new FormatException(
                L.Get("service.otp.error.invalidRefreshPeriod"));
        }

        if (entry.Counter < 0)
        {
            throw new FormatException(
                L.Get("service.otp.error.negativeHotpCounter"));
        }

        const long maximumTimeOffsetSeconds = 10L * 365 * 24 * 60 * 60;
        if (entry.TimeOffsetSeconds is < -maximumTimeOffsetSeconds or > maximumTimeOffsetSeconds)
        {
            throw new FormatException(
                L.Get("service.otp.error.timeOffsetOutOfRange"));
        }
    }

    public static string GetCode(AuthenticatorEntry entry, DateTimeOffset? instant = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var secret = Base32Encoding.Decode(entry.Secret);

        return entry.Kind switch
        {
            AuthenticatorKind.Hotp => GenerateHotp(
                secret,
                entry.Counter,
                entry.Digits,
                entry.Algorithm),
            AuthenticatorKind.Steam => GenerateSteamCode(
                secret,
                GetTimeCounter(entry, instant)),
            AuthenticatorKind.Trion => GenerateTrionCode(
                secret,
                GetTimeCounter(entry, instant)),
            _ => GenerateHotp(
                secret,
                GetTimeCounter(entry, instant),
                entry.Digits,
                entry.Algorithm)
        };
    }

    public static long GetTimeCounter(AuthenticatorEntry entry, DateTimeOffset? instant = null)
    {
        var period = Math.Clamp(entry.Period, 1, 3600);
        var timestamp = (instant ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() + entry.TimeOffsetSeconds;
        return Math.Max(0, timestamp / period);
    }

    public static int GetRemainingSeconds(AuthenticatorEntry entry, DateTimeOffset? instant = null)
    {
        if (entry.Kind == AuthenticatorKind.Hotp)
        {
            return 0;
        }

        var period = Math.Clamp(entry.Period, 1, 3600);
        var timestamp = (instant ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() + entry.TimeOffsetSeconds;
        var elapsed = (int)(((timestamp % period) + period) % period);
        return period - elapsed;
    }

    public static double GetRemainingFraction(AuthenticatorEntry entry, DateTimeOffset? instant = null)
    {
        if (entry.Kind == AuthenticatorKind.Hotp)
        {
            return 1d;
        }

        return GetRemainingSeconds(entry, instant) / (double)Math.Max(1, entry.Period);
    }

    public static string GenerateTotp(
        ReadOnlySpan<byte> secret,
        long unixTimeSeconds,
        int period = 30,
        int digits = 6,
        OtpAlgorithm algorithm = OtpAlgorithm.Sha1)
    {
        if (period <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(period),
                L.Get("service.otp.error.invalidRefreshPeriod"));
        }

        return GenerateHotp(secret, unixTimeSeconds / period, digits, algorithm);
    }

    public static string GenerateHotp(
        ReadOnlySpan<byte> secret,
        long counter,
        int digits = 6,
        OtpAlgorithm algorithm = OtpAlgorithm.Sha1)
    {
        if (secret.Length == 0)
        {
            throw new ArgumentException(
                L.Get("service.otp.error.secretRequired"),
                nameof(secret));
        }

        if (counter < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(counter),
                L.Get("service.otp.error.negativeHotpCounter"));
        }

        if (digits is < 4 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(digits),
                L.Get("service.otp.error.invalidDigitCount"));
        }

        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
        var digest = ComputeHmac(secret, counterBytes, algorithm);
        var binaryCode = Truncate(digest);
        var modulus = Pow10(digits);
        return (binaryCode % modulus).ToString(new string('0', digits), CultureInfo.InvariantCulture);
    }

    public static string GenerateSteamCode(ReadOnlySpan<byte> secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
        var digest = ComputeHmac(secret, counterBytes, OtpAlgorithm.Sha1);
        var value = Truncate(digest);

        Span<char> code = stackalloc char[5];
        for (var index = 0; index < code.Length; index++)
        {
            code[index] = SteamAlphabet[(int)(value % SteamAlphabet.Length)];
            value /= (uint)SteamAlphabet.Length;
        }

        return new string(code);
    }

    public static string GenerateTrionCode(ReadOnlySpan<byte> secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);
        var digest = ComputeHmac(secret, counterBytes, OtpAlgorithm.Sha1);
        var offset = digest[^1] & 0x0f;
        var fullCode = BinaryPrimitives.ReadUInt32BigEndian(digest.AsSpan(offset, 4));
        var eightDigits = (fullCode % 100_000_000u).ToString("D8", CultureInfo.InvariantCulture);
        return eightDigits[..6];
    }

    public static string? GetBattleNetRestoreCode(AuthenticatorEntry entry)
    {
        if (entry.Kind != AuthenticatorKind.BattleNet ||
            string.IsNullOrWhiteSpace(entry.Serial) ||
            !Base32Encoding.TryDecode(entry.Secret, out var secret))
        {
            return null;
        }

        var serialBytes = Encoding.UTF8.GetBytes(entry.Serial.ToUpperInvariant().Replace("-", string.Empty));
        var combined = new byte[serialBytes.Length + secret.Length];
        serialBytes.CopyTo(combined, 0);
        secret.CopyTo(combined, serialBytes.Length);
        var digest = SHA1.HashData(combined);

        Span<char> code = stackalloc char[10];
        for (var index = 0; index < code.Length; index++)
        {
            code[index] = RestoreCodeCharacter(digest[digest.Length - code.Length + index]);
        }

        CryptographicOperations.ZeroMemory(combined);
        return new string(code);
    }

    public static string FormatCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Any(character => !char.IsDigit(character)))
        {
            return code;
        }

        return code.Length switch
        {
            6 => $"{code[..3]} {code[3..]}",
            8 => $"{code[..4]} {code[4..]}",
            10 => $"{code[..5]} {code[5..]}",
            _ => code
        };
    }

    private static byte[] ComputeHmac(
        ReadOnlySpan<byte> secret,
        ReadOnlySpan<byte> value,
        OtpAlgorithm algorithm)
    {
        return algorithm switch
        {
            OtpAlgorithm.Sha1 => HMACSHA1.HashData(secret, value),
            OtpAlgorithm.Sha256 => HMACSHA256.HashData(secret, value),
            OtpAlgorithm.Sha512 => HMACSHA512.HashData(secret, value),
            _ => throw new ArgumentOutOfRangeException(
                nameof(algorithm),
                L.Get("service.otp.error.unsupportedAlgorithm"))
        };
    }

    private static uint Truncate(ReadOnlySpan<byte> digest)
    {
        var offset = digest[^1] & 0x0f;
        return BinaryPrimitives.ReadUInt32BigEndian(digest.Slice(offset, 4)) & 0x7fff_ffffu;
    }

    private static ulong Pow10(int digits)
    {
        ulong value = 1;
        for (var index = 0; index < digits; index++)
        {
            value *= 10;
        }

        return value;
    }

    private static char RestoreCodeCharacter(byte value)
    {
        var index = value & 0x1f;
        if (index <= 9)
        {
            return (char)('0' + index);
        }

        index += 'A' - 10;
        if (index >= 'I')
        {
            index++;
        }

        if (index >= 'L')
        {
            index++;
        }

        if (index >= 'O')
        {
            index++;
        }

        if (index >= 'S')
        {
            index++;
        }

        return (char)index;
    }
}
