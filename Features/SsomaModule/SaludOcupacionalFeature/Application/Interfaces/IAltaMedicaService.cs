using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AltaMedica;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IAltaMedicaService
    {
        Task<AltaMedicaDto?> GetByAccidenteId(int accidenteId);
        Task<List<(int Id, string Nombre)>> GetTipos();
        Task<int> Create(int accidenteId, AltaMedicaCreateDto dto, int? userId);
        Task Update(int accidenteId, AltaMedicaUpdateDto dto);
        Task Delete(int accidenteId);
    }
}
