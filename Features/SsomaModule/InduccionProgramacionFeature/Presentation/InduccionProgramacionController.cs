using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Interfaces;
using Abril_Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/induccion-programacion")]
    [Authorize(Roles = $"{Roles.AdministradorSsoma},{Roles.CoordinadorSsoma},{Roles.Prevencionista}")]
    public class InduccionProgramacionController : ControllerBase
    {
        private readonly IInduccionProgramacionService _service;
        private readonly IConfiguration _configuration;

        public InduccionProgramacionController(IInduccionProgramacionService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
        }

        [HttpGet("proyectos-disponibles")]
        public async Task<IActionResult> GetProyectosDisponibles()
        {
            try { return Ok(await _service.GetProyectosDisponiblesAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("responsables-disponibles")]
        public async Task<IActionResult> GetResponsablesDisponibles([FromQuery] int proyectoId)
        {
            try { return Ok(await _service.GetResponsablesDisponiblesAsync(proyectoId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("rotacion")]
        public async Task<IActionResult> GetRotacion()
        {
            try { return Ok(await _service.GetRotacionAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("rotacion")]
        public async Task<IActionResult> AgregarARotacion([FromBody] RotacionAgregarDto dto)
        {
            try { return Ok(await _service.AgregarARotacionAsync(dto.ProyectoId, dto.ResponsableWorkerId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("rotacion/{id:int}/responsable")]
        public async Task<IActionResult> SetResponsable(int id, [FromBody] RotacionResponsableDto dto)
        {
            try { await _service.SetResponsableAsync(id, dto.ResponsableWorkerId); return NoContent(); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("rotacion/reordenar")]
        public async Task<IActionResult> Reordenar([FromBody] RotacionReordenarDto dto)
        {
            try { await _service.ReordenarAsync(dto); return NoContent(); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("rotacion/{id:int}/activo")]
        public async Task<IActionResult> SetActivo(int id, [FromBody] RotacionActivoDto dto)
        {
            try { await _service.SetActivoAsync(id, dto.Activo); return NoContent(); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("calendario")]
        public async Task<IActionResult> GetCalendario([FromQuery] DateOnly desde, [FromQuery] DateOnly hasta)
        {
            try { return Ok(await _service.GetCalendarioAsync(desde, hasta)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("calendario/{id:int}/reasignar")]
        public async Task<IActionResult> Reasignar(int id, [FromBody] ProgramacionReasignarDto dto)
        {
            try { await _service.ReasignarAsync(id, dto); return NoContent(); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("calendario/{id:int}/responsable")]
        public async Task<IActionResult> SetProgramacionResponsable(int id, [FromBody] ProgramacionResponsableDto dto)
        {
            try { await _service.SetProgramacionResponsableAsync(id, dto.ResponsableWorkerId); return NoContent(); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("calendario/{id:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int id, [FromBody] ProgramacionCancelarDto dto)
        {
            try { await _service.CancelarAsync(id, dto); return NoContent(); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("calendario/{id:int}/reprogramar")]
        public async Task<IActionResult> Reprogramar(int id, [FromBody] ProgramacionReprogramarDto dto)
        {
            try { await _service.ReprogramarAsync(id, dto); return NoContent(); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Endpoint de cron (mismo patrón que EmoAlertaController): debe programarse
        /// externamente para correr TODOS los días hábiles a las 3:00pm (hora Lima) y además
        /// los SÁBADOS a las 10:00am (para el aviso de las inducciones de los lunes).
        /// </summary>
        [HttpGet("aviso")]
        [AllowAnonymous]
        public async Task<IActionResult> EnviarAvisos()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                return Ok(await _service.EnviarAvisosPendientesAsync());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
