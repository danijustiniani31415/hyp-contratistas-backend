using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule.Application.Interfaces
{
    public interface IRolPermisoService
    {
        Task<List<RolListItemDto>> ListRoles();
        Task<List<PermisoDto>> ListPermisos();
        Task<RolDetalleDto> GetRolDetalle(short rolId);
        Task<RolDetalleDto> ActualizarPermisos(short rolId, ActualizarPermisosRolDto dto);
    }
}
