using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.LearningModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.LearningModule.Presentation
{
    /// <summary>
    /// Lectura pública del centro de aprendizaje: los videos del login (anónimo) y los
    /// del /inicio (autenticado, filtrados por rol).
    /// </summary>
    [ApiController]
    [Route("api/v1/learning")]
    public class LearningController : ControllerBase
    {
        private readonly ILearningService _service;
        private readonly ILogger<LearningController> _logger;

        public LearningController(ILearningService service, ILogger<LearningController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Videos del modal de /auth/login (público, sin sesión). Solo contratistas.</summary>
        [HttpGet("login")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLogin()
        {
            try { return Ok(await _service.GetLoginCategories()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningController.GetLogin");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Grupos de videos del Centro de aprendizaje de /inicio, filtrados por los roles del usuario.</summary>
        [HttpGet("inicio")]
        [Authorize]
        public async Task<IActionResult> GetInicio()
        {
            try
            {
                var roleIds = User.FindAll(ClaimTypes.Role)
                    .Select(c => int.TryParse(c.Value, out var id) ? id : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToArray();

                return Ok(await _service.GetInicioCategories(roleIds));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningController.GetInicio");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
