using Abril_Backend.Features.ComprasModule.Application.Dtos;

namespace Abril_Backend.Features.ComprasModule.Application.Interfaces
{
    public interface IComprasService
    {
        Task<List<ProveedorDto>> ListProveedores();
        Task<ProveedorDto> CrearProveedor(ProveedorCreateDto dto);

        Task<OrdenCompraDetailDto> Crear(OrdenCompraCreateDto dto, long solicitadoPorId);
        Task<OrdenCompraListResponseDto> List(string? estado, int page, int pageSize);
        Task<OrdenCompraDetailDto> GetById(long id);

        /// <summary>Registra una recepción parcial o total de UN ítem — repone stock por lo recibido.</summary>
        Task<OrdenCompraDetailDto> RecibirItem(long ordenId, long itemId, RecibirItemDto dto, long recibidoPorId);
        Task<OrdenCompraDetailDto> Cancelar(long id);
    }
}
