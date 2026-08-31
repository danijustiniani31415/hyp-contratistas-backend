using System.Net.Http.Json;
using Abril_Backend.Shared.Services.Decolecta.Interfaces;
using Abril_Backend.Shared.Services.Reniec.Interfaces;
using Abril_Backend.Shared.Services.Reniec.Dtos;

namespace Abril_Backend.Shared.Services.Reniec.Services
{
    public class ReniecService : IReniecService
    {
        private readonly IDecolectaApiClient _api;

        public ReniecService(IDecolectaApiClient api)
        {
            _api = api;
        }

        public async Task<ReniecPersonDto?> GetByDniAsync(string dni)
        {
            // La rotación de tokens y el error de cuota agotada (503) los maneja IDecolectaApiClient.
            using var response = await _api.GetAsync($"/v1/reniec/dni?numero={dni}",
                "Se agotaron las consultas disponibles del servicio de consulta de DNI. Por favor, contacte con el administrador del sistema.");

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ReniecPersonDto>();
        }
    }
}
