using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Infrastructure.Interfaces
{
    public interface ICorreoConfigRepository
    {
        Task<CorreoConfigInicialDto> GetInicialAsync();
        Task UpdateReglasAsync(string eventoCodigo, CorreoReglasUpdateDto dto);
    }
}
