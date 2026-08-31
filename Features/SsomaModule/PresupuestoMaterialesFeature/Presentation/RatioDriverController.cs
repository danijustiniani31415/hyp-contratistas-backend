using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation;

/// <summary>
/// Ratios historicos de HH y N Trabajadores por m2 de area techada, calculados a partir de
/// los proyectos que ya tienen esos drivers cargados. Se usa para sugerir HH/Trabajadores al
/// generar el presupuesto de un proyecto nuevo — es siempre una sugerencia editable, nunca se
/// aplica solo: el responsable confirma o ajusta el valor a mano antes de generar.
/// </summary>
[ApiController]
[Route("api/v1/ssoma/presupuesto-materiales/ratios/drivers")]
[Authorize]
[RequireFeature("ssoma.gestion.presupuesto-materiales")]
public class RatioDriverController : ControllerBase
{
    private readonly IRatioDriverService _service;
    public RatioDriverController(IRatioDriverService service) => _service = service;

    /// <summary>Recalcula los ratios HH/m2 y Trabajadores/m2 de todos los proyectos con área techada cargada.</summary>
    [HttpPost("calcular")]
    public async Task<IActionResult> Calcular()
    {
        try { return Ok(await _service.CalcularRatiosAsync()); }
        catch (Exception) { return StatusCode(500, new { message = "Error al calcular los ratios de HH/Trabajadores." }); }
    }

    /// <summary>Comparación entre proyectos para un tipo de driver (HH | TRABAJADORES), con mediana/min/max.</summary>
    [HttpGet("{tipo}")]
    public async Task<IActionResult> ObtenerComparacion(string tipo)
    {
        try { return Ok(await _service.ObtenerComparacionAsync(tipo)); }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener la comparación de drivers." }); }
    }

    /// <summary>Incluye o excluye manualmente un proyecto del cálculo del ratio recomendado (criterio del responsable).</summary>
    [HttpPatch("{tipo}/proyectos/{projectId}/incluir")]
    public async Task<IActionResult> ActualizarIncluidoManual(string tipo, int projectId, [FromBody] ActualizarIncluidoManualDriverDto dto)
    {
        try
        {
            await _service.ActualizarIncluidoManualAsync(tipo, projectId, dto.Incluir);
            return Ok(new { tipo, projectId, incluido = dto.Incluir });
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al actualizar inclusión." }); }
    }

    /// <summary>Ratio recomendado (mediana) de HH y Trabajadores por m2 — para sugerir valores al generar un presupuesto nuevo.</summary>
    [HttpGet("recomendados")]
    public async Task<IActionResult> ObtenerRecomendados()
    {
        try { return Ok(await _service.ObtenerRecomendadosAsync()); }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener los ratios recomendados." }); }
    }
}
