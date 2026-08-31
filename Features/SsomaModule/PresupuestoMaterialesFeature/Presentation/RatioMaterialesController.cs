using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma/presupuesto-materiales/ratios")]
[Authorize]
[RequireFeature("ssoma.gestion.presupuesto-materiales")]
public class RatioMaterialesController : ControllerBase
{
    private readonly IRatioService _ratioService;
    private readonly IDriversService _driversService;

    public RatioMaterialesController(IRatioService ratioService, IDriversService driversService)
    {
        _ratioService   = ratioService;
        _driversService = driversService;
    }

    // ─── Drivers ────────────────────────────────────────────────────────────

    /// <summary>Lista todos los proyectos con sus drivers (HH, área, trabajadores) y fuente.</summary>
    [HttpGet("proyectos/drivers")]
    public async Task<IActionResult> ObtenerDrivers()
    {
        try { return Ok(await _driversService.ObtenerTodosAsync()); }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener drivers." }); }
    }

    /// <summary>
    /// Actualiza los drivers de un proyecto y recalcula sus ratios automáticamente.
    /// Úsalo cuando un proyecto culmina y tienes los valores reales finales (HH_REAL),
    /// o para actualizar la proyección de un proyecto activo (HH_PROYECTADO).
    /// </summary>
    [HttpPut("proyectos/{projectId}/drivers")]
    public async Task<IActionResult> ActualizarDrivers(int projectId, [FromBody] ActualizarDriversDto dto)
    {
        try
        {
            var resultado = await _driversService.ActualizarYRecalcularAsync(projectId, dto);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error al actualizar drivers." }); }
    }

    /// <summary>Calcula y guarda los ratios de un proyecto a partir de sus consumos históricos.</summary>
    [HttpPost("proyectos/{projectId}/calcular")]
    public async Task<IActionResult> Calcular(int projectId)
    {
        try
        {
            var resultado = await _ratioService.CalcularRatiosProyectoAsync(projectId);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error al calcular ratios." }); }
    }

    /// <summary>Calcula ratios de TODOS los proyectos con consumo SSOMA estandarizado de una sola vez
    /// (en vez de entrar proyecto por proyecto) — útil tras estandarizar varios históricos seguidos.</summary>
    [HttpPost("proyectos/calcular-todos")]
    public async Task<IActionResult> CalcularTodos()
    {
        try
        {
            var resultado = await _ratioService.CalcularRatiosTodosLosProyectosAsync();
            return Ok(resultado);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al calcular ratios de todos los proyectos." }); }
    }

    /// <summary>Obtiene los ratios calculados de un proyecto.</summary>
    [HttpGet("proyectos/{projectId}")]
    public async Task<IActionResult> ObtenerPorProyecto(int projectId)
    {
        try { return Ok(await _ratioService.ObtenerRatiosProyectoAsync(projectId)); }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener ratios." }); }
    }

    /// <summary>Lista las familias que ya tienen algún ratio calculado, para elegir cuál comparar.</summary>
    [HttpGet("familias")]
    public async Task<IActionResult> ListarFamilias()
    {
        try { return Ok(await _ratioService.ListarFamiliasConRatioAsync()); }
        catch (Exception) { return StatusCode(500, new { message = "Error al listar familias." }); }
    }

    /// <summary>Compara el ratio de una familia entre todos los proyectos históricos.</summary>
    [HttpGet("familias/{familiaId}/comparacion")]
    public async Task<IActionResult> ComparacionFamilia(int familiaId)
    {
        try
        {
            var resultado = await _ratioService.ObtenerComparacionFamiliaAsync(familiaId);
            if (resultado == null) return NotFound(new { message = "No hay ratios calculados para esta familia." });
            return Ok(resultado);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al comparar familia." }); }
    }

    /// <summary>
    /// Incluye o excluye manualmente un proyecto del cálculo del ratio recomendado de una familia
    /// (independiente del flag automático de outlier).
    /// </summary>
    [HttpPatch("familias/{familiaId}/proyectos/{projectId}/incluir")]
    public async Task<IActionResult> ActualizarIncluidoManual(int familiaId, int projectId, [FromBody] ActualizarIncluidoManualDto dto)
    {
        try
        {
            await _ratioService.ActualizarIncluidoManualAsync(familiaId, projectId, dto.Incluir, dto.Campo);
            return Ok(new { familiaId, projectId, incluido = dto.Incluir, campo = dto.Campo });
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al actualizar inclusión." }); }
    }

    /// <summary>Resumen general de todos los proyectos con ratios calculados.</summary>
    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen()
    {
        try { return Ok(await _ratioService.ObtenerResumenAsync()); }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener resumen." }); }
    }
}
