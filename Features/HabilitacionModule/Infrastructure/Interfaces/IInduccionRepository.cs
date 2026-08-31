using Abril_Backend.Features.Habilitacion.Application.Dtos.Inducciones;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IInduccionRepository
    {
        Task<List<int>> CreateAsync(InduccionCreateDto dto, int programadoPor);
        Task<List<InduccionListDto>> GetAsync(
            int? proyectoId, int? empresaId, string? estado,
            DateTime? fechaDesde, DateTime? fechaHasta);
        Task<List<InduccionTrabajadorDto>> GetTrabajadoresPorProgramarAsync(int? empresaId, int proyectoId, string? search = null);
        Task AprobarAsync(int id);
        Task AprobarBatchAsync(List<int> ids);
        Task RechazarAsync(int id);
        Task<int> ResetFaltaAsync();
    }
}
