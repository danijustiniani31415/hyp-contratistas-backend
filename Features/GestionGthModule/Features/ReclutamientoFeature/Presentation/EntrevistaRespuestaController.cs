using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Presentation
{
    /// <summary>
    /// Respuesta del candidato a su citación a entrevista: la que da desde los botones Confirmar y
    /// Rechazar del correo de invitación. Es PÚBLICA (<c>[AllowAnonymous]</c>) porque el candidato
    /// no tiene usuario en el sistema — lo que lo identifica es el token de su entrevista, igual
    /// que en el formulario del postulante.
    ///
    /// La acción va en POST y no en el GET del enlace a propósito: los antivirus de correo y los
    /// previsualizadores de enlaces siguen los GET de un mensaje, y con la respuesta en el GET la
    /// entrevista quedaría confirmada sola sin que el candidato hubiera pulsado nada. El botón
    /// abre la página pública del frontend y es ella la que hace este POST.
    /// </summary>
    [ApiController]
    [Route("api/v1/gestion-gth/entrevista")]
    public class EntrevistaRespuestaController : ControllerBase
    {
        private readonly IReclutamientoService _service;
        private readonly ILogger<EntrevistaRespuestaController> _logger;

        public EntrevistaRespuestaController(
            IReclutamientoService service,
            ILogger<EntrevistaRespuestaController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>
        /// Registra que el candidato confirma o rechaza su entrevista y le avisa a GTH. Devuelve la
        /// cita para que la página pública le confirme sobre cuál respondió. Idempotente: volver a
        /// abrir el mismo enlace responde igual sin reenviarle el aviso a GTH.
        /// </summary>
        [HttpPost("respuesta")]
        [AllowAnonymous]
        public async Task<IActionResult> Responder([FromQuery] string token, [FromQuery] string r)
        {
            try
            {
                return Ok(await _service.ResponderEntrevista(token, r));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EntrevistaRespuestaController.Responder");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
