using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Presentation;

/// <summary>
/// Charla diaria obligatoria del contratista: cuando control de acceso tarea a la
/// empresa en un proyecto (ss_tareo_detalle_contratista), la empresa debe subir
/// evidencia de la charla dictada ese día.
/// </summary>
[ApiController]
[Route("api/v1/ssoma-charlas/contratista")]
[Authorize]
public class CharlaContratistaController : ControllerBase
{
    private readonly ICharlaContratistaService _service;
    private readonly ILogger<CharlaContratistaController> _logger;

    public CharlaContratistaController(ICharlaContratistaService service, ILogger<CharlaContratistaController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    private int? GetEmpresaIdContratista() =>
        User.FindFirst("tipo")?.Value == "CONTRATISTA"
            && int.TryParse(User.FindFirst("empresaId")?.Value, out var id)
            ? id
            : null;

    [HttpGet("pendientes")]
    public async Task<IActionResult> GetPendientes([FromQuery] string? fecha)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (!empresaId.HasValue) return Forbid();
            var fechaDto = string.IsNullOrEmpty(fecha) ? DateOnly.FromDateTime(DateTime.Today) : DateOnly.Parse(fecha);
            return Ok(await _service.GetPendientesAsync(empresaId.Value, fechaDto));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error pendientes charla contratista"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("dias-faltantes")]
    public async Task<IActionResult> GetDiasFaltantes()
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (!empresaId.HasValue) return Forbid();
            return Ok(await _service.GetDiasFaltantesAsync(empresaId.Value));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error días faltantes charla contratista"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("historial")]
    public async Task<IActionResult> GetHistorial([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (!empresaId.HasValue) return Forbid();
            return Ok(await _service.GetHistorialAsync(empresaId.Value, page, pageSize));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error historial charla contratista"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Subir([FromBody] CharlaContratistaUploadRequest req)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (!empresaId.HasValue) return Forbid();
            var result = await _service.SubirAsync(empresaId.Value, req, GetUserId());
            return StatusCode(201, result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error subir charla contratista"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    /// <summary>Vista para SSOMA/admin: empresas tareadas que no subieron su charla del día.</summary>
    [HttpGet("incumplimientos")]
    public async Task<IActionResult> GetIncumplimientos([FromQuery] string? fecha, [FromQuery] int? proyectoId)
    {
        try
        {
            if (GetEmpresaIdContratista().HasValue) return Forbid(); // solo staff interno
            var fechaDto = string.IsNullOrEmpty(fecha) ? DateOnly.FromDateTime(DateTime.Today) : DateOnly.Parse(fecha);
            return Ok(await _service.GetIncumplimientosAsync(fechaDto, proyectoId));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error incumplimientos charla contratista"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    /// <summary>Vista para SSOMA/prevencionista: charlas de contratista pendientes de revisión (o filtradas por estado).</summary>
    [HttpGet("revision")]
    [RequireFeature("ssoma.charlas.aprobar")]
    public async Task<IActionResult> GetRevision([FromQuery] string? estado, [FromQuery] int? proyectoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (GetEmpresaIdContratista().HasValue) return Forbid(); // solo staff interno
            return Ok(await _service.GetRevisionAsync(estado, proyectoId, page, pageSize));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error revisión charla contratista"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPut("{id:int}/aprobar")]
    [RequireFeature("ssoma.charlas.aprobar")]
    public async Task<IActionResult> Aprobar(int id)
    {
        try
        {
            if (GetEmpresaIdContratista().HasValue) return Forbid();
            return Ok(await _service.AprobarAsync(id, GetUserId()));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error aprobar charla contratista"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPut("{id:int}/rechazar")]
    [RequireFeature("ssoma.charlas.aprobar")]
    public async Task<IActionResult> Rechazar(int id, [FromBody] RechazarCharlaContratistaDto dto)
    {
        try
        {
            if (GetEmpresaIdContratista().HasValue) return Forbid();
            return Ok(await _service.RechazarAsync(id, dto.Motivo, GetUserId()));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error rechazar charla contratista"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }
}
