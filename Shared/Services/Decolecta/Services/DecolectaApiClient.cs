using System.Net;
using System.Net.Http.Headers;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Shared.Services.Decolecta.Interfaces;

namespace Abril_Backend.Shared.Services.Decolecta.Services
{
    public class DecolectaApiClient : IDecolectaApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IDecolectaTokenStore _tokenStore;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DecolectaApiClient> _logger;

        public DecolectaApiClient(
            HttpClient httpClient,
            IDecolectaTokenStore tokenStore,
            IConfiguration configuration,
            ILogger<DecolectaApiClient> logger)
        {
            _httpClient = httpClient;
            _tokenStore = tokenStore;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> GetAsync(string requestUri, string mensajeAgotado)
        {
            var tokens = await _tokenStore.GetDisponiblesAsync();

            foreach (var token in tokens)
            {
                var response = await SendAsync(requestUri, token.Token);
                if (!EsCuotaAgotada(response.StatusCode))
                    return response;

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "[Decolecta] Token {Username} ({Correo}) respondió {StatusCode}; se marca agotado y se rota al siguiente. Body: {Body}",
                    token.Username, token.Correo, (int)response.StatusCode, body);
                response.Dispose();

                await _tokenStore.MarcarAgotadoAsync(token.Id);
            }

            // Red de seguridad: si la tabla no tiene tokens usables (p. ej. BD de dev sin datos),
            // se intenta una vez con el token fijo de appsettings.
            if (tokens.Count == 0)
            {
                var tokenConfig = _configuration["Sunat:Token"] ?? _configuration["Reniec:Token"];
                if (!string.IsNullOrEmpty(tokenConfig))
                {
                    _logger.LogWarning("[Decolecta] Sin tokens disponibles en la tabla decolecta_token; se usa el token de appsettings.");
                    var response = await SendAsync(requestUri, tokenConfig);
                    if (!EsCuotaAgotada(response.StatusCode))
                        return response;
                    response.Dispose();
                }
            }

            throw new AbrilException(mensajeAgotado, 503);
        }

        private async Task<HttpResponseMessage> SendAsync(string requestUri, string token)
        {
            // El header va por request (no en DefaultRequestHeaders) porque el token puede
            // cambiar entre requests por la rotación.
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _httpClient.SendAsync(request);
        }

        private static bool EsCuotaAgotada(HttpStatusCode statusCode) =>
            statusCode is HttpStatusCode.Unauthorized
                or HttpStatusCode.Forbidden
                or HttpStatusCode.TooManyRequests;
    }
}
