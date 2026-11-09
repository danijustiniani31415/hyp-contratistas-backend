using Abril_Backend.Features.AlmacenModule.Application.Dtos;

namespace Abril_Backend.Features.AlmacenModule.Application.Interfaces
{
    public interface IAlmacenKardexService
    {
        Task<StockListResponseDto> ListStock(int? almacenId, string? search, int page, int pageSize, HashSet<int>? almacenes = null);
        Task<MovimientoListResponseDto> ListMovimientos(int? almacenId, long? productoId, int page, int pageSize, HashSet<int>? almacenes = null);

        /// <summary>Registra INGRESO o SALIDA y actualiza lb_stock en una sola transacción con bloqueo de fila.</summary>
        Task RegistrarMovimiento(RegistrarMovimientoDto dto, long? usuarioSistemaId, HashSet<int>? almacenes = null);

        Task AjustarUmbrales(AjustarUmbralesDto dto, HashSet<int>? almacenes = null);

        /// <summary>Productos con CantidadActual bajo StockMinimo — alimenta el panel de Logística
        /// para reponer aunque no haya un pedido puntual detrás.</summary>
        Task<List<ReposicionSugeridaDto>> ListReposicionSugerida(int? almacenId, HashSet<int>? almacenes = null);
    }
}
