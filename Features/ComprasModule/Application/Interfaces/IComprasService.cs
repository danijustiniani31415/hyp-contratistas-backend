using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule;

namespace Abril_Backend.Features.ComprasModule.Application.Interfaces
{
    public interface IComprasService
    {
        Task<List<ProveedorDto>> ListProveedores();
        Task<ProveedorDto> CrearProveedor(ProveedorCreateDto dto);

        Task<OrdenCompraDetailDto> Crear(OrdenCompraCreateDto dto, long solicitadoPorId);
        Task<OrdenCompraListResponseDto> List(string? search, string? estado, HashSet<int>? proyectosPermitidos, int page, int pageSize);
        Task<OrdenCompraDetailDto> GetById(long id);

        /// <summary>Registra una recepción parcial o total de UN ítem — repone stock por lo recibido.</summary>
        Task<OrdenCompraDetailDto> RecibirItem(long ordenId, long itemId, RecibirItemDto dto, long recibidoPorId, LbScopeProyectos scope);
        Task<OrdenCompraDetailDto> Cancelar(long id, LbScopeProyectos scope);
    }
}
