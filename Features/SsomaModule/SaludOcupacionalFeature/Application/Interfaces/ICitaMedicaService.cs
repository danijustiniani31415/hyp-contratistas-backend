using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CitaMedica;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface ICitaMedicaService
    {
        Task<List<CitaMedicaListItemDto>> GetByAccidenteId(int accidenteId);
        Task<List<(int Id, string Nombre)>> GetTipos();
        Task<int> Create(int accidenteId, CitaMedicaCreateDto dto, int? userId);
        Task Update(int id, CitaMedicaUpdateDto dto);
        Task Delete(int id);
    }
}
