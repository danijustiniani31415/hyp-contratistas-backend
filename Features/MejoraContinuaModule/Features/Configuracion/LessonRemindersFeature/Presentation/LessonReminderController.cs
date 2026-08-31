using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Presentation
{
    [ApiController]
    [Route("api/v1/lesson-reminder")]
    public class LessonReminderController : ControllerBase
    {
        private readonly ILessonReminderService _service;

        public LessonReminderController(ILessonReminderService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? subarea = null, [FromQuery] int? workerId = null, [FromQuery] bool includeWorkers = false, [FromQuery] int? obraOficinaStaffId = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                if (page < 1) page = 1;
                var result = await _service.GetPaged(page, pageSize, subarea, workerId, includeWorkers, obraOficinaStaffId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("create-data")]
        public async Task<IActionResult> GetCreateData()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetCreateData();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LessonReminderCreateDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                if (dto.WorkerId <= 0)
                    return BadRequest(new { message = "Debe seleccionar un trabajador." });
                if (dto.ProjectId <= 0)
                    return BadRequest(new { message = "Debe seleccionar un proyecto." });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Create(dto, userId);
                return Ok(new { message = "Recordatorio creado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPut("{id}/project")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] LessonReminderUpdateProjectDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                if (dto.ProjectId <= 0)
                    return BadRequest(new { message = "Debe seleccionar un proyecto." });

                var userId = int.Parse(userIdClaim.Value);
                await _service.UpdateProjectAsync(id, dto.ProjectId, userId);
                return Ok(new { message = "Recordatorio actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.DeleteSoftAsync(id, userId);
                if (!result)
                    return NotFound(new { message = "Recordatorio no encontrado." });
                return Ok(new { message = "Recordatorio eliminado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPut("{id}/toggle")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.ToggleActiveAsync(id, userId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Filtro project_staff_reminder (lista + toggle)
        // ─────────────────────────────────────────────────────────────────────

        [Authorize]
        [HttpGet("project-staff")]
        public async Task<IActionResult> GetProjectStaff()
        {
            try
            {
                var result = await _service.GetAllProjectStaffAsync();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPut("project-staff/toggle/{projectId}")]
        public async Task<IActionResult> ToggleProjectStaff(int projectId)
        {
            try
            {
                var result = await _service.ToggleProjectStaffAsync(projectId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Jefaturas (lesson_jefe_reminder): lista + toggle del recordatorio del 4.º día
        // ─────────────────────────────────────────────────────────────────────

        [Authorize]
        [HttpGet("jefe")]
        public async Task<IActionResult> GetJefes()
        {
            try
            {
                var result = await _service.GetAllJefesAsync();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Revisor de Trabajadores (workers.worker_lesson_jefe_id)
        // ─────────────────────────────────────────────────────────────────────

        [Authorize]
        [HttpGet("worker-revisor")]
        public async Task<IActionResult> GetWorkerRevisores()
        {
            try
            {
                var result = await _service.GetWorkerRevisoresAsync();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("worker-revisor/options")]
        public async Task<IActionResult> GetWorkerRevisorOptions()
        {
            try
            {
                var result = await _service.GetWorkerRevisorOptionsAsync();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPut("worker-revisor/{workerId}")]
        public async Task<IActionResult> UpdateWorkerRevisor(int workerId, [FromBody] WorkerRevisorUpdateDTO dto)
        {
            try
            {
                await _service.UpdateWorkerRevisorAsync(workerId, dto.JefeWorkerId);
                return Ok(new { message = "Jefe actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPut("worker-revisor/{workerId}/auto-approve")]
        public async Task<IActionResult> ToggleAutoApproveLesson(int workerId)
        {
            try
            {
                var result = await _service.ToggleAutoApproveLessonAsync(workerId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPut("jefe/toggle/{workerId}")]
        public async Task<IActionResult> ToggleJefe(int workerId)
        {
            try
            {
                var result = await _service.ToggleJefeAsync(workerId);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
