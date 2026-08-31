using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma/presupuesto-materiales/presupuestos")]
[Authorize]
[RequireFeature("ssoma.gestion.presupuesto-materiales")]
public class PresupuestoMaterialesController : ControllerBase
{
    private readonly IPresupuestoService _service;
    public PresupuestoMaterialesController(IPresupuestoService service) => _service = service;

    private int? UserId => int.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    /// <summary>
    /// Genera un nuevo presupuesto para el proyecto usando los ratios históricos recomendados.
    /// Los drivers (HH, Área, Trabajadores) se toman del proyecto; se pueden sobreescribir en el body.
    /// </summary>
    [HttpPost("proyectos/{projectId}/generar")]
    public async Task<IActionResult> Generar(int projectId, [FromBody] GenerarPresupuestoDto dto)
    {
        try
        {
            var resultado = await _service.GenerarAsync(projectId, dto, UserId);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception)         { return StatusCode(500, new { message = "Error al generar presupuesto." }); }
    }

    /// <summary>Lista todos los presupuestos generados para un proyecto (versiones).</summary>
    [HttpGet("proyectos/{projectId}")]
    public async Task<IActionResult> ListarPorProyecto(int projectId)
    {
        try { return Ok(await _service.ObtenerPorProyectoAsync(projectId)); }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener presupuestos." }); }
    }

    /// <summary>Devuelve el detalle completo de un presupuesto con todas sus líneas por tipo.</summary>
    [HttpGet("{presupuestoId}")]
    public async Task<IActionResult> ObtenerDetalle(int presupuestoId)
    {
        try
        {
            var resultado = await _service.ObtenerDetalleAsync(presupuestoId);
            if (resultado is null) return NotFound(new { message = "Presupuesto no encontrado." });
            return Ok(resultado);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener detalle." }); }
    }

    /// <summary>
    /// Sobreescribe manualmente la cantidad y/o precio de una línea del presupuesto.
    /// Se usa cuando el responsable SSOMA ajusta un valor por criterio propio.
    /// </summary>
    [HttpPut("{presupuestoId}/lineas/{lineaId}")]
    public async Task<IActionResult> ActualizarLinea(
        int presupuestoId, int lineaId, [FromBody] ActualizarLineaPresupuestoDto dto)
    {
        try
        {
            var resultado = await _service.ActualizarLineaAsync(presupuestoId, lineaId, dto);
            return Ok(resultado);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al actualizar línea." }); }
    }

    /// <summary>Aprueba el presupuesto (cambia estado de BORRADOR a APROBADO).</summary>
    [HttpPost("{presupuestoId}/aprobar")]
    public async Task<IActionResult> Aprobar(int presupuestoId)
    {
        try
        {
            var estado = await _service.AprobarAsync(presupuestoId);
            return Ok(new { estado });
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al aprobar presupuesto." }); }
    }
}
