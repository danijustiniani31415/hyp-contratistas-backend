using System.Net.Http.Json;
using Abril_Backend.Shared.Services.Decolecta.Interfaces;
using Abril_Backend.Shared.Services.Sunat.Dtos;
using Abril_Backend.Shared.Services.Sunat.Interfaces;

namespace Abril_Backend.Shared.Services.Sunat.Providers.Decolecta
{
    public class DecolectaSunatService : ISunatService
    {
        private readonly IDecolectaApiClient _api;
        private readonly ILogger<DecolectaSunatService> _logger;

        public DecolectaSunatService(IDecolectaApiClient api, ILogger<DecolectaSunatService> logger)
        {
            _api = api;
            _logger = logger;
        }

        public async Task<SunatContributorDto?> GetByRucAsync(string ruc)
        {
            var requestUrl = $"/v1/sunat/ruc/full?numero={ruc}";
            _logger.LogInformation("[Sunat] GET {Url}", requestUrl);

            // La rotación de tokens y el error de cuota agotada (503) los maneja IDecolectaApiClient.
            using var response = await _api.GetAsync(requestUrl,
                "Se agotaron las consultas disponibles del servicio de consulta de RUC. Por favor, contacte con el administrador del sistema.");

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[Sunat] HTTP {StatusCode} para RUC {Ruc}. Body: {Body}",
                    (int)response.StatusCode, ruc, body);
                return null;
            }

            var data = await response.Content.ReadFromJsonAsync<DecolectaRucResponse>();
            if (data is null)
            {
                _logger.LogWarning("[Sunat] Respuesta vacía para RUC {Ruc}", ruc);
                return null;
            }

            return DecolectaRucMapper.ToSunatContributorDto(data);
        }
    }
}
