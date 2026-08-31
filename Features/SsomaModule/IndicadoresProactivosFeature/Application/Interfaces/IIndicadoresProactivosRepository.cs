using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Interfaces;

public interface IIndicadoresProactivosRepository
{
    // ── Ocultar/mostrar empresas en el seguimiento ────────────────────────────
    Task<bool> EsCoordinadorSsomaAsync(int userId);
    Task<HashSet<int>> GetEmpresaExcluidaIdsAsync();
    Task OcultarEmpresaAsync(int empresaId, string? motivo, int userId);
    Task MostrarEmpresaAsync(int empresaId);

    // ── Programación de inspecciones ──────────────────────────────────────────

    Task<List<InspeccionTipoDto>> GetTiposInspeccionAsync();

    Task<ProgInspeccionResumenDto> GetProgInspeccionAsync(int proyectoId, int mes, int anio);

    Task GuardarProgInspeccionAsync(GuardarProgInspeccionRequest request, int userId);

    // ── Cálculo de metas e indicadores ───────────────────────────────────────

    /// <summary>
    /// Devuelve metas y actuals por empresa para un proyecto y período.
    /// Solo incluye empresas activas (>= 10 días en el mes).
    /// Si mes/anio corresponde al mes actual, proyecta la meta al cierre.
    /// </summary>
    Task<List<MetaEmpresaDto>> GetMetasEmpresaAsync(int proyectoId, int mes, int anio);

    /// <summary>
    /// Devuelve el resumen de indicadores proactivos de todos los proyectos activos
    /// para el dashboard de seguimiento (vista competitiva).
    /// </summary>
    Task<List<IndicadorProactivoProyectoDto>> GetSeguimientoTodosProyectosAsync(int mes, int anio);

    // ── Puntaje del mes ───────────────────────────────────────────────────────

    Task<PuntajeMesDto> GetPuntajeMesAsync(int proyectoId, int mes, int anio);

    /// <summary>
    /// Si <paramref name="seguimiento"/> se provee, se reutiliza en vez de recalcular
    /// (evita repetir las 8 bulk queries de <see cref="GetSeguimientoTodosProyectosAsync"/>).
    /// </summary>
    Task<List<PuntajeMesDto>> GetPuntajeTodosProyectosAsync(
        int mes, int anio, List<IndicadorProactivoProyectoDto>? seguimiento = null);

    // ── Indicadores reactivos IF / IG / IA ───────────────────────────────────
    Task<IndicadorReactivoProyectoDto> GetIndicadoresReactivosAsync(int proyectoId, int mes, int anio);
    Task<List<IndicadorReactivoProyectoDto>> GetIndicadoresReactivosTodosAsync(int mes, int anio);

    // ── Meta anual de reactivos ───────────────────────────────────────────────
    Task<MetaAnualDto> GetMetaAnualAsync(int anio);
    Task<MetaAnualDto> GuardarMetaAnualAsync(GuardarMetaAnualRequest request, int userId);
}
