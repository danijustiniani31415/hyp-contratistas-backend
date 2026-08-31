using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Topico;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.topico")]
    public class TopicoController : ControllerBase
    {
        private readonly ITopicoService _service;
        private readonly IFileStorageService _storage;
        private readonly ILogger<TopicoController> _logger;

        public TopicoController(ITopicoService service, IFileStorageService storage, ILogger<TopicoController> logger)
        {
            _service = service;
            _storage = storage;
            _logger = logger;
        }

        private int? CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        [HttpGet("topico/hoy")]
        public async Task<IActionResult> GetHoy()
        {
            try { return Ok(await _service.GetHoy()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("topico/tipos-atencion")]
        public async Task<IActionResult> GetTiposAtencion()
        {
            try { return Ok(await _service.GetTiposAtencion()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("topico")]
        public async Task<IActionResult> GetList([FromQuery] TopicoFilterDto filter)
        {
            try { return Ok(await _service.ListPaged(filter)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("topico/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try { return Ok(await _service.GetById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("topico")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] TopicoCreateDto dto, IFormFile? archivoInforme)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "No se pudo identificar al usuario." });

                if (archivoInforme is { Length: > 0 })
                {
                    var urls = await _storage.UploadFilesAsync(
                        [(archivoInforme.OpenReadStream(), archivoInforme.FileName)],
                        "ssoma-topico");
                    dto.UrlInforme = urls.FirstOrDefault();
                }

                var id = await _service.Create(dto, userId);
                return Ok(new { id, message = "Atención de tópico registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("topico/{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(int id, [FromForm] TopicoUpdateDto dto, IFormFile? archivoInforme)
        {
            try
            {
                if (archivoInforme is { Length: > 0 })
                {
                    var urls = await _storage.UploadFilesAsync(
                        [(archivoInforme.OpenReadStream(), archivoInforme.FileName)],
                        "ssoma-topico");
                    dto.UrlInforme = urls.FirstOrDefault();
                }
                await _service.Update(id, dto);
                return Ok(new { message = "Atención de tópico actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("topico/{id:int}/cerrar")]
        public async Task<IActionResult> Cerrar(int id)
        {
            try
            {
                await _service.Cerrar(id, CurrentUserId());
                return Ok(new { message = "Atención de tópico cerrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("topico/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.Delete(id);
                return Ok(new { message = "Atención de tópico eliminada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── Evoluciones clínicas ──────────────────────────────────────────────

        [HttpGet("topico/{id:int}/evoluciones")]
        public async Task<IActionResult> GetEvoluciones(int id)
        {
            try { return Ok(await _service.GetEvoluciones(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController.GetEvoluciones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("topico/{id:int}/evoluciones")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateEvolucion(int id, [FromForm] TopicoEvolucionCreateDto dto, IFormFile? archivoEvidencia)
        {
            try
            {
                var userId = CurrentUserId();
                if (!userId.HasValue)
                    return Unauthorized(new { message = "No se pudo identificar al usuario." });

                if (archivoEvidencia is { Length: > 0 })
                {
                    var urls = await _storage.UploadFilesAsync(
                        [(archivoEvidencia.OpenReadStream(), archivoEvidencia.FileName)],
                        "ssoma-topico-evidencia");
                    dto.UrlEvidencia = urls.FirstOrDefault();
                }

                var evId = await _service.CreateEvolucion(id, dto, userId);
                return Ok(new { id = evId, message = "Evolución clínica registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController.CreateEvolucion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("topico/evoluciones/{evId:int}")]
        public async Task<IActionResult> DeleteEvolucion(int evId)
        {
            try
            {
                await _service.DeleteEvolucion(evId);
                return Ok(new { message = "Evolución eliminada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en TopicoController.DeleteEvolucion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
