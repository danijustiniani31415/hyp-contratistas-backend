using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.SsomaModule.MiSaludFeature.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/mi-salud")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.mi-salud")]
    public class MiSaludController : ControllerBase
    {
        private readonly IMiSaludService _service;
        private readonly ILogger<MiSaludController> _logger;

        public MiSaludController(IMiSaludService service, ILogger<MiSaludController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        private int CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(val, out var id))
                throw new AbrilException("No se pudo identificar al usuario.", 401);
            return id;
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen()
        {
            try { return Ok(await _service.GetResumen(CurrentUserId())); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en MiSaludController.GetResumen"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("descansos")]
        public async Task<IActionResult> GetDescansos([FromQuery] int page = 1)
        {
            try { return Ok(await _service.GetDescansos(CurrentUserId(), page)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en MiSaludController.GetDescansos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("descansos")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateDescanso([FromForm] CrearMiDescansoDto dto, [FromForm] List<IFormFile>? documentos)
        {
            try
            {
                dto.Documentos = documentos;
                var id = await _service.CreateDescanso(CurrentUserId(), dto);
                return Ok(new { id, message = "Descanso médico registrado exitosamente. Pendiente de aprobación." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en MiSaludController.CreateDescanso"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Sirve el certificado de un descanso propio. El archivo vive en la carpeta de SharePoint
        /// configurada (ss_descanso_carpeta) y lo baja el backend con su token de app, de modo que
        /// el trabajador lo ve sin tener que estar logueado en Microsoft 365 en el navegador.
        /// </summary>
        [HttpGet("descansos/adjuntos/{adjuntoId:int}")]
        public async Task<IActionResult> GetCertificado(int adjuntoId)
        {
            try
            {
                var archivo = await _service.GetCertificado(CurrentUserId(), adjuntoId);
                return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en MiSaludController.GetCertificado"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
