using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/configuracion/correos")]
    [Authorize]
    public class CorreoConfigController : ControllerBase
    {
        private readonly ICorreoConfigService _service;
        private readonly ILogger<CorreoConfigController> _logger;

        public CorreoConfigController(ICorreoConfigService service, ILogger<CorreoConfigController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Carga inicial: los correos configurables con sus reglas + opciones de los desplegables.</summary>
        [HttpGet]
        public async Task<IActionResult> GetInicial()
        {
            try { return Ok(await _service.GetInicialAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CorreoConfigController.GetInicial");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Reemplaza las reglas (inclusiones + exclusiones) de un correo por su código.</summary>
        [HttpPut("{eventoCodigo}")]
        public async Task<IActionResult> UpdateReglas(string eventoCodigo, [FromBody] CorreoReglasUpdateDto dto)
        {
            try
            {
                await _service.UpdateReglasAsync(eventoCodigo, dto ?? new CorreoReglasUpdateDto());
                return Ok(new { message = "Destinatarios del correo actualizados exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CorreoConfigController.UpdateReglas");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
