using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Presentation
{
    [ApiController]
    [Route("api/v1/mejora-continua/lesson-areas")]
    public class LessonAreaController : ControllerBase
    {
        private readonly ILessonAreaService _service;
        public LessonAreaController(ILessonAreaService service) => _service = service;

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try { return Ok(await _service.GetAllAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Ramas habilitadas que tienen al menos una relación (scope_item) configurada.</summary>
        [Authorize]
        [HttpGet("with-scope")]
        public async Task<IActionResult> GetAllWithScope()
        {
            try { return Ok(await _service.GetAllWithScopeAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Áreas para el filtro del listado (incluye contenedores include_descendants).</summary>
        [Authorize]
        [HttpGet("for-filter")]
        public async Task<IActionResult> GetAllForFilter()
        {
            try { return Ok(await _service.GetAllForFilterAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPut("toggle/{areaScopeId}")]
        public async Task<IActionResult> Toggle(int areaScopeId)
        {
            try { return Ok(await _service.ToggleAsync(areaScopeId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPut("include-in-form/{areaScopeId}")]
        public async Task<IActionResult> SetIncludeInForm(int areaScopeId, [FromQuery] bool value)
        {
            try { return Ok(await _service.SetIncludeInFormAsync(areaScopeId, value)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPut("include-descendants/{areaScopeId}")]
        public async Task<IActionResult> SetIncludeDescendants(int areaScopeId, [FromQuery] bool value)
        {
            try { return Ok(await _service.SetIncludeDescendantsAsync(areaScopeId, value)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Marca/desmarca el área como independiente en el formulario (requiere "En formulario").</summary>
        [Authorize]
        [HttpPut("include-as-independent/{areaScopeId}")]
        public async Task<IActionResult> SetIncludeAsIndependent(int areaScopeId, [FromQuery] bool value)
        {
            try { return Ok(await _service.SetIncludeAsIndependentAsync(areaScopeId, value)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
