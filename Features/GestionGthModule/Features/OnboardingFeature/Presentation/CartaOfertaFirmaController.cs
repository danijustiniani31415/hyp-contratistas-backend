using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Presentation
{
    /// <summary>
    /// Página PÚBLICA de la carta oferta: el postulante entra con el token que le llegó por correo, lee
    /// su carta, registra su firma y la firma. Todos los endpoints son <c>[AllowAnonymous]</c> —el
    /// postulante no es usuario del sistema— y el token es lo único que autoriza: cada acción lo
    /// resuelve de nuevo contra la base de datos, así que un token inválido no llega a ninguna parte.
    ///
    /// Reemplaza al flujo anterior, en el que la carta se enviaba adjunta y GTH subía a mano el
    /// documento que el postulante devolvía firmado por correo. Esa vía sigue existiendo en la pantalla
    /// de GTH como respaldo (<see cref="OnboardingController"/>).
    /// </summary>
    [ApiController]
    [Route("api/v1/gestion-gth/carta-oferta-firma")]
    public class CartaOfertaFirmaController : ControllerBase
    {
        private readonly ICartaOfertaFirmaService _service;
        private readonly ILogger<CartaOfertaFirmaController> _logger;

        public CartaOfertaFirmaController(
            ICartaOfertaFirmaService service,
            ILogger<CartaOfertaFirmaController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>
        /// Todo lo de la página en una sola petición: datos de la propuesta, la firma que ya tenga
        /// registrada y en qué estado está el documento.
        /// </summary>
        [HttpGet("publico")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublico([FromQuery] string token)
        {
            try
            {
                return Ok(await _service.GetPublico(token));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CartaOfertaFirmaController.GetPublico");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// PDF de la carta oferta para el visor (el firmado si ya firmó, el original si no). Va como
        /// endpoint aparte del contexto porque es un binario: la página lo carga en un blob y lo
        /// muestra, sin que el postulante vea nunca la URL de SharePoint.
        /// </summary>
        [HttpGet("publico/documento")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDocumento([FromQuery] string token)
        {
            try
            {
                var (content, contentType, fileName) = await _service.GetDocumento(token);

                // inline: la carta se lee dentro de la página, no se fuerza una descarga.
                Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";
                return File(content, contentType);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CartaOfertaFirmaController.GetDocumento");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Guarda la firma que el postulante dibujó en el canvas. Queda en su ficha de la base maestra
        /// y es lo que habilita el botón «Firmar».
        /// </summary>
        [HttpPut("publico/firma")]
        [AllowAnonymous]
        public async Task<IActionResult> GuardarFirma(
            [FromQuery] string token, [FromBody] CartaOfertaFirmaGuardarDto dto)
        {
            try
            {
                return Ok(await _service.GuardarFirma(token, dto));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CartaOfertaFirmaController.GuardarFirma");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Firma la carta: estampa la firma registrada en la última página, la guarda en el file digital
        /// del colaborador y la deja como carta firmada del onboarding, pendiente de revisión de GTH.
        /// </summary>
        [HttpPost("publico/firmar")]
        [AllowAnonymous]
        public async Task<IActionResult> Firmar([FromQuery] string token)
        {
            try
            {
                return Ok(await _service.Firmar(token));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CartaOfertaFirmaController.Firmar");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
