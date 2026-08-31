using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Presentation
{
    [ApiController]
    [Route("api/v1/mejora-continua/scope")]
    public class ScopeController : ControllerBase
    {
        private readonly IScopeService _service;
        public ScopeController(IScopeService service) => _service = service;

        // ── ScopeItem ────────────────────────────────────────────────────────────

        [Authorize]
        [HttpGet("tree/{lessonAreaId}")]
        public async Task<IActionResult> GetTree(int lessonAreaId)
        {
            try { return Ok(await _service.GetScopeTreeAsync(lessonAreaId)); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPut("tree")]
        public async Task<IActionResult> UpsertScope([FromBody] ScopeItemUpsertDTO dto)
        {
            try
            {
                await _service.UpsertScopeAsync(dto);
                return Ok(new { message = "Scope actualizado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── Templates ────────────────────────────────────────────────────────────

        [Authorize]
        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates()
        {
            try { return Ok(await _service.GetTemplatesAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate([FromBody] ScopeTemplateCreateDTO dto)
        {
            try
            {
                await _service.CreateTemplateAsync(dto);
                return Ok(new { message = "Plantilla creada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpPut("templates")]
        public async Task<IActionResult> UpdateTemplate([FromBody] ScopeTemplateUpdateDTO dto)
        {
            try
            {
                await _service.UpdateTemplateAsync(dto);
                return Ok(new { message = "Plantilla actualizada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [Authorize]
        [HttpDelete("templates/{scopeTemplateId}")]
        public async Task<IActionResult> DeleteTemplate(int scopeTemplateId)
        {
            try
            {
                await _service.DeleteTemplateAsync(scopeTemplateId);
                return Ok(new { message = "Plantilla eliminada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
