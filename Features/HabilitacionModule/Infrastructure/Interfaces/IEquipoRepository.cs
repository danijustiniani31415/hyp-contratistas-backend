using Abril_Backend.Features.Habilitacion.Application.Dtos.Equipos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IEquipoRepository
    {
        Task<(List<EquipoListDto> Items, int Total)> GetPagedAsync(
            int? proyectoId, int? empresaId, string? search,
            bool? activo, int page, int pageSize);

        Task<SsEquipo?> GetByIdAsync(int id);
        Task<SsEquipo> CreateAsync(EquipoCreateDto dto);
        Task<SsEquipo> UpdateAsync(int id, EquipoCreateDto dto);
        Task<List<EquipoEntregableDto>> GetEntregablesAsync(int equipoId);
        Task<SsHabEquipo> UpdateEntregableAsync(int id, EquipoEntregableUpdateDto dto, int? userId, int? empresaId = null);
        Task<List<SsHabDocumentoVersionEquipoDto>> GetVersionesAsync(int habEquipoId);
    }
}
