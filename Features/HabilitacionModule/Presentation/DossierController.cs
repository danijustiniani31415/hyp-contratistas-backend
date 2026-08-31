using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Dossier;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Habilitacion.Presentation;

[ApiController]
[Route("api/v1/habilitacion/dossier")]
[Authorize]
public class DossierController : ControllerBase
{
    private readonly IDossierService _service;
    private readonly ILogger<DossierController> _logger;

    public DossierController(IDossierService service, ILogger<DossierController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private bool EsContratista() => User.FindFirst("tipo")?.Value == "CONTRATISTA";

    /// <summary>Dossier no tenía NINGÚN control de propiedad server-side: contributorId venía
    /// del query string sin validar, y no había forma de saber si un dossierId/docId/archivoId
    /// pertenecía a la empresa del contratista logueado. Mismo patrón que RAC/Inspecciones.</summary>
    private int? GetEmpresaIdContratista() =>
        EsContratista() && int.TryParse(User.FindFirst("empresaId")?.Value, out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> GetSemanas(
        [FromQuery] int? contributorId,
        [FromQuery] int? proyectoId,
        [FromQuery] int? anio)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue) contributorId = empresaId.Value; // nunca confiar en el query string
            return Ok(await _service.GetSemanasAsync(contributorId, proyectoId, anio));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error GetSemanas dossier"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try
        {
            var detalle = await _service.GetDetalleAsync(id);
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue && detalle.ContributorId != empresaId.Value) return Forbid();
            return Ok(detalle);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error GetDetalle dossier {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("semana")]
    public async Task<IActionResult> EnsureSemana([FromBody] EnsureSemanaRequest req)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue && req.ContributorId != empresaId.Value) return Forbid();
            return Ok(await _service.EnsureSemanaAsync(req));
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error EnsureSemana dossier"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{dossierId:int}/documento")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubirDocumento(int dossierId, [FromForm] SubirDocumentoDossierRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.TipoDoc))
                return BadRequest(new { message = "El tipo de documento es requerido." });
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var detalle = await _service.GetDetalleAsync(dossierId);
                if (detalle.ContributorId != empresaId.Value) return Forbid();
            }
            await _service.SubirDocumentoAsync(dossierId, req.TipoDoc, req.File!);
            return Ok(new { message = "Documento subido correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error SubirDocumento dossier {DossierId}", dossierId); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("documento/{docId:int}/marcar-na")]
    public async Task<IActionResult> MarcarNa(int docId)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var contributorId = await _service.GetContributorIdDeDocumentoAsync(docId);
                if (contributorId != empresaId.Value) return Forbid();
            }
            await _service.MarcarNaAsync(docId);
            return Ok(new { message = "Estado actualizado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error MarcarNA doc {DocId}", docId); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{dossierId:int}/enviar")]
    public async Task<IActionResult> Enviar(int dossierId)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var detalle = await _service.GetDetalleAsync(dossierId);
                if (detalle.ContributorId != empresaId.Value) return Forbid();
            }
            await _service.EnviarAsync(dossierId);
            return Ok(new { message = "Dossier enviado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error Enviar dossier {DossierId}", dossierId); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{dossierId:int}/revisar")]
    public async Task<IActionResult> Revisar(int dossierId, [FromBody] RevisarDossierRequest req)
    {
        try
        {
            // Aprobar/Rechazar es exclusivo del revisor interno (Abril) — el contratista sube
            // documentos y envía, pero nunca puede aprobarse/rechazarse a sí mismo.
            if (EsContratista()) return Forbid();
            await _service.RevisarAsync(dossierId, req);
            return Ok(new { message = "Dossier revisado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error Revisar dossier {DossierId}", dossierId); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("documento/{docId:int}/revisar")]
    public async Task<IActionResult> RevisarDocumento(int docId, [FromBody] RevisarDocumentoRequest req)
    {
        try
        {
            if (EsContratista()) return Forbid();
            await _service.RevisarDocumentoAsync(docId, req);
            return Ok(new { message = "Documento revisado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error RevisarDocumento doc {DocId}", docId); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("{dossierId:int}/no-aplica")]
    public async Task<IActionResult> MarcarNoAplica(int dossierId)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var detalle = await _service.GetDetalleAsync(dossierId);
                if (detalle.ContributorId != empresaId.Value) return Forbid();
            }
            await _service.MarcarSemanaNoAplicaAsync(dossierId);
            return Ok(new { message = "Semana marcada como No Aplica." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error MarcarNoAplica dossier {Id}", dossierId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpDelete("archivo/{archivoId:int}")]
    public async Task<IActionResult> EliminarArchivo(int archivoId)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var contributorId = await _service.GetContributorIdDeArchivoAsync(archivoId);
                if (contributorId != empresaId.Value) return Forbid();
            }
            await _service.EliminarArchivoAsync(archivoId);
            return Ok(new { message = "Archivo eliminado." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error EliminarArchivo {ArchivoId}", archivoId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpGet("archivo/{archivoId:int}/url")]
    public async Task<IActionResult> GetArchivoUrl(int archivoId)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var contributorId = await _service.GetContributorIdDeArchivoAsync(archivoId);
                if (contributorId != empresaId.Value) return Forbid();
            }
            var url = await _service.GetArchivoUrlAsync(archivoId);
            return Ok(new { url });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error GetArchivoUrl {ArchivoId}", archivoId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpGet("documento/{docId:int}/url")]
    public async Task<IActionResult> GetDocumentoUrl(int docId)
    {
        try
        {
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue)
            {
                var contributorId = await _service.GetContributorIdDeDocumentoAsync(docId);
                if (contributorId != empresaId.Value) return Forbid();
            }
            var url = await _service.GetDocumentoUrlAsync(docId);
            return Ok(new { url });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error GetDocumentoUrl {DocId}", docId); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }
}
