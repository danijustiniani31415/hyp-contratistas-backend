using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;
using System.Security.Claims;

namespace Abril_Backend.Features.SsomaModule.InspeccionFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma-inspeccion")]
[Authorize]
public class InspeccionController : ControllerBase
{
    private readonly IInspeccionService _service;
    private readonly InspeccionPdfService _pdfService;
    private readonly IInspeccionSharePointService _sp;
    private readonly ILogger<InspeccionController> _logger;

    public InspeccionController(IInspeccionService service, InspeccionPdfService pdfService, IInspeccionSharePointService sp, ILogger<InspeccionController> logger)
    {
        _service = service;
        _pdfService = pdfService;
        _sp = sp;
        _logger = logger;
    }

    private static string ContentTypeDeArchivo(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };
    }

    private int? GetEmpresaIdContratista() =>
        User.FindFirst("tipo")?.Value == "CONTRATISTA"
            && int.TryParse(User.FindFirst("empresaId")?.Value, out var id)
            ? id
            : null;

    private bool EsContratista() => User.FindFirst("tipo")?.Value == "CONTRATISTA";

    private int? GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    [HttpGet("catalogos")]
    public async Task<IActionResult> GetCatalogos()
    {
        try { return Ok(await _service.GetCatalogosAsync()); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error catalogos inspeccion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("checklist/{tipoId:int}")]
    public async Task<IActionResult> GetChecklist(int tipoId)
    {
        try { return Ok(await _service.GetChecklistAsync(tipoId)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error checklist inspeccion {TipoId}", tipoId); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet]
    [RequireFeature("ssoma.gestion.inspeccion.lista")]
    public async Task<IActionResult> GetList(
        [FromQuery] int? proyectoId, [FromQuery] int? tipoId,
        [FromQuery] string? estado,
        [FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try { return Ok(await _service.GetListAsync(proyectoId, tipoId, estado, fechaDesde, fechaHasta, page, pageSize, GetEmpresaIdContratista())); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error lista inspecciones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("dashboard")]
    [RequireFeature("ssoma.gestion.inspeccion.dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int? proyectoId, [FromQuery] int? anio)
    {
        try { return Ok(await _service.GetDashboardAsync(proyectoId, anio, GetEmpresaIdContratista())); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error dashboard inspecciones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try
        {
            var detalle = await _service.GetDetalleAsync(id);
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue && detalle.EmpresaId != empresaId.Value && detalle.EmpresaInspectoraId != empresaId.Value)
                return Forbid();
            return Ok(detalle);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error detalle inspeccion {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost]
    [RequireFeature("ssoma.gestion.inspeccion.nueva")]
    public async Task<IActionResult> Crear([FromBody] CrearInspeccionRequest request)
    {
        try
        {
            if (request.TipoId <= 0)
                return BadRequest(new { message = "El tipo de inspección es requerido." });
            if (request.ProyectoId <= 0)
                return BadRequest(new { message = "El proyecto es requerido." });
            request.EmpresaInspectoraId = GetEmpresaIdContratista();
            var id = await _service.CrearInspeccionAsync(request, GetUserId());
            return Ok(new { id, message = "Inspección registrada correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error crear inspeccion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("~/api/v1/ssoma-inspeccion-hallazgo/{id:int}/cerrar")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> CerrarHallazgo(int id, [FromBody] CerrarHallazgoRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.AccionCorrectiva))
                return BadRequest(new { message = "La acción correctiva es requerida." });
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var empresaHallazgo = await _service.GetEmpresaIdDeHallazgoAsync(id);
                if (empresaHallazgo.EmpresaId != empresaId.Value && empresaHallazgo.EmpresaInspectoraId != empresaId.Value) return Forbid();
            }
            await _service.CerrarHallazgoAsync(id, request);
            return Ok(new { message = "Hallazgo cerrado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cerrar hallazgo {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPut("~/api/v1/ssoma-inspeccion-hallazgo/{id:int}")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> EditarHallazgo(int id, [FromBody] EditarHallazgoRequest request)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var empresaHallazgo = await _service.GetEmpresaIdDeHallazgoAsync(id);
                if (empresaHallazgo.EmpresaId != empresaId.Value && empresaHallazgo.EmpresaInspectoraId != empresaId.Value) return Forbid();
            }
            await _service.EditarHallazgoAsync(id, request);
            return Ok(new { message = "Hallazgo actualizado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error editar hallazgo {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpDelete("~/api/v1/ssoma-inspeccion-hallazgo/{id:int}")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> EliminarHallazgo(int id)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var empresaHallazgo = await _service.GetEmpresaIdDeHallazgoAsync(id);
                if (empresaHallazgo.EmpresaId != empresaId.Value && empresaHallazgo.EmpresaInspectoraId != empresaId.Value) return Forbid();
            }
            await _service.EliminarHallazgoAsync(id);
            return Ok(new { message = "Hallazgo eliminado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error eliminar hallazgo {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("abiertas")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> GetAbiertas([FromQuery] int? proyectoId)
    {
        try { return Ok(await _service.GetAbiertasAsync(proyectoId)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error listar inspecciones abiertas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/unirse")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> Unirse(int id)
    {
        try
        {
            await _service.UnirseAsync(id, GetUserId(), EsContratista());
            return Ok(new { message = "Te uniste a la inspección." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error unirse inspeccion {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/hallazgos")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> AgregarHallazgo(int id, [FromBody] InspeccionHallazgoRequest request)
    {
        try
        {
            await _service.AgregarHallazgoAsync(id, request, GetUserId(), EsContratista());
            return Ok(new { message = "Hallazgo agregado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error agregar hallazgo a inspeccion {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}/destinatarios-cierre-colaborativa")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> DestinatariosCierreColaborativa(int id)
    {
        try
        {
            if (EsContratista()) return Forbid();
            var dto = await _service.GetDestinatariosCierreColaborativaAsync(id, GetUserId());
            return Ok(dto);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error resolver destinatarios de cierre de inspeccion colaborativa {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("{id:int}/cerrar-colaborativa")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> CerrarColaborativa(int id)
    {
        try
        {
            // Solo staff interno de Abril puede cerrar una inspección grupal (no contratistas).
            if (EsContratista()) return Forbid();
            await _service.CerrarInspeccionColaborativaAsync(id, GetUserId());
            return Ok(new { message = "Inspección cerrada correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cerrar inspeccion colaborativa {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("{id:int}/reabrir-colaborativa")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> ReabrirColaborativa(int id)
    {
        try
        {
            if (EsContratista()) return Forbid();
            await _service.ReabrirInspeccionColaborativaAsync(id);
            return Ok(new { message = "Inspección reabierta correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error reabrir inspeccion colaborativa {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("media")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> GetMedia([FromQuery] string path, [FromQuery] string tipo = "fotos")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return BadRequest(new { message = "path es requerido." });
            var contexto = tipo == "firmas" ? "inspeccion-firmas" : "inspeccion-fotos";
            var bytes = await _sp.DescargarAsync(path, contexto);
            if (bytes == null) return NotFound();
            return File(bytes, ContentTypeDeArchivo(path));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error media inspeccion {Path}", path); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}/pdf")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> GenerarPdf(int id)
    {
        try
        {
            var detalle = await _service.GetDetalleAsync(id);
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue && detalle.EmpresaId != empresaId.Value && detalle.EmpresaInspectoraId != empresaId.Value)
                return Forbid();
            var pdf = await _pdfService.GenerarPdfAsync(detalle);
            return File(pdf, "application/pdf", $"Inspeccion_{id}_{DateTime.Now:yyyyMMdd}.pdf");
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error PDF inspeccion {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("hallazgos")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> GetHallazgos(
        [FromQuery] string? estado,
        [FromQuery] string? proyecto,
        [FromQuery] string? area,
        [FromQuery] int? responsableId,
        [FromQuery] DateTime? fechaLimiteHasta)
    {
        try { return Ok(await _service.GetHallazgosAsync(estado, proyecto, area, fechaLimiteHasta, GetEmpresaIdContratista())); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error listar hallazgos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("hallazgos/{hallazgoId:int}/levantar")]
    [RequireFeature("ssoma.gestion.inspeccion")]
    public async Task<IActionResult> LevantarHallazgo(int hallazgoId, [FromBody] LevantarHallazgoDto dto)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var empresaHallazgo = await _service.GetEmpresaIdDeHallazgoAsync(hallazgoId);
                if (empresaHallazgo.EmpresaId != empresaId.Value && empresaHallazgo.EmpresaInspectoraId != empresaId.Value) return Forbid();
            }
            await _service.LevantarHallazgoAsync(hallazgoId, dto);
            return Ok(new { message = "Hallazgo actualizado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error levantar hallazgo {Id}", hallazgoId); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }
}
