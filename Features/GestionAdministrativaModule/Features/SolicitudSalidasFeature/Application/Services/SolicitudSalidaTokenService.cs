using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Services
{
    /// <summary>
    /// Tokens autocontenidos firmados con HMAC-SHA256. Formato:
    ///   base64url(payload) + "." + base64url(signature)
    /// donde payload = "{solicitudId}:{action}:{expiresAtUnix}".
    /// La clave de firma reutiliza Jwt:Key.
    /// </summary>
    public class SolicitudSalidaTokenService : ISolicitudSalidaTokenService
    {
        private readonly byte[] _key;

        public SolicitudSalidaTokenService(IConfiguration configuration)
        {
            var key = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key no configurado.");
            _key = Encoding.UTF8.GetBytes(key);
        }

        public string Generate(int solicitudId, SolicitudSalidaAction action, TimeSpan validity)
        {
            var exp = DateTimeOffset.UtcNow.Add(validity).ToUnixTimeSeconds();
            var payload = $"{solicitudId}:{(int)action}:{exp}";
            var sig = Sign(payload);
            return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(sig)}";
        }

        public SolicitudSalidaTokenPayload? Validate(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var parts = token.Split('.');
            if (parts.Length != 2) return null;

            byte[] payloadBytes, sigBytes;
            try
            {
                payloadBytes = Base64UrlDecode(parts[0]);
                sigBytes     = Base64UrlDecode(parts[1]);
            }
            catch { return null; }

            var payload = Encoding.UTF8.GetString(payloadBytes);
            var expectedSig = Sign(payload);
            if (!CryptographicOperations.FixedTimeEquals(expectedSig, sigBytes)) return null;

            var fields = payload.Split(':');
            if (fields.Length != 3) return null;
            if (!int.TryParse(fields[0], out var solicitudId)) return null;
            if (!int.TryParse(fields[1], out var actionInt))   return null;
            if (!long.TryParse(fields[2], out var expUnix))    return null;

            if (DateTimeOffset.FromUnixTimeSeconds(expUnix) <= DateTimeOffset.UtcNow) return null;

            return new SolicitudSalidaTokenPayload
            {
                SolicitudId = solicitudId,
                Action      = (SolicitudSalidaAction)actionInt,
            };
        }

        private byte[] Sign(string payload)
        {
            using var hmac = new HMACSHA256(_key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        }

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string s)
        {
            var padded = s.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }
    }
}
