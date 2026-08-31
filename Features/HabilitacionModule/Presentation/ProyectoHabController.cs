using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/proyectos")]
    [Authorize]
    public class ProyectoHabController : ControllerBase
    {
        private readonly IProyectoHabRepository _repo;
        private readonly ILogger<ProyectoHabController> _logger;

        public ProyectoHabController(IProyectoHabRepository repo, ILogger<ProyectoHabController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetActivos()
        {
            try
            {
                var proyectos = await _repo.GetActivosAsync();
                return Ok(proyectos);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProyectoHabController.GetActivos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Proyectos del desplegable del modal "Programar Inducción". Filtro propio
        /// (HABILITACION_INDUCCION), separado del de Habilitación general que usa <see cref="GetActivos"/>.
        /// </summary>
        [HttpGet("induccion")]
        public async Task<IActionResult> GetActivosInduccion()
        {
            try
            {
                var proyectos = await _repo.GetActivosInduccionAsync();
                return Ok(proyectos);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProyectoHabController.GetActivosInduccion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
