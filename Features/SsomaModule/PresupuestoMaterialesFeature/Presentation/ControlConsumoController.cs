using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma/presupuesto-materiales/control")]
[Authorize]
[RequireFeature("ssoma.gestion.presupuesto-materiales")]
public class ControlConsumoController : ControllerBase
{
    private readonly IControlConsumoService _service;
    public ControlConsumoController(IControlConsumoService service) => _service = service;

    private int? UserId => int.TryParse(
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    /// <summary>
    /// Abre una nueva semana de control para el presupuesto indicado.
    /// El número de semana se asigna automáticamente (correlativo).
    /// </summary>
    [HttpPost("semanas")]
    public async Task<IActionResult> AbrirSemana([FromBody] AbrirSemanaDto dto)
    {
        try
        {
            var resultado = await _service.AbrirSemanaAsync(dto, UserId);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception)         { return StatusCode(500, new { message = "Error al abrir semana." }); }
    }

    /// <summary>
    /// Registra o actualiza el consumo de materiales de una semana.
    /// Solo se acepta si la semana está ABIERTA.
    /// Enviar solo las familias con consumo &gt; 0.
    /// </summary>
    [HttpPut("semanas/{controlId}/consumo")]
    public async Task<IActionResult> RegistrarConsumo(
        int controlId, [FromBody] List<RegistrarConsumoLineaDto> lineas)
    {
        try
        {
            var resultado = await _service.RegistrarConsumoAsync(controlId, lineas);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception)         { return StatusCode(500, new { message = "Error al registrar consumo." }); }
    }

    /// <summary>Cierra la semana de control. No permite más modificaciones.</summary>
    [HttpPost("semanas/{controlId}/cerrar")]
    public async Task<IActionResult> CerrarSemana(int controlId)
    {
        try
        {
            var resultado = await _service.CerrarSemanaAsync(controlId);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception)         { return StatusCode(500, new { message = "Error al cerrar semana." }); }
    }

    /// <summary>Detalle de una semana de control con sus líneas de consumo.</summary>
    [HttpGet("semanas/{controlId}")]
    public async Task<IActionResult> ObtenerSemana(int controlId)
    {
        try
        {
            var resultado = await _service.ObtenerSemanaAsync(controlId);
            if (resultado is null) return NotFound(new { message = "Semana no encontrada." });
            return Ok(resultado);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener semana." }); }
    }

    /// <summary>Lista todas las semanas registradas para un presupuesto.</summary>
    [HttpGet("presupuestos/{presupuestoId}/semanas")]
    public async Task<IActionResult> ListarSemanas(int presupuestoId)
    {
        try { return Ok(await _service.ListarSemanasAsync(presupuestoId)); }
        catch (Exception) { return StatusCode(500, new { message = "Error al listar semanas." }); }
    }

    /// <summary>
    /// Dashboard en tiempo real: consumo acumulado vs presupuesto por familia y tipo.
    /// Incluye semáforo (OK / ADVERTENCIA ≥80% / ALERTA ≥100%) para cada familia.
    /// </summary>
    [HttpGet("presupuestos/{presupuestoId}/dashboard")]
    public async Task<IActionResult> Dashboard(int presupuestoId)
    {
        try
        {
            var resultado = await _service.ObtenerDashboardAsync(presupuestoId);
            if (resultado is null) return NotFound(new { message = "Presupuesto no encontrado." });
            return Ok(resultado);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener dashboard." }); }
    }
}
