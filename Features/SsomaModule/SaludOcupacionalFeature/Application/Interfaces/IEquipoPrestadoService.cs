using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.EquipoPrestado;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IEquipoPrestadoService
    {
        Task<List<EquipoPrestadoListItemDto>> GetByAccidenteId(int accidenteId);
        Task<List<(int Id, string Nombre)>> GetTipos();
        Task<int> Create(int accidenteId, EquipoPrestadoCreateDto dto, int? userId);
        Task Devolver(int id, EquipoPrestadoDevolverDto dto);
        Task Delete(int id);
    }
}
