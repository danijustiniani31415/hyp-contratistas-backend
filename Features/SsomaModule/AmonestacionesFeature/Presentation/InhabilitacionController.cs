using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Presentation;

/// <summary>
/// Restringido a Jefe SSOMA (9), Administrador de Obra (60) y Coordinador SSOMA (70) —
/// espejo exacto de ROLES_INHABILITACION en amonestaciones.ts (frontend). No usa featureKey
/// porque "Inhabilitados" es un tab dentro de Amonestaciones, no una ruta propia.
/// </summary>
[ApiController]
[Route("api/v1/ssoma-inhabilitacion")]
[Authorize(Roles = $"{Roles.AdministradorSsoma},{Roles.AdministradorDeObra},{Roles.CoordinadorSsoma}")]
public class InhabilitacionController : ControllerBase
{
    private readonly SsomaInhabilitacionService _svc;
    private readonly ILogger<InhabilitacionController> _logger;

    public InhabilitacionController(SsomaInhabilitacionService svc, ILogger<InhabilitacionController> logger)
    {
        _svc    = svc;
        _logger = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    /// <summary>Lista de trabajadores inhabilitados actualmente.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLista()
    {
        try { return Ok(await _svc.GetListaInhabilitadosAsync()); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en InhabilitacionController.GetLista"); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    /// <summary>Inhabilita manualmente a un trabajador (sin email ni PDF).</summary>
    [HttpPost("manual")]
    public async Task<IActionResult> InhabilitarManual([FromBody] InhabilitarManualRequest req)
    {
        try
        {
            await _svc.InhabilitarManualAsync(req.WorkerId, req.Motivo, GetUserId());
            return StatusCode(201, new { message = "Trabajador inhabilitado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en InhabilitacionController.InhabilitarManual"); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    /// <summary>Desbloquea manualmente una inhabilitación activa.</summary>
    [HttpPost("{id:int}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        try
        {
            await _svc.DesbloquearManualAsync(id, GetUserId());
            return Ok(new { message = "Trabajador desbloqueado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en InhabilitacionController.Desbloquear"); return StatusCode(500, new { message = "Error del servidor." }); }
    }
}
