using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AccidenteTrabajo;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.accidentes")]
    public class AccidenteTrabajoController : ControllerBase
    {
        private readonly IAccidenteTrabajoService _service;
        private readonly ILogger<AccidenteTrabajoController> _logger;

        public AccidenteTrabajoController(IAccidenteTrabajoService service, ILogger<AccidenteTrabajoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        [HttpGet("accidentes")]
        public async Task<IActionResult> GetList([FromQuery] AccidenteFilterDto filter)
        {
            try { return Ok(await _service.ListPaged(filter)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("accidentes/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try { return Ok(await _service.GetById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("accidentes")]
        public async Task<IActionResult> Create([FromBody] AccidenteTrabajoCreateDto dto)
        {
            try
            {
                var id = await _service.Create(dto, CurrentUserId());
                return Ok(new { id, message = "Accidente de trabajo registrado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("accidentes/{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AccidenteTrabajoUpdateDto dto)
        {
            try
            {
                await _service.Update(id, dto);
                return Ok(new { message = "Accidente de trabajo actualizado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("accidentes/{id:int}/cerrar")]
        public async Task<IActionResult> Cerrar(int id, [FromBody] AccidenteCerrarDto dto)
        {
            try
            {
                await _service.Cerrar(id, dto, CurrentUserId());
                return Ok(new { message = "Accidente dado de alta exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("accidentes/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.Delete(id);
                return Ok(new { message = "Accidente de trabajo eliminado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("accidentes/{id:int}/reinduccion")]
        public async Task<IActionResult> MarcarReinduccion(int id)
        {
            try
            {
                await _service.MarcarReinduccionAsync(id, CurrentUserId());
                return Ok(new { message = "Reinducción de seguridad registrada. El trabajador puede reintegrarse." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("accidentes/{accidenteId:int}/seguimientos")]
        public async Task<IActionResult> CreateSeguimiento(int accidenteId, [FromBody] AccidenteSeguimientoCreateDto dto)
        {
            try
            {
                var id = await _service.CreateSeguimiento(accidenteId, dto, CurrentUserId());
                return Ok(new { id, message = "Seguimiento registrado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("accidentes/seguimientos/{seguimientoId:int}")]
        public async Task<IActionResult> DeleteSeguimiento(int seguimientoId)
        {
            try
            {
                await _service.DeleteSeguimiento(seguimientoId);
                return Ok(new { message = "Seguimiento eliminado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AccidenteTrabajoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
