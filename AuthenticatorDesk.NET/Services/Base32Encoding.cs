using System.Text;
using AuthenticatorDesk.Localization;

namespace AuthenticatorDesk.Services;

public static class Base32Encoding
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static byte[] Decode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException(L.Get("service.base32.error.secretRequired"));
        }

        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            throw new FormatException(
                L.Get("service.base32.error.noValidCharacters"));
        }

        var output = new byte[normalized.Length * 5 / 8];
        var outputIndex = 0;
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var character in normalized)
        {
            var digit = Alphabet.IndexOf(character);
            if (digit < 0)
            {
                throw new FormatException(
                    L.Format("service.base32.error.invalidCharacter", character));
            }

            buffer = (buffer << 5) | digit;
            bitsLeft += 5;

            if (bitsLeft < 8)
            {
                continue;
            }

            bitsLeft -= 8;
            output[outputIndex++] = (byte)(buffer >> bitsLeft);
            buffer &= (1 << bitsLeft) - 1;
        }

        if (outputIndex == output.Length)
        {
            return output;
        }

        return output[..outputIndex];
    }

    public static string Encode(ReadOnlySpan<byte> data, bool includePadding = false)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var value in data)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;

            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                output.Append(Alphabet[(buffer >> bitsLeft) & 31]);
                buffer &= (1 << bitsLeft) - 1;
            }
        }

        if (bitsLeft > 0)
        {
            output.Append(Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        if (includePadding)
        {
            while (output.Length % 8 != 0)
            {
                output.Append('=');
            }
        }

        return output.ToString();
    }

    public static string Normalize(string value)
    {
        var output = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (character is ' ' or '-' or '\t' or '\r' or '\n' or '=')
            {
                continue;
            }

            output.Append(char.ToUpperInvariant(character));
        }

        return output.ToString();
    }

    public static bool TryDecode(string value, out byte[] bytes)
    {
        try
        {
            bytes = Decode(value);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }
}
