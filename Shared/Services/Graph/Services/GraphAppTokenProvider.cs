using System.Text.Json;
using Abril_Backend.Shared.Services.Graph.Interfaces;

namespace Abril_Backend.Shared.Services.Graph.Services
{
    /// <inheritdoc cref="IGraphAppTokenProvider"/>
    public class GraphAppTokenProvider : IGraphAppTokenProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GraphAppTokenProvider> _logger;

        // El token de aplicación dura ~1 hora y no depende de ningún usuario, así que se cachea:
        // pedir uno por cada correo sería un roundtrip extra a login.microsoftonline.com en cada
        // envío. El semáforo evita que varias requests concurrentes lo renueven a la vez.
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string? _cachedToken;
        private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

        /// <summary>Margen para no entregar un token que expire mientras la request viaja.</summary>
        private static readonly TimeSpan Skew = TimeSpan.FromMinutes(5);

        public GraphAppTokenProvider(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<GraphAppTokenProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
                return _cachedToken;

            await _gate.WaitAsync(cancellationToken);
            try
            {
                // Otra request pudo renovarlo mientras esperábamos el semáforo.
                if (_cachedToken is not null && DateTimeOffset.UtcNow < _expiresAt)
                    return _cachedToken;

                var tenantId = _configuration["AzureAd:TenantId"];
                var clientId = _configuration["AzureAd:ClientId"];
                var clientSecret = _configuration["AzureAd:ClientSecret"];

                if (string.IsNullOrWhiteSpace(tenantId) ||
                    string.IsNullOrWhiteSpace(clientId) ||
                    string.IsNullOrWhiteSpace(clientSecret))
                    throw new InvalidOperationException(
                        "Faltan AzureAd:TenantId, AzureAd:ClientId o AzureAd:ClientSecret en la configuración.");

                var body = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
                });

                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(
                    $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token", body, cancellationToken);

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException(
                        $"Entra ID rechazó el token de aplicación ({(int)response.StatusCode}): {json}");

                using var doc = JsonDocument.Parse(json);

                var token = doc.RootElement.TryGetProperty("access_token", out var tokenProp)
                    ? tokenProp.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(token))
                    throw new InvalidOperationException(
                        "La respuesta de Entra ID no trae access_token.");

                var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expiresProp)
                    && expiresProp.TryGetInt32(out var seconds)
                        ? seconds
                        : 3600;

                _cachedToken = token;
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(seconds: expiresIn) - Skew;

                _logger.LogInformation(
                    "Token de aplicación de Graph renovado; vigente hasta {ExpiresAt:u}.", _expiresAt);

                return token;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
