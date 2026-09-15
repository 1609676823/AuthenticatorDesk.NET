using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Org.BouncyCastle.Crypto
{
    internal interface ICipherParameters
    {
    }

    internal interface IBlockCipher
    {
        string AlgorithmName { get; }

        bool IsPartialBlockOkay { get; }

        void Init(bool forEncryption, ICipherParameters parameters);

        int GetBlockSize();

        int ProcessBlock(byte[] input, int inOff, byte[] output, int outOff);

        void Reset();
    }

    internal sealed class DataLengthException(string message) : Exception(message);

    internal sealed class InvalidCipherTextException(string message) : Exception(message);
}

namespace Org.BouncyCastle.Crypto.Parameters
{
    using Org.BouncyCastle.Crypto;

    internal sealed class KeyParameter : ICipherParameters
    {
        private readonly byte[] _key;

        public KeyParameter(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);
            _key = (byte[])key.Clone();
        }

        public byte[] GetKey() => (byte[])_key.Clone();
    }
}

namespace Org.BouncyCastle.Crypto.Utilities
{
    internal static class Pack
    {
        public static uint BE_To_UInt32(byte[] input, int offset)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(input.AsSpan(offset, 4));
        }

        public static void UInt32_To_BE(uint value, byte[] output, int offset)
        {
            BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(offset, 4), value);
        }
    }
}

namespace Org.BouncyCastle.Crypto.Paddings
{
    using Org.BouncyCastle.Crypto;

    /// <summary>
    /// Marker matching WinAuth's original ISO10126-2 padding selection.
    /// Random padding bytes are deliberately not validated during decryption.
    /// </summary>
    internal sealed class ISO10126d2Padding
    {
    }

    /// <summary>
    /// Minimal buffered block-cipher adapter used only by the WinAuth compatibility
    /// layer. It intentionally implements the subset of the historic Bouncy Castle
    /// API that WinAuth 3.5 used.
    /// </summary>
    internal sealed class PaddedBufferedBlockCipher
    {
        private readonly IBlockCipher _cipher;
        private readonly MemoryStream _buffer = new();
        private bool _encrypting;

        public PaddedBufferedBlockCipher(
            IBlockCipher cipher,
            ISO10126d2Padding padding)
        {
            _cipher = cipher;
            _ = padding;
        }

        public void Init(bool forEncryption, ICipherParameters parameters)
        {
            _encrypting = forEncryption;
            _buffer.SetLength(0);
            _cipher.Init(forEncryption, parameters);
        }

        public int GetOutputSize(int inputLength)
        {
            var blockSize = _cipher.GetBlockSize();
            var total = checked((int)_buffer.Length + inputLength);
            if (_encrypting)
            {
                return checked(total + blockSize - (total % blockSize));
            }

            return total;
        }

        public int ProcessBytes(
            byte[] input,
            int inputOffset,
            int length,
            byte[] output,
            int outputOffset)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(output);
            if (inputOffset < 0 || length < 0 || inputOffset > input.Length - length)
            {
                throw new ArgumentOutOfRangeException(nameof(inputOffset));
            }

            if (outputOffset < 0 || outputOffset > output.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(outputOffset));
            }

            _buffer.Write(input, inputOffset, length);
            return 0;
        }

        public int DoFinal(byte[] output, int outputOffset)
        {
            ArgumentNullException.ThrowIfNull(output);
            var input = _buffer.ToArray();
            _buffer.SetLength(0);
            return _encrypting
                ? EncryptFinal(input, output, outputOffset)
                : DecryptFinal(input, output, outputOffset);
        }

        private int EncryptFinal(byte[] input, byte[] output, int outputOffset)
        {
            var blockSize = _cipher.GetBlockSize();
            var paddingLength = blockSize - input.Length % blockSize;
            var padded = new byte[input.Length + paddingLength];
            input.CopyTo(padded, 0);
            if (paddingLength > 1)
            {
                RandomNumberGenerator.Fill(
                    padded.AsSpan(input.Length, paddingLength - 1));
            }

            padded[^1] = (byte)paddingLength;
            EnsureOutput(output, outputOffset, padded.Length);
            for (var offset = 0; offset < padded.Length; offset += blockSize)
            {
                _cipher.ProcessBlock(padded, offset, output, outputOffset + offset);
            }

            CryptographicOperations.ZeroMemory(padded);
            return input.Length + paddingLength;
        }

        private int DecryptFinal(byte[] input, byte[] output, int outputOffset)
        {
            var blockSize = _cipher.GetBlockSize();
            if (input.Length == 0 || input.Length % blockSize != 0)
            {
                throw new InvalidCipherTextException(
                    "Ciphertext is not a non-empty multiple of the block size.");
            }

            var plaintext = new byte[input.Length];
            try
            {
                for (var offset = 0; offset < input.Length; offset += blockSize)
                {
                    _cipher.ProcessBlock(input, offset, plaintext, offset);
                }

                var paddingLength = plaintext[^1];
                if (paddingLength < 1 || paddingLength > blockSize)
                {
                    throw new InvalidCipherTextException("Invalid ISO10126 padding.");
                }

                var length = plaintext.Length - paddingLength;
                EnsureOutput(output, outputOffset, length);
                plaintext.AsSpan(0, length).CopyTo(output.AsSpan(outputOffset));
                return length;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }

        private static void EnsureOutput(byte[] output, int offset, int length)
        {
            if (offset < 0 || length < 0 || offset > output.Length - length)
            {
                throw new DataLengthException("Output buffer is too short.");
            }
        }
    }
}
