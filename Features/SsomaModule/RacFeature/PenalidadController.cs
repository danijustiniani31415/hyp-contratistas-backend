using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.Rac.Dtos;
using Abril_Backend.Features.Ssoma.Rac.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.Ssoma.Rac;

[ApiController]
[Route("api/v1/ssoma-rac-penalidad")]
[Authorize]
[RequireFeature("ssoma.gestion.rac.penalidades")]
public class PenalidadController : ControllerBase
{
    private readonly IPenalidadService _service;
    private readonly ILogger<PenalidadController> _logger;

    public PenalidadController(IPenalidadService service, ILogger<PenalidadController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    private bool EsContratista() => User.FindFirst("tipo")?.Value == "CONTRATISTA";

    /// <summary>Penalidad es de una sola vía (Abril → Contratista): un contratista solo
    /// puede ver/operar penalidades de su propia empresa.</summary>
    private int? GetEmpresaIdContratista() =>
        EsContratista() && int.TryParse(User.FindFirst("empresaId")?.Value, out var id) ? id : null;

    private bool EsPropioDeContratista(int? empresaId)
    {
        var propia = GetEmpresaIdContratista();
        return !propia.HasValue || empresaId == propia.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] PenalidadListQuery q)
    {
        try
        {
            q.EmpresaId = GetEmpresaIdContratista() ?? q.EmpresaId;
            return Ok(await _service.GetListAsync(q));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PenalidadController.GetList"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try
        {
            var r = await _service.GetDetalleAsync(id);
            if (r is null) return NotFound(new { message = "Penalidad no encontrada." });
            if (!EsPropioDeContratista(r.EmpresaId)) return Forbid();
            return Ok(r);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PenalidadController.GetDetalle"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("{id:int}/descargo")]
    public async Task<IActionResult> PresentarDescargo(int id, [FromBody] PenalidadDescargaRequest req)
    {
        try
        {
            var actual = await _service.GetDetalleAsync(id);
            if (actual is null) return NotFound(new { message = "Penalidad no encontrada." });
            if (!EsPropioDeContratista(actual.EmpresaId)) return Forbid();
            await _service.PresentarDescargaAsync(id, req, GetUserId());
            return NoContent();
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PenalidadController.PresentarDescargo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("{id:int}/resolver")]
    public async Task<IActionResult> Resolver(int id, [FromBody] PenalidadResolverRequest req)
    {
        try
        {
            // Resolver es exclusivo del personal SSOMA de Abril, no del contratista.
            if (EsContratista()) return Forbid();
            return Ok(await _service.ResolverAsync(id, req, GetUserId()));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PenalidadController.Resolver"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        try
        {
            var actual = await _service.GetDetalleAsync(id);
            if (actual is null) return NotFound(new { message = "Penalidad no encontrada." });
            if (!EsPropioDeContratista(actual.EmpresaId)) return Forbid();
            var url = await _service.GetPdfResolucionAsync(id);
            return Redirect(url);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PenalidadController.GetPdf"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }
}
