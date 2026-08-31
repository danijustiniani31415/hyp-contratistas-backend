using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    /// <summary>
    /// Configuración de EMOs. Acceso restringido por la feature
    /// "ssoma.salud-ocupacional.emos.configuracion". Expone la matriz de
    /// destinatarios de los 4 correos de EMO — programación automática,
    /// programación manual, aceptada por la clínica y rechazada por la clínica —
    /// donde cada destinatario se prende/apaga por perfil de trabajador
    /// (Oficina Central / Staff / Obra / Contratista).
    /// </summary>
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/emos/configuracion")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.emos.configuracion")]
    public class EmoConfiguracionController : ControllerBase
    {
        private readonly IEmoCorreoConfigService _service;
        private readonly ILogger<EmoConfiguracionController> _logger;

        public EmoConfiguracionController(
            IEmoCorreoConfigService service,
            ILogger<EmoConfiguracionController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Perfiles y los 4 correos con su matriz completa, en una sola petición.</summary>
        [HttpGet("correos")]
        public async Task<IActionResult> GetCorreos()
        {
            try { return Ok(await _service.GetConfig()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoConfiguracionController.GetCorreos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Alta de un correo adicional dentro de la sección de un correo.</summary>
        [HttpPost("correos/destinatarios")]
        public async Task<IActionResult> CrearAdicional([FromBody] EmoCorreoAdicionalCreateDto dto)
        {
            try
            {
                var id = await _service.CrearAdicional(dto);
                return Ok(new { id, message = "Destinatario agregado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoConfiguracionController.CrearAdicional"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Cambia el correo de un buzón de área o de un correo adicional.</summary>
        [HttpPut("correos/destinatarios/{id:int}")]
        public async Task<IActionResult> ActualizarDestinatario(int id, [FromBody] EmoCorreoDestinatarioUpdateDto dto)
        {
            try
            {
                await _service.ActualizarDestinatario(id, dto);
                return Ok(new { message = "Destinatario actualizado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoConfiguracionController.ActualizarDestinatario"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Prende o apaga una celda de la matriz (correo × perfil × destinatario).</summary>
        [HttpPut("correos/reglas/{reglaId:int}")]
        public async Task<IActionResult> ActualizarRegla(int reglaId, [FromBody] EmoCorreoReglaPatchDto dto)
        {
            try
            {
                await _service.SetReglaActive(reglaId, dto.Active);
                return Ok(new { message = "Configuración de correo actualizada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoConfiguracionController.ActualizarRegla"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("correos/destinatarios/{id:int}")]
        public async Task<IActionResult> EliminarAdicional(int id)
        {
            try
            {
                await _service.EliminarAdicional(id);
                return Ok(new { message = "Destinatario eliminado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmoConfiguracionController.EliminarAdicional"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
