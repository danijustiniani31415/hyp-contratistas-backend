using System.Security.Cryptography;

namespace Abril_Backend.Shared.Services
{
    /// <summary>
    /// Hashing del PIN de firma del médico ocupacional (PBKDF2/SHA-256). El PIN nunca se
    /// guarda ni se transmite en texto plano fuera de la request de configuración inicial.
    /// Formato almacenado: "{iteraciones}.{saltBase64}.{hashBase64}".
    /// </summary>
    public static class PinHasher
    {
        private const int Iteraciones = 100_000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public static string Hash(string pin)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltBytes);
            var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iteraciones, HashAlgorithmName.SHA256, HashBytes);
            return $"{Iteraciones}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string pin, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            var parts = storedHash.Split('.');
            if (parts.Length != 3) return false;
            if (!int.TryParse(parts[0], out var iteraciones)) return false;

            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iteraciones, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
