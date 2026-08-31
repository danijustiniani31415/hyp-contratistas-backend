using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Presentation
{
    /// <summary>
    /// Reclutadores (Gestión GTH → Configuración): quiénes del equipo de GTH pueden llevar un
    /// proceso de selección. La lista sale sola del área de Gestión del Talento Humano y lo
    /// único que se administra acá es el interruptor de cada uno, que manda sobre el desplegable
    /// "Responsable del proceso" del detalle de Reclutamiento.
    ///
    /// El interruptor vive en <c>gth_responsable_proceso</c>, una tabla aparte: desactivar a
    /// alguien como reclutador no toca su ficha en <c>workers</c> ni lo desactiva en ninguna
    /// otra pantalla del sistema.
    /// </summary>
    [ApiController]
    [Route("api/v1/gestion-gth/reclutadores")]
    [Authorize]
    [RequireFeature("gestion-gth.config.reclutadores")]
    public class ReclutadoresController : ControllerBase
    {
        private readonly IReclutadoresService _service;
        private readonly ILogger<ReclutadoresController> _logger;

        public ReclutadoresController(
            IReclutadoresService service,
            ILogger<ReclutadoresController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        private int? UserId =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        /// <summary>Carga de la pantalla: todas las filas con su estado, en una sola petición.</summary>
        [HttpGet]
        public async Task<IActionResult> GetReclutadores()
        {
            try { return Ok(await _service.GetReclutadores()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutadoresController.GetReclutadores");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Prende o apaga a un trabajador como reclutador.</summary>
        [HttpPatch("{workerId:int}/toggle")]
        public async Task<IActionResult> Toggle(int workerId, [FromBody] ReclutadorTogglePatchDto dto)
        {
            try { return Ok(await _service.Toggle(workerId, dto?.Activo ?? false, UserId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ReclutadoresController.Toggle");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
