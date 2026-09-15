using Abril_Backend.Features.AlmacenModule.Application.Dtos;

namespace Abril_Backend.Features.AlmacenModule.Application.Interfaces
{
    public interface IAlmacenKardexService
    {
        Task<StockListResponseDto> ListStock(int? almacenId, string? search, int page, int pageSize);
        Task<MovimientoListResponseDto> ListMovimientos(int? almacenId, long? productoId, int page, int pageSize);

        /// <summary>Registra INGRESO o SALIDA y actualiza lb_stock en una sola transacción con bloqueo de fila.</summary>
        Task RegistrarMovimiento(RegistrarMovimientoDto dto, long? usuarioSistemaId);

        Task AjustarUmbrales(AjustarUmbralesDto dto);
    }
}
