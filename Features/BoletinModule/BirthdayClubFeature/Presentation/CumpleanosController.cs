using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Presentation
{
    [ApiController]
    [Route("api/v1/boletin/cumpleanos")]
    [Authorize]
    public class CumpleanosController : ControllerBase
    {
        private readonly ICumpleanosService _service;
        private readonly ILogger<CumpleanosController> _logger;

        public CumpleanosController(ICumpleanosService service, ILogger<CumpleanosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Cumpleañeros del trimestre (1-4) SIN fotos. Las fotos se piden aparte, bajo demanda
        /// (hover), vía <see cref="GetFoto"/>, para que la carga del trimestre sea liviana y rápida.
        /// </summary>
        [HttpGet("trimestre/{trimestre:int}")]
        public async Task<IActionResult> GetTrimestre(int trimestre)
        {
            try { return Ok(await _service.GetTrimestre(trimestre)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CumpleanosController.GetTrimestre");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Foto de perfil (data URI base64) de un cumpleañero, resuelta a demanda cuando el
        /// usuario hace hover sobre el día. Devuelve <c>{ fotoBase64: null }</c> si no hay foto.
        /// </summary>
        [HttpGet("foto")]
        public async Task<IActionResult> GetFoto([FromQuery] string email)
        {
            try { return Ok(new { fotoBase64 = await _service.GetFoto(email) }); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CumpleanosController.GetFoto");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
