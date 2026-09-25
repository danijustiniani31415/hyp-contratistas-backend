using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Features.PersonasModule;

namespace Abril_Backend.Features.PersonasModule.Presentation
{
    /// <summary>Fase 2 del motor de Planillas — asistencia diaria (CONTEXT_LOGISTICA.md, ampliación 2026-09-21).</summary>
    [ApiController]
    [Route("api/v1/tareo")]
    [Authorize]
    public class TareoController : ControllerBase
    {
        private readonly ITareoService _service;

        public TareoController(ITareoService service)
        {
            _service = service;
        }

        private long? CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> GetMes([FromQuery] int anio, [FromQuery] int mes)
        {
            try { return Ok(await _service.GetMes(anio, mes)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut]
        public async Task<IActionResult> GuardarMes([FromBody] TareoGuardarDto dto)
        {
            if (!User.HasLbPermiso("TAREO_REGISTRAR"))
                return StatusCode(403, new { message = "No tienes permiso para registrar tareo." });
            try
            {
                await _service.GuardarMes(dto, CurrentUsuarioSistemaId);
                return Ok(new { message = "Tareo guardado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
