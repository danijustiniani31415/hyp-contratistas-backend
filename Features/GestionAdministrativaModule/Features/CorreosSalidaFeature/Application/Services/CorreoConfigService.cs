using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Infrastructure.Interfaces;

namespace Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Services
{
    /// <summary>Wrapper delgado sobre el repositorio de configuración de correos.</summary>
    public class CorreoConfigService : ICorreoConfigService
    {
        private readonly ICorreoConfigRepository _repo;

        public CorreoConfigService(ICorreoConfigRepository repo) => _repo = repo;

        public Task<CorreoConfigInicialDto> GetInicialAsync() => _repo.GetInicialAsync();

        public Task UpdateReglasAsync(string eventoCodigo, CorreoReglasUpdateDto dto) =>
            _repo.UpdateReglasAsync(eventoCodigo, dto);
    }
}
