using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CitaMedica;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface ICitaMedicaRepository
    {
        Task<List<CitaMedicaListItemDto>> GetByAccidenteId(int accidenteId);
        Task<CitaMedicaListItemDto> GetById(int id);
        Task<List<(int Id, string Nombre)>> GetTipos();
        Task<int> Create(int accidenteId, CitaMedicaCreateDto dto, int registradoPorId);
        Task Update(int id, CitaMedicaUpdateDto dto);
        Task Delete(int id);
    }
}
