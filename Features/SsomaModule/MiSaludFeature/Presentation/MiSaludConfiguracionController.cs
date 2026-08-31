using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.SsomaModule.MiSaludFeature.Presentation
{
    /// <summary>
    /// Configuración de Mi Salud. Acceso restringido por la feature
    /// "ssoma.salud-ocupacional.mi-salud.configuracion" (hoy asignada solo al rol
    /// ADMINISTRADOR DEL SISTEMA en role_feature). Por ahora expone una única
    /// funcionalidad: activar/inactivar los correos que se envían al registrar un
    /// descanso médico.
    /// </summary>
    [ApiController]
    [Route("api/v1/ssoma/mi-salud/configuracion")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.mi-salud.configuracion")]
    public class MiSaludConfiguracionController : ControllerBase
    {
        private readonly IMiSaludService _service;
        private readonly ILogger<MiSaludConfiguracionController> _logger;

        public MiSaludConfiguracionController(IMiSaludService service, ILogger<MiSaludConfiguracionController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpGet("correos")]
        public async Task<IActionResult> GetCorreos()
        {
            try { return Ok(await _service.GetCorreoConfigs()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en MiSaludConfiguracionController.GetCorreos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("correos/{id:int}")]
        public async Task<IActionResult> ActualizarCorreo(int id, [FromBody] ActualizarCorreoConfigDto dto)
        {
            try
            {
                await _service.SetCorreoConfigActive(id, dto.Active);
                return Ok(new { message = "Configuración de correo actualizada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en MiSaludConfiguracionController.ActualizarCorreo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
