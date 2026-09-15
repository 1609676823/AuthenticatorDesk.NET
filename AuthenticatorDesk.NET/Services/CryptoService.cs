using System.Security.Cryptography;
using System.Text;
using AuthenticatorDesk.Localization;

namespace AuthenticatorDesk.Services;

internal static class CryptoService
{
    public const int PasswordIterations = 600_000;
    public const int KeySize = 32;
    public const int SaltSize = 16;
    public const int NonceSize = 12;
    public const int TagSize = 16;

    public static byte[] CreateRandomKey() => RandomNumberGenerator.GetBytes(KeySize);

    public static byte[] DeriveKey(string password, ReadOnlySpan<byte> salt, int iterations)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException(
                L.Get("service.crypto.error.passwordRequired"),
                nameof(password));
        }

        if (iterations < 100_000)
        {
            throw new CryptographicException(
                L.Get("service.crypto.error.iterationsBelowMinimum"));
        }

        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeySize);
    }

    public static CipherPackage Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key,
        string associatedData)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        var aad = Encoding.UTF8.GetBytes(associatedData);

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);
        }

        return new CipherPackage(nonce, tag, ciphertext);
    }

    public static byte[] Decrypt(
        CipherPackage package,
        ReadOnlySpan<byte> key,
        string associatedData)
    {
        if (package.Nonce.Length != NonceSize || package.Tag.Length != TagSize)
        {
            throw new CryptographicException(
                L.Get("service.crypto.error.invalidNonceOrTagLength"));
        }

        var plaintext = new byte[package.Ciphertext.Length];
        var aad = Encoding.UTF8.GetBytes(associatedData);
        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Decrypt(package.Nonce, package.Ciphertext, package.Tag, plaintext, aad);
        }

        return plaintext;
    }
}

internal sealed record CipherPackage(byte[] Nonce, byte[] Tag, byte[] Ciphertext);
