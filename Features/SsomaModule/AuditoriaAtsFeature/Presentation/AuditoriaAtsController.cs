using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma-auditoria-ats")]
[Authorize]
[RequireFeature("ssoma.gestion.auditoria-ats")]
public class AuditoriaAtsController : ControllerBase
{
    private readonly IAuditoriaAtsService _service;
    private readonly ILogger<AuditoriaAtsController> _logger;

    public AuditoriaAtsController(IAuditoriaAtsService service, ILogger<AuditoriaAtsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int? GetEmpresaIdContratista() =>
        User.FindFirst("tipo")?.Value == "CONTRATISTA"
            && int.TryParse(User.FindFirst("empresaId")?.Value, out var id)
            ? id
            : null;

    [HttpGet("preguntas")]
    public async Task<IActionResult> GetPreguntas()
    {
        try { return Ok(await _service.GetPreguntasAsync()); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error obtener preguntas auditoria ATS"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int? auditadoWorkerId,
        [FromQuery] int? auditorWorkerId,
        [FromQuery] int? proyectoId,
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] string? estado,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _service.GetListAsync(
                auditadoWorkerId, auditorWorkerId, proyectoId,
                fechaDesde, fechaHasta, estado, page, pageSize, GetEmpresaIdContratista()));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error listar auditorias ATS"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try
        {
            var detalle = await _service.GetDetalleAsync(id);
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue && detalle.EmpresaId != empresaId.Value && detalle.EmpresaAuditorId != empresaId.Value)
                return Forbid();
            return Ok(detalle);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error detalle auditoria ATS {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearAuditoriaAtsRequest request)
    {
        try
        {
            if (request.AuditorWorkerId <= 0)
                return BadRequest(new { message = "El auditor es requerido." });
            if (request.AuditadoWorkerId <= 0)
                return BadRequest(new { message = "El trabajador auditado es requerido." });
            if (request.Respuestas.Count == 0)
                return BadRequest(new { message = "Debe completar la evaluación de criterios." });

            var id = await _service.CrearAsync(request);
            return Ok(new { id, message = "Auditoría de ATS registrada correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error crear auditoria ATS"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }
}
