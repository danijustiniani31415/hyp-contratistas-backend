using Abril_Backend.Features.PedidosModule.Application.Dtos;

namespace Abril_Backend.Features.PedidosModule.Application.Interfaces
{
    public interface IPedidoService
    {
        Task<PedidoDetailDto> Crear(PedidoCreateDto dto, long solicitanteId);

        /// <summary>soloPropios=true restringe a los pedidos creados por usuarioSistemaId (para quien no tiene PEDIDO_VER_TODOS).</summary>
        Task<PedidoListResponseDto> List(string? estado, int? proyectoId, bool soloPropios, long usuarioSistemaId, int page, int pageSize);
        Task<PedidoDetailDto> GetById(long id);

        Task<PedidoDetailDto> Aprobar(long id, long aprobadorId);
        Task<PedidoDetailDto> Rechazar(long id, long aprobadorId, RechazarPedidoDto dto);
        Task<PedidoDetailDto> Entregar(long id, long entregadorId);
        Task<PedidoDetailDto> Cancelar(long id, long solicitanteId);
    }
}
