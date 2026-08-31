using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

public interface IPresupuestoRepository
{
    Task<List<RatioRecomendadoDto>> ObtenerRatiosRecomendadosAsync();
    Task<int> SiguienteVersionAsync(int projectId);
    Task<int> CrearPresupuestoAsync(int projectId, int version, decimal hh, decimal area,
        int trabajadores, decimal total, int? generadoPor, string? notas);
    Task InsertarLineasAsync(int presupuestoId, IEnumerable<PresupuestoLineaDto> lineas);
    Task ActualizarTotalAsync(int presupuestoId, decimal total);
    Task<PresupuestoDetalleDto?> ObtenerDetalleAsync(int presupuestoId);
    Task<List<PresupuestoResumenDto>> ObtenerPorProyectoAsync(int projectId);
    Task ActualizarLineaAsync(int lineaId, decimal? cantidadManual, decimal? precioManual, string? notas);
    Task<string> AprobarAsync(int presupuestoId);
}
