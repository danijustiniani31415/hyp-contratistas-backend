using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/evaluaciones/asignaciones-supervisor")]
    [Authorize]
    public class EvAsignacionSupervisorController : ControllerBase
    {
        private readonly IEvAsignacionSupervisorService _service;
        private readonly ILogger<EvAsignacionSupervisorController> _logger;

        public EvAsignacionSupervisorController(
            IEvAsignacionSupervisorService service,
            ILogger<EvAsignacionSupervisorController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        [HttpGet("proyectos")]
        public async Task<IActionResult> GetProyectosActivos()
        {
            try { return Ok(await _service.GetProyectosActivosAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvAsignacionSupervisorController.GetProyectosActivos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupervisores()
        {
            try { return Ok(await _service.GetSupervisoresConAsignacionesAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvAsignacionSupervisorController.GetSupervisores"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{supervisorWorkerId:int}")]
        public async Task<IActionResult> ActualizarAsignaciones(int supervisorWorkerId, [FromBody] ActualizarAsignacionesDto dto)
        {
            try
            {
                await _service.ActualizarAsignacionesAsync(supervisorWorkerId, dto.ProjectIds, GetUserId());
                return Ok(new { message = "Asignaciones actualizadas correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvAsignacionSupervisorController.ActualizarAsignaciones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
