using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.LearningModule.Application.Dtos;
using Abril_Backend.Features.LearningModule.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.LearningModule.Presentation
{
    /// <summary>
    /// Administración del centro de aprendizaje (grupos y videos). Restringido por el
    /// featureKey <c>configuracion.aprendizaje</c> (solo ADMINISTRADOR DEL SISTEMA lo tiene
    /// asignado en role_feature).
    /// </summary>
    [ApiController]
    [Route("api/v1/learning/admin")]
    [Authorize]
    [RequireFeature("configuracion.aprendizaje")]
    public class LearningAdminController : ControllerBase
    {
        private readonly ILearningService _service;
        private readonly ILogger<LearningAdminController> _logger;

        public LearningAdminController(ILearningService service, ILogger<LearningAdminController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Todo lo necesario para la página de administración en una sola petición.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try { return Ok(await _service.GetAdminData()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.GetAll");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ─────────────────────────────── Grupos ───────────────────────────────

        [HttpPost("categoria")]
        public async Task<IActionResult> CreateCategory([FromBody] LearningCategoryCreateDto dto)
        {
            try
            {
                var id = await _service.CreateCategory(dto);
                return Ok(new { id, message = "Grupo creado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.CreateCategory");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("categoria/{id:int}")]
        public async Task<IActionResult> EditCategory(int id, [FromBody] LearningCategoryEditDto dto)
        {
            try
            {
                await _service.EditCategory(id, dto);
                return Ok(new { message = "Grupo actualizado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.EditCategory");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("categoria/{id:int}/toggle")]
        public async Task<IActionResult> ToggleCategory(int id)
        {
            try { return Ok(new { activo = await _service.ToggleCategory(id) }); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.ToggleCategory");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpDelete("categoria/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                await _service.DeleteCategory(id);
                return Ok(new { message = "Grupo eliminado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.DeleteCategory");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ─────────────────────────────── Videos ───────────────────────────────

        [HttpPost("video")]
        public async Task<IActionResult> CreateVideo([FromBody] LearningVideoCreateDto dto)
        {
            try
            {
                var id = await _service.CreateVideo(dto);
                return Ok(new { id, message = "Video creado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.CreateVideo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("video/{id:int}")]
        public async Task<IActionResult> EditVideo(int id, [FromBody] LearningVideoEditDto dto)
        {
            try
            {
                await _service.EditVideo(id, dto);
                return Ok(new { message = "Video actualizado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.EditVideo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("video/{id:int}/toggle")]
        public async Task<IActionResult> ToggleVideo(int id)
        {
            try { return Ok(new { activo = await _service.ToggleVideo(id) }); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.ToggleVideo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpDelete("video/{id:int}")]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            try
            {
                await _service.DeleteVideo(id);
                return Ok(new { message = "Video eliminado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LearningAdminController.DeleteVideo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
