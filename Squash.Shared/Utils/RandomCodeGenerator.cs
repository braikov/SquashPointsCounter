using System.Security.Cryptography;

namespace Squash.Shared.Utils
{
    public static class RandomCodeGenerator
    {
        private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string GenerateSixCharCode()
        {
            Span<char> buffer = stackalloc char[6];
            Span<byte> bytes = stackalloc byte[6];

            RandomNumberGenerator.Fill(bytes);

            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = AllowedChars[bytes[i] % AllowedChars.Length];
            }

            return new string(buffer);
        }
    }
}
