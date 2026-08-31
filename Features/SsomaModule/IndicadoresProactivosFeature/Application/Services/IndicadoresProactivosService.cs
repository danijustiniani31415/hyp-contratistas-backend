using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Interfaces;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Services;

public class IndicadoresProactivosService : IIndicadoresProactivosService
{
    private readonly IIndicadoresProactivosRepository _repo;
    private readonly IChecklistRepository _checklistRepo;

    public IndicadoresProactivosService(
        IIndicadoresProactivosRepository repo,
        IChecklistRepository checklistRepo)
    {
        _repo = repo;
        _checklistRepo = checklistRepo;
    }

    public Task<bool> EsCoordinadorSsomaAsync(int userId)
        => _repo.EsCoordinadorSsomaAsync(userId);

    public Task<HashSet<int>> GetEmpresaExcluidaIdsAsync()
        => _repo.GetEmpresaExcluidaIdsAsync();

    public Task OcultarEmpresaAsync(int empresaId, string? motivo, int userId)
        => _repo.OcultarEmpresaAsync(empresaId, motivo, userId);

    public Task MostrarEmpresaAsync(int empresaId)
        => _repo.MostrarEmpresaAsync(empresaId);

    public Task<List<InspeccionTipoDto>> GetTiposInspeccionAsync()
        => _repo.GetTiposInspeccionAsync();

    public Task<ProgInspeccionResumenDto> GetProgInspeccionAsync(int proyectoId, int mes, int anio)
        => _repo.GetProgInspeccionAsync(proyectoId, mes, anio);

    public Task GuardarProgInspeccionAsync(GuardarProgInspeccionRequest request, int userId)
        => _repo.GuardarProgInspeccionAsync(request, userId);

    public async Task<IndicadorProactivoProyectoDto> GetIndicadoresProyectoAsync(int proyectoId, int mes, int anio)
    {
        var empresas = await _repo.GetMetasEmpresaAsync(proyectoId, mes, anio);
        var activas = empresas.Where(e => e.EsActiva).ToList();

        var resumenChecklists = await _checklistRepo.GetResumenProyectoAsync(proyectoId);
        var checklists = resumenChecklists.Checklists.Select(c => new ChecklistResumenDto(
            c.ChecklistProyectoId,
            c.NombrePlantilla,
            c.PorcentajeCompletado,
            c.Estado,
            c.EsObligatorio,
            c.TotalItems,
            c.ItemsCompletados
        )).ToList();

        // Misma fórmula que el seguimiento de todos los proyectos — antes esta pantalla
        // promediaba los % POR EMPRESA y la otra agregaba los totales, así que el mismo
        // proyecto y mes podía mostrar dos porcentajes distintos según dónde se mirara.
        return IndicadorProactivoCalculo.ConstruirProyecto(proyectoId, "", activas)
            with { Checklists = checklists };
    }

    public Task<List<IndicadorProactivoProyectoDto>> GetSeguimientoTodosProyectosAsync(int mes, int anio)
        => _repo.GetSeguimientoTodosProyectosAsync(mes, anio);

    public Task<PuntajeMesDto> GetPuntajeMesAsync(int proyectoId, int mes, int anio)
        => _repo.GetPuntajeMesAsync(proyectoId, mes, anio);

    public Task<List<PuntajeMesDto>> GetPuntajeTodosProyectosAsync(
        int mes, int anio, List<IndicadorProactivoProyectoDto>? seguimiento = null)
        => _repo.GetPuntajeTodosProyectosAsync(mes, anio, seguimiento);

    public Task<IndicadorReactivoProyectoDto> GetIndicadoresReactivosAsync(int proyectoId, int mes, int anio)
        => _repo.GetIndicadoresReactivosAsync(proyectoId, mes, anio);

    public Task<List<IndicadorReactivoProyectoDto>> GetIndicadoresReactivosTodosAsync(int mes, int anio)
        => _repo.GetIndicadoresReactivosTodosAsync(mes, anio);

    public Task<MetaAnualDto> GetMetaAnualAsync(int anio)
        => _repo.GetMetaAnualAsync(anio);

    public Task<MetaAnualDto> GuardarMetaAnualAsync(GuardarMetaAnualRequest request, int userId)
        => _repo.GuardarMetaAnualAsync(request, userId);
}
