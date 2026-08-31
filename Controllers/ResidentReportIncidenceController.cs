using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Controllers
{

    [ApiController]
    [Route("api/v1/[controller]")]
    public class ResidentReportIncidenceController : ControllerBase
    {
        IResidentReportIncidenceService _service;
        public ResidentReportIncidenceController(IResidentReportIncidenceService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int? projectId = null,
            [FromQuery] int? stateId = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });
                if (page < 1)
                    page = 1;

                var userId = int.Parse(userIdClaim.Value);
                var isResidente = User.IsInRole(Roles.Residente);

                var result = await _service.GetPaged(page, userId, isResidente, projectId, stateId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("assigned-projects")]
        public async Task<IActionResult> GetAssignedProjects()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var isResidente = User.IsInRole(Roles.Residente);

                var result = await _service.GetAssignedProjects(userId, isResidente);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize(Roles = Roles.AdministradorResidentes)]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateIncidence([FromForm] ResidentReportIncidenceCreateDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                await _service.Create(dto, userId);
                return Ok(new { message = "Reporte creado exitosamente" });
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

        [Authorize(Roles = Roles.Residente)]
        [HttpPost("response")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateResponse(ResidentReportResponseCreateDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                await _service.CreateResponse(dto, userId);
                return Ok(new { message = "Respuesta creada exitosamente" });
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

        [Authorize(Roles = Roles.AdministradorResidentes)]
        [HttpPatch]
        public async Task<IActionResult> UpdateIncidenceState(UpdateIncidenceDTO incidenceId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                await _service.UpdateIncidenceState(incidenceId, userId);
                return Ok(new { message = "Incidencia levantada exitosamente" });
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
    }
}