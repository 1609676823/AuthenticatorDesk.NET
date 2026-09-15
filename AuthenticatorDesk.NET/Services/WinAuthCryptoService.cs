/*
 * This file is part of AuthenticatorDesk.
 * Modifications Copyright (C) 2026 AuthenticatorDesk contributors.
 *
 * Adapted and substantially modified for AuthenticatorDesk on 2026-07-29
 * from portions of WinAuth Authenticator.cs:
 * Copyright (C) 2011 Colin Mackie.
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

using System.Security.Cryptography;
using System.Text;
using AuthenticatorDesk.Localization;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;

namespace AuthenticatorDesk.Services;

[Flags]
internal enum WinAuthProtection
{
    None = 0,
    Password = 1,
    WindowsUser = 2,
    WindowsMachine = 4,
    YubiKeySlot1 = 8,
    YubiKeySlot2 = 16
}

public sealed class WinAuthPasswordRequiredException : Exception
{
    public WinAuthPasswordRequiredException()
        : base(L.Get("service.winAuthCrypto.error.passwordRequired"))
    {
    }
}

public sealed class WinAuthCompatibilityException : Exception
{
    public WinAuthCompatibilityException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

internal static class WinAuthCryptoService
{
    private const int SaltLength = 8;
    private const int Pbkdf2Iterations = 2_000;
    private const int Pbkdf2KeyLength = 256;
    private static readonly string EncryptionHeader = Convert.ToHexString(Encoding.UTF8.GetBytes("WINAUTH3"));

    public static WinAuthProtection DecodeProtection(string? value)
    {
        var result = WinAuthProtection.None;
        foreach (var character in value ?? string.Empty)
        {
            result |= char.ToLowerInvariant(character) switch
            {
                'y' => WinAuthProtection.Password,
                'u' => WinAuthProtection.WindowsUser,
                'm' => WinAuthProtection.WindowsMachine,
                'a' => WinAuthProtection.YubiKeySlot1,
                'b' => WinAuthProtection.YubiKeySlot2,
                _ => WinAuthProtection.None
            };
        }

        return result;
    }

    public static string EncodeProtection(WinAuthProtection protection)
    {
        var output = new StringBuilder(5);
        if (protection.HasFlag(WinAuthProtection.Password))
        {
            output.Append('y');
        }

        if (protection.HasFlag(WinAuthProtection.WindowsUser))
        {
            output.Append('u');
        }

        if (protection.HasFlag(WinAuthProtection.WindowsMachine))
        {
            output.Append('m');
        }

        if (protection.HasFlag(WinAuthProtection.YubiKeySlot1))
        {
            output.Append('a');
        }

        if (protection.HasFlag(WinAuthProtection.YubiKeySlot2))
        {
            output.Append('b');
        }

        return output.ToString();
    }

    public static string DecryptSequence(
        string encoded,
        WinAuthProtection protection,
        string? password)
    {
        ValidateSupported(protection);
        encoded = NormalizeHex(encoded);

        if (!encoded.StartsWith(EncryptionHeader, StringComparison.OrdinalIgnoreCase))
        {
            return DecryptLayers(encoded, protection, password);
        }

        var offset = EncryptionHeader.Length;
        var saltHexLength = SaltLength * 2;
        var hashHexLength = SHA256.HashSizeInBytes * 2;
        if (encoded.Length < offset + saltHexLength + hashHexLength)
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthCrypto.error.incompleteHeader"));
        }

        var salt = Convert.FromHexString(encoded.AsSpan(offset, saltHexLength));
        offset += saltHexLength;
        var expectedHash = Convert.FromHexString(encoded.AsSpan(offset, hashHexLength));
        offset += hashHexLength;

        var plaintextHex = DecryptLayers(encoded[offset..], protection, password);
        var plaintext = Convert.FromHexString(plaintextHex);
        var hashInput = new byte[salt.Length + plaintext.Length];
        salt.CopyTo(hashInput, 0);
        plaintext.CopyTo(hashInput, salt.Length);
        var actualHash = SHA256.HashData(hashInput);
        CryptographicOperations.ZeroMemory(hashInput);

        if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthCrypto.error.wrongPasswordOrCorrupt"));
        }

        return plaintextHex;
    }

    public static string EncryptSequence(
        string plaintextHex,
        WinAuthProtection protection,
        string? password)
    {
        ValidateSupported(protection);
        plaintextHex = NormalizeHex(plaintextHex);
        var plaintext = Convert.FromHexString(plaintextHex);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hashInput = new byte[salt.Length + plaintext.Length];
        salt.CopyTo(hashInput, 0);
        plaintext.CopyTo(hashInput, salt.Length);
        var hash = SHA256.HashData(hashInput);
        CryptographicOperations.ZeroMemory(hashInput);

        var encrypted = EncryptLayers(plaintextHex, protection, password);
        return EncryptionHeader + Convert.ToHexString(salt) + Convert.ToHexString(hash) + encrypted;
    }

    private static string DecryptLayers(
        string encoded,
        WinAuthProtection protection,
        string? password)
    {
        try
        {
            if (protection.HasFlag(WinAuthProtection.WindowsMachine))
            {
                encoded = Convert.ToHexString(ProtectedData.Unprotect(
                    Convert.FromHexString(encoded),
                    null,
                    DataProtectionScope.LocalMachine));
            }

            if (protection.HasFlag(WinAuthProtection.WindowsUser))
            {
                encoded = Convert.ToHexString(ProtectedData.Unprotect(
                    Convert.FromHexString(encoded),
                    null,
                    DataProtectionScope.CurrentUser));
            }

            if (protection.HasFlag(WinAuthProtection.Password))
            {
                if (string.IsNullOrEmpty(password))
                {
                    throw new WinAuthPasswordRequiredException();
                }

                encoded = DecryptWithPassword(encoded, password);
            }

            return NormalizeHex(encoded);
        }
        catch (WinAuthPasswordRequiredException)
        {
            throw;
        }
        catch (WinAuthCompatibilityException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is CryptographicException or
            FormatException or
            InvalidCipherTextException or
            ArgumentException)
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthCrypto.error.decryptFailed"),
                exception);
        }
    }

    private static string EncryptLayers(
        string plaintextHex,
        WinAuthProtection protection,
        string? password)
    {
        if (protection.HasFlag(WinAuthProtection.Password))
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new WinAuthPasswordRequiredException();
            }

            plaintextHex = EncryptWithPassword(plaintextHex, password);
        }

        if (protection.HasFlag(WinAuthProtection.WindowsUser))
        {
            plaintextHex = Convert.ToHexString(ProtectedData.Protect(
                Convert.FromHexString(plaintextHex),
                null,
                DataProtectionScope.CurrentUser));
        }

        if (protection.HasFlag(WinAuthProtection.WindowsMachine))
        {
            plaintextHex = Convert.ToHexString(ProtectedData.Protect(
                Convert.FromHexString(plaintextHex),
                null,
                DataProtectionScope.LocalMachine));
        }

        return plaintextHex;
    }

    private static string EncryptWithPassword(string plaintextHex, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var key = DeriveLegacyKey(password, salt);
        var plaintext = Convert.FromHexString(plaintextHex);
        try
        {
            var encrypted = TransformBlowfish(plaintext, key, true);
            return Convert.ToHexString(salt) + Convert.ToHexString(encrypted);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static string DecryptWithPassword(string encoded, string password)
    {
        var saltHexLength = SaltLength * 2;
        if (encoded.Length <= saltHexLength)
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthCrypto.error.incompletePasswordData"));
        }

        var salt = Convert.FromHexString(encoded.AsSpan(0, saltHexLength));
        var key = DeriveLegacyKey(password, salt);
        var ciphertext = Convert.FromHexString(encoded.AsSpan(saltHexLength));
        try
        {
            var plaintext = TransformBlowfish(ciphertext, key, false);
            try
            {
                return Convert.ToHexString(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(ciphertext);
        }
    }

    private static byte[] DeriveLegacyKey(string password, ReadOnlySpan<byte> salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA1,
            Pbkdf2KeyLength);
    }

    private static byte[] TransformBlowfish(byte[] input, byte[] key, bool encrypt)
    {
        var cipher = new PaddedBufferedBlockCipher(
            new BlowfishEngine(),
            new ISO10126d2Padding());
        cipher.Init(encrypt, new KeyParameter(key));

        var output = new byte[cipher.GetOutputSize(input.Length)];
        var length = cipher.ProcessBytes(input, 0, input.Length, output, 0);
        length += cipher.DoFinal(output, length);
        return output[..length];
    }

    private static string NormalizeHex(string value)
    {
        var normalized = string.Concat(value.Where(character => !char.IsWhiteSpace(character)));
        if (normalized.Length == 0 || normalized.Length % 2 != 0)
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthCrypto.error.invalidHex"));
        }

        _ = Convert.FromHexString(normalized);
        return normalized.ToUpperInvariant();
    }

    private static void ValidateSupported(WinAuthProtection protection)
    {
        if (protection.HasFlag(WinAuthProtection.YubiKeySlot1) ||
            protection.HasFlag(WinAuthProtection.YubiKeySlot2))
        {
            throw new WinAuthCompatibilityException(
                L.Get("service.winAuthCrypto.error.yubiKeyProtected"));
        }
    }
}
