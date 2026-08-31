using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Interfaces;

public interface IIndicadoresProactivosService
{
    // ── Ocultar/mostrar empresas en el seguimiento ────────────────────────────
    Task<bool> EsCoordinadorSsomaAsync(int userId);
    Task<HashSet<int>> GetEmpresaExcluidaIdsAsync();
    Task OcultarEmpresaAsync(int empresaId, string? motivo, int userId);
    Task MostrarEmpresaAsync(int empresaId);

    Task<List<InspeccionTipoDto>> GetTiposInspeccionAsync();
    Task<ProgInspeccionResumenDto> GetProgInspeccionAsync(int proyectoId, int mes, int anio);
    Task GuardarProgInspeccionAsync(GuardarProgInspeccionRequest request, int userId);
    Task<IndicadorProactivoProyectoDto> GetIndicadoresProyectoAsync(int proyectoId, int mes, int anio);
    Task<List<IndicadorProactivoProyectoDto>> GetSeguimientoTodosProyectosAsync(int mes, int anio);
    Task<PuntajeMesDto> GetPuntajeMesAsync(int proyectoId, int mes, int anio);

    /// <summary>
    /// Si <paramref name="seguimiento"/> se provee, se reutiliza en vez de recalcular.
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
