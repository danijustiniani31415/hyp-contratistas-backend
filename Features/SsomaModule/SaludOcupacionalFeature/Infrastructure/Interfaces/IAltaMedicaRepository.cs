using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AltaMedica;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface IAltaMedicaRepository
    {
        Task<AltaMedicaDto?> GetByAccidenteId(int accidenteId);
        Task<List<(int Id, string Nombre)>> GetTipos();
        Task<int> Create(int accidenteId, AltaMedicaCreateDto dto, int registradoPorId);
        Task Update(int accidenteId, AltaMedicaUpdateDto dto);
        Task Delete(int accidenteId);
    }
}
