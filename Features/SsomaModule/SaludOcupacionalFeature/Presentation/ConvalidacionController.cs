using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/convalidaciones")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.convalidaciones")]
    public class ConvalidacionController : ControllerBase
    {
        private readonly IConvalidacionService _service;
        private readonly ILogger<ConvalidacionController> _logger;

        public ConvalidacionController(IConvalidacionService service, ILogger<ConvalidacionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private string? ClientUserAgent() => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] ConvalidacionFilterDto filter)
        {
            try { return Ok(await _service.List(filter)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ConvalidacionController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ConvalidacionCreateDto dto)
        {
            try
            {
                var id = await _service.Create(dto, CurrentUserId(), ClientIp(), ClientUserAgent());
                return Ok(new { id, message = "Convalidación registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ConvalidacionController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ConvalidacionUpdateDto dto)
        {
            try
            {
                await _service.Update(id, dto, CurrentUserId(), ClientIp(), ClientUserAgent());
                return Ok(new { message = "Convalidación actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ConvalidacionController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> GetPdf(int id)
        {
            try
            {
                var bytes = await _service.GenerarPdfAsync(id);
                return File(bytes, "application/pdf", $"Convalidacion_{id}.pdf");
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error generando PDF de convalidación"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
