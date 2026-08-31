using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Presentation;

[Authorize]
[ApiController]
[Route("api/v1/arquitectura-comercial/revisiones")]
[RequireFeature("arquitectura-comercial.revisiones")]
public class RevisionesController : ControllerBase
{
    private readonly IRevisionService _service;
    private readonly ILogger<RevisionesController> _logger;

    public RevisionesController(IRevisionService service, ILogger<RevisionesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── Catálogo de revisiones (proyecto + tipo + lugar → nombre autogenerado) ──

    [HttpGet("catalogo")]
    public async Task<IActionResult> GetCatalogo([FromQuery] int? proyectoId, [FromQuery] bool soloActivas = true)
    {
        try
        {
            var result = await _service.GetRevisiones(proyectoId, soloActivas);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.GetCatalogo");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPost("catalogo")]
    public async Task<IActionResult> CreateCatalogo([FromBody] CreateRevisionDTO body)
    {
        try
        {
            var result = await _service.CreateRevision(body);
            return Ok(result);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.CreateCatalogo");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpDelete("catalogo/{id:int}")]
    public async Task<IActionResult> DeleteCatalogo(int id)
    {
        try
        {
            var eliminado = await _service.DeleteRevision(id);
            if (!eliminado) return NotFound(new { message = "No se encontró la revisión." });
            return Ok(new { message = "Revisión eliminada." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.DeleteCatalogo");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    // ── Observaciones dentro de una revisión ──

    [HttpGet]
    public async Task<IActionResult> GetObservaciones(
        [FromQuery] int? revisionId,
        [FromQuery] int? proyectoId,
        [FromQuery] string? estado,
        [FromQuery] string? partida,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? search,
        [FromQuery] int pagina = 1,
        [FromQuery] int porPagina = 20)
    {
        try
        {
            var result = await _service.GetObservaciones(revisionId, proyectoId, estado, partida, desde, hasta, search, pagina, porPagina);
            return Ok(result);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.GetObservaciones");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetObservacionById(id);
            if (result == null) return NotFound(new { message = "No se encontró la observación." });
            return Ok(result);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.GetById");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpGet("filtros")]
    public async Task<IActionResult> GetFiltros()
    {
        try
        {
            var result = await _service.GetFiltros();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.GetFiltros");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? proyectoId)
    {
        try
        {
            var result = await _service.GetDashboard(desde, hasta, proyectoId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.GetDashboard");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? proyectoId)
    {
        try
        {
            var result = await _service.GetStats(desde, hasta, proyectoId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.GetStats");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> CreateObservacion([FromForm] CreateRevisionObservacionDTO body, IFormFile? foto)
    {
        try
        {
            Stream? stream = foto != null ? foto.OpenReadStream() : null;
            var result = await _service.CreateObservacion(body, stream, foto?.FileName);
            return Ok(result);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.CreateObservacion");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPost("{id:int}/levantar")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Levantar(int id, [FromForm] LevantarRevisionObservacionDTO body, IFormFile? foto)
    {
        try
        {
            Stream? stream = foto != null ? foto.OpenReadStream() : null;
            var result = await _service.LevantarObservacion(id, stream, foto?.FileName, body);
            if (result == null) return NotFound(new { message = "No se encontró la observación." });
            return Ok(result);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.Levantar");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPut("{id:int}")]
    [RequireFeature("arquitectura-comercial.revisiones.editar")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRevisionObservacionDTO body)
    {
        try
        {
            var result = await _service.UpdateObservacion(id, body);
            if (result == null) return NotFound(new { message = "No se encontró la observación." });
            return Ok(result);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RevisionesController.Update");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPost("{id:int}/fotos")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> AgregarFoto(int id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "Debe adjuntar un archivo." });
            using var stream = file.OpenReadStream();
            var url = await _service.AgregarFotoObservacion(id, stream, file.FileName);
            return Ok(new { url });
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error agregando foto a observación de revisión {Id}", id);
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPatch("fotos/{fotoId:int}")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ReemplazarFoto(int fotoId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "Debe adjuntar un archivo." });
            using var stream = file.OpenReadStream();
            var url = await _service.ReemplazarFoto(fotoId, stream, file.FileName);
            return Ok(new { url });
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reemplazando foto {FotoId}", fotoId);
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpGet("fotos/{fotoId:int}/contenido")]
    public async Task<IActionResult> GetFotoContenido(int fotoId)
    {
        try
        {
            var foto = await _service.GetFotoContenido(fotoId);
            if (foto == null) return NotFound();
            return File(foto.Value.Bytes, foto.Value.ContentType);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sirviendo contenido de foto {FotoId}", fotoId);
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }
}
