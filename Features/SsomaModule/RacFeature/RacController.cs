using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.Rac.Dtos;
using Abril_Backend.Features.Ssoma.Rac.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.Ssoma.Rac;

[ApiController]
[Route("api/v1/ssoma-rac")]
[Authorize]
public class RacController : ControllerBase
{
    private readonly IRacService _service;
    private readonly ILogger<RacController> _logger;

    public RacController(IRacService service, ILogger<RacController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    private bool EsContratista() =>
        User.FindFirst(ClaimTypes.Role)?.Value == Roles.Contratista;

    /// <summary>
    /// Empresa del usuario contratista logueado (claim "empresaId" del JWT emitido
    /// por ContratistaAuthService). Null si el usuario no es contratista.
    /// </summary>
    private int? GetEmpresaIdContratista() =>
        EsContratista() && int.TryParse(User.FindFirst("empresaId")?.Value, out var id) ? id : null;

    [HttpGet]
    [RequireFeature("ssoma.gestion.rac.lista")]
    public async Task<IActionResult> GetList([FromQuery] RacListQuery q)
    {
        try
        {
            q.EmpresaIdContratista = GetEmpresaIdContratista();
            return Ok(await _service.GetListAsync(q));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetList"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("dashboard")]
    [RequireFeature("ssoma.gestion.rac.dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try { return Ok(await _service.GetDashboardAsync(GetEmpresaIdContratista())); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetDashboard"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategorias()
    {
        try { return Ok(await _service.GetCategoriasAsync()); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetCategorias"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("infracciones")]
    public async Task<IActionResult> GetInfracciones()
    {
        try { return Ok(await _service.GetInfraccionesAsync()); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetInfracciones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("proyecto/{projectId:int}/niveles")]
    public async Task<IActionResult> GetNiveles(int projectId)
    {
        try { return Ok(await _service.GetNivelesProyectoAsync(projectId)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetNiveles"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost]
    [RequireFeature("ssoma.gestion.rac.crear")]
    public async Task<IActionResult> Crear([FromBody] RacCreateRequest req)
    {
        try { return StatusCode(201, await _service.CrearAsync(req, GetUserId())); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.Crear"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}")]
    [RequireFeature("ssoma.gestion.rac.detalle")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try
        {
            var r = await _service.GetDetalleAsync(id);
            if (r is null) return NotFound(new { message = "RAC no encontrado." });
            if (!EsPropioDeContratista(r.EmpresaReportadaId, r.EmpresaReportanteId)) return Forbid();
            return Ok(r);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetDetalle"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("{id:int}/cerrar")]
    [RequireFeature("ssoma.gestion.rac.cerrar")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] RacCerrarRequest req)
    {
        try
        {
            var actual = await _service.GetDetalleAsync(id);
            if (actual is null) return NotFound(new { message = "RAC no encontrado." });
            // Cerrar/levantar es solo de la empresa reportada (a quien se le observó);
            // la empresa reportante puede ver el RAC pero no levantarlo por ella.
            // Se devuelve un 403 con mensaje (no Forbid(), que responde con cuerpo vacío
            // y el frontend lo muestra como el genérico "Ocurrió un error.").
            if (!EsPropioDeContratista(actual.EmpresaReportadaId))
                return StatusCode(403, new { message = "Solo la empresa observada puede cerrar este RAC." });
            return Ok(await _service.CerrarAsync(id, req, GetUserId()));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.Cerrar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/fotos")]
    [RequestSizeLimit(50_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubirFoto(int id, [FromForm] IFormFile file, [FromForm] string tipo = "Hallazgo")
    {
        try
        {
            var actual = await _service.GetDetalleAsync(id);
            if (actual is null) return NotFound(new { message = "RAC no encontrado." });
            if (!EsPropioDeContratista(actual.EmpresaReportadaId, actual.EmpresaReportanteId)) return Forbid();
            return StatusCode(201, await _service.SubirFotoAsync(id, file, tipo, GetUserId()));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.SubirFoto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}/fotos/{fotoId:int}")]
    public async Task<IActionResult> GetFoto(int id, int fotoId)
    {
        try
        {
            var actual = await _service.GetDetalleAsync(id);
            if (actual is null) return NotFound(new { message = "RAC no encontrado." });
            if (!EsPropioDeContratista(actual.EmpresaReportadaId, actual.EmpresaReportanteId)) return Forbid();
            if (!actual.Fotos.Any(f => f.Id == fotoId)) return NotFound(new { message = "Foto no encontrada." });
            var bytes = await _service.GetFotoBytesAsync(fotoId);
            if (bytes is null) return NotFound(new { message = "Foto no disponible." });
            return File(bytes, "image/jpeg");
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetFoto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}/reporte")]
    public async Task<IActionResult> GetReporte(int id)
    {
        try
        {
            var rac = await _service.GetDetalleAsync(id);
            if (rac is null) return NotFound(new { message = "RAC no encontrado." });
            if (!EsPropioDeContratista(rac.EmpresaReportadaId, rac.EmpresaReportanteId)) return Forbid();
            if (string.IsNullOrWhiteSpace(rac.PdfUrl)) return NotFound(new { message = "PDF no disponible." });
            var url = "https://abrilinmob.sharepoint.com/sites/SSOMA-Powerapps/RacPDF2026/" + rac.PdfUrl;
            return Ok(new { url });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetReporte"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GetPdf(int id)
    {
        try
        {
            var rac = await _service.GetDetalleAsync(id);
            if (rac is null) return NotFound(new { message = "RAC no encontrado." });
            if (!EsPropioDeContratista(rac.EmpresaReportadaId, rac.EmpresaReportanteId)) return Forbid();
            var bytes = await _service.GetPdfAsync(id);
            return File(bytes, "application/pdf", $"RAC-{id}.pdf");
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en RacController.GetPdf"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    /// <summary>
    /// Si el usuario es contratista, exige que el RAC pertenezca a su propia
    /// empresa — ya sea como reportada O como reportante; los usuarios internos
    /// (admin/SSOMA Abril) ven todo. Antes solo comparaba contra la empresa
    /// reportada, lo que bloqueaba (403) a un contratista de ver/subir evidencia
    /// a un RAC que él mismo reportó contra otra empresa.
    /// </summary>
    private bool EsPropioDeContratista(int? empresaReportadaId, int? empresaReportanteId = null)
    {
        var empresaId = GetEmpresaIdContratista();
        if (!empresaId.HasValue) return true;
        return empresaReportadaId == empresaId.Value || empresaReportanteId == empresaId.Value;
    }
}
