using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Presentation;

[Authorize]
[ApiController]
[Route("api/v1/arquitectura-comercial/observaciones")]
[RequireFeature("arquitectura-comercial.observaciones")]
public class ObservacionesController : ControllerBase
{
    private readonly IObservacionService _service;
    private readonly ILogger<ObservacionesController> _logger;

    public ObservacionesController(IObservacionService service, ILogger<ObservacionesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetObservaciones(
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
            var result = await _service.GetObservaciones(proyectoId, estado, partida, desde, hasta, search, pagina, porPagina);
            return Ok(result);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception)
        {
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
        catch (Exception)
        {
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
        catch (Exception)
        {
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
        catch (Exception)
        {
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
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPost]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> CreateObservacion([FromForm] CreateObservacionDTO body, IFormFile? foto)
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
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPost("{id:int}/levantar")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Levantar(int id, [FromForm] LevantarObservacionDTO body, IFormFile? foto)
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
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPut("{id:int}")]
    [RequireFeature("arquitectura-comercial.observaciones.editar")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateObservacionDTO body)
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
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    /// <summary>Sube la foto de "Observacion" cuando la observación se creó sin ella.</summary>
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
            _logger.LogError(ex, "Error agregando foto a observación {ObservacionId}", id);
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    /// <summary>Reemplaza el archivo de una foto ya subida (se equivocaron de foto al reportar o al levantar).</summary>
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

    /// <summary>
    /// Sirve el contenido de una foto desde nuestro propio dominio en vez de la webUrl cruda de
    /// SharePoint. El &lt;img src&gt; que apuntaba directo a SharePoint solo cargaba en navegadores
    /// con sesión de Microsoft 365 activa (ambiente de escritorio de oficina) — en celular, sin esa
    /// sesión, SharePoint devolvía una página de login en vez de la imagen y la miniatura salía rota.
    /// Acepta el JWT por query string (?access_token=) porque un &lt;img&gt; no puede mandar el
    /// header Authorization — mismo mecanismo que ya se usa para /hubs (ver Program.cs).
    /// </summary>
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
