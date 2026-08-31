using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.EquipoPrestado;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface IEquipoPrestadoRepository
    {
        Task<List<EquipoPrestadoListItemDto>> GetByAccidenteId(int accidenteId);
        Task<List<(int Id, string Nombre)>> GetTipos();
        Task<int> Create(int accidenteId, EquipoPrestadoCreateDto dto, int registradoPorId);
        Task Devolver(int id, EquipoPrestadoDevolverDto dto);
        Task Delete(int id);
    }
}
