using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.SctrGestion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.asistente-social")]
    public class SctrGestionController : ControllerBase
    {
        private readonly ISctrGestionService _service;
        private readonly ILogger<SctrGestionController> _logger;

        public SctrGestionController(ISctrGestionService service, ILogger<SctrGestionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        [HttpGet("asistente-social/{casoSocialId:guid}/sctr")]
        public async Task<IActionResult> GetByCasoSocial(Guid casoSocialId)
        {
            try { return Ok(await _service.GetByCasoSocialId(casoSocialId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrGestionController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("asistente-social/{casoSocialId:guid}/sctr")]
        public async Task<IActionResult> Create(Guid casoSocialId, [FromBody] SctrGestionCreateDto dto)
        {
            try
            {
                var id = await _service.Create(casoSocialId, dto, CurrentUserId());
                return Ok(new { id, message = "Gestión SCTR registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrGestionController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("asistente-social/sctr/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SctrGestionUpdateDto dto)
        {
            try
            {
                await _service.Update(id, dto);
                return Ok(new { message = "Gestión SCTR actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrGestionController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("asistente-social/sctr/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.Delete(id);
                return Ok(new { message = "Gestión SCTR eliminada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrGestionController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
