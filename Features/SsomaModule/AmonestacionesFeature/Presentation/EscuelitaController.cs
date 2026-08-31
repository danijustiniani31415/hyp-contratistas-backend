using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma-escuelita")]
[Authorize]
[RequireFeature("ssoma.gestion.amonestaciones")]
public class EscuelitaController : ControllerBase
{
    private readonly SsomaEscuelitaService _svc;
    private readonly SsomaInhabilitacionService _inhabilitacion;
    private readonly ILogger<EscuelitaController> _logger;

    public EscuelitaController(
        SsomaEscuelitaService svc,
        SsomaInhabilitacionService inhabilitacion,
        ILogger<EscuelitaController> logger)
    {
        _svc            = svc;
        _inhabilitacion = inhabilitacion;
        _logger         = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    /// <summary>Registra un curso de Escuelita Abril y descuenta puntos al trabajador.</summary>
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] EscuelitaRegistrarRequest req)
    {
        try
        {
            var id = await _svc.RegistrarAsync(req, GetUserId());
            return StatusCode(201, new { id, message = "Curso registrado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en EscuelitaController.Registrar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    /// <summary>Historial de cursos del trabajador.</summary>
    [HttpGet("worker/{workerId:int}")]
    public async Task<IActionResult> GetByWorker(int workerId)
    {
        try { return Ok(await _svc.GetByWorkerAsync(workerId)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en EscuelitaController.GetByWorker"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    /// <summary>Puntos netos actuales del trabajador.</summary>
    [HttpGet("worker/{workerId:int}/puntos")]
    public async Task<IActionResult> GetPuntos(int workerId)
    {
        try
        {
            var puntos = await _inhabilitacion.GetPuntosNetosAsync(workerId);
            return Ok(new { workerId, puntosNetos = puntos });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en EscuelitaController.GetPuntos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }
}
