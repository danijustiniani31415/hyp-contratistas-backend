using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;

/// <summary>Resultado ya decidido para una línea, pendiente de persistir — junto a otros miles en
/// AplicarResultadosEstandarizacionAsync, una sola vez, en vez de un SaveChanges por línea.</summary>
public record ResultadoLineaParaGuardar(
    long LineaId, bool Estandarizado, int? ItemId, bool PerteneceSsoma,
    string? MetodoMatch, decimal? ScoreMatch, string? EstadoRevision, decimal FactorConversion);

public interface IConsumoRepository
{
    Task<SsConsumoCarga> CrearCargaAsync(SsConsumoCarga carga);
    /// <summary>Líneas activas con guía de origen conocida (carga acumulativa), para matchear contra el archivo nuevo.</summary>
    Task<List<SsConsumoLinea>> ObtenerLineasActivasConGuiaPorProyectoAsync(int projectId);
    /// <summary>
    /// Aplica en una sola transacción el diff entre la carga acumulada nueva y lo ya guardado:
    /// inserta las líneas nuevas, actualiza cantidad/precio de las regularizadas (conservando su
    /// clasificación de catálogo) y da de baja (activo=false) las que ya no aparecen en el archivo.
    /// </summary>
    Task AplicarDiffCargaAsync(
        IEnumerable<SsConsumoLinea> nuevas,
        IEnumerable<(long LineaId, decimal Cantidad, decimal PrecioUnitario, decimal PrecioTotal, DateOnly FechaGuia)> actualizaciones,
        IEnumerable<long> idsDarDeBaja,
        string motivoBaja);
    Task<List<SsConsumoLinea>> ObtenerLineasSinEstandarizarAsync(int cargaId);
    Task ActualizarLineaEstandarizadaAsync(long lineaId, int itemId, bool perteneceSsoma, string metodo, decimal score, string? estadoRevision, decimal factorConversion = 1);
    /// <summary>Ninguna estrategia automática encontró candidato: la marca PENDIENTE sin item sugerido, para que aparezca en Revisión y se le asigne uno a mano.</summary>
    Task MarcarSinMatchAsync(long lineaId);
    /// <summary>Ya se sabe (alias de rechazo) que este texto no es SSOMA: rechaza directo, sin pasar por Revisión.</summary>
    Task MarcarRechazadoAutomaticoAsync(long lineaId);
    /// <summary>Persiste en una sola operación el resultado ya decidido de un lote entero de líneas
    /// (usado por EstandarizarCargaAsync) — evita un SaveChanges por línea en lotes grandes.</summary>
    Task AplicarResultadosEstandarizacionAsync(List<ResultadoLineaParaGuardar> resultados);
    Task ActualizarContadoresCargaAsync(int cargaId, int estandarizadas, int pendientes);
    Task ActualizarResumenCargaAsync(int cargaId, int totalLineas, int nuevas, int actualizadas, int eliminadas);
    Task<List<ConsumoCargaResumenDto>> ObtenerCargasPorProyectoAsync(int projectId);
    Task<List<MaterialPendienteDto>> ObtenerPendientesRevisionAsync(int projectId);
    Task<List<MaterialPendienteGlobalDto>> ObtenerPendientesRevisionGlobalAsync();
    Task<List<MaterialNoSsomaDto>> ObtenerNoSsomaAsync();
    Task<SsConsumoLinea?> ObtenerLineaPorIdAsync(long lineaId);
    Task ActualizarRevisionAsync(long lineaId, string decision, int? itemIdConfirmado);
    Task<int> AsignarHitosPorFechaAsync(int projectId);
}
