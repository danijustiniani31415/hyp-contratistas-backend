using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Interfaces;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Presentation
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ProjectLinkController : ControllerBase
    {
        private readonly IProjectLinkService _service;

        public ProjectLinkController(IProjectLinkService service)
        {
            _service = service;
        }

        private const string RolAdministrador  = Roles.CostosAdministrador;
        private const string RolOficinaCentral = Roles.CostosOficinaCentral;
        private const string RolOficinaTecnica = Roles.CostosOficinaTecnica;

        /// <summary>
        /// Oficina Técnica (sin Admin ni Of. Central) solo gestiona links de sus proyectos
        /// (aquellos donde su correo está registrado en staff_project_email).
        /// </summary>
        private bool RestrictToOwnProjects()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return roles.Contains(RolOficinaTecnica)
                && !roles.Contains(RolAdministrador)
                && !roles.Contains(RolOficinaCentral);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int? projectId,
            [FromQuery] int page = 1)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var filter = new ProjectLinkFilterDto
                {
                    ProjectId = projectId,
                    Page = page
                };

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.GetPaged(filter, userId, RestrictToOwnProjects());
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("form-data")]
        public async Task<IActionResult> GetFormData()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.GetFormData(userId, RestrictToOwnProjects());
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectLinkCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Create(dto, userId, RestrictToOwnProjects());
                return Ok(new { message = "Link registrado exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = advertencia de configuración/permiso).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProjectLinkUpdateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Update(dto, userId, RestrictToOwnProjects());
                return Ok(new { message = "Link actualizado exitosamente." });
            }
            catch (AbrilException ex)
            {
                // Respeta el código de la excepción (p. ej. 422 = advertencia de configuración/permiso).
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpDelete("{projectLinkId}")]
        public async Task<IActionResult> Delete(int projectLinkId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.Delete(projectLinkId, userId, RestrictToOwnProjects());
                if (!result)
                    return NotFound(new { message = "El link no existe." });

                return Ok(new { message = "Link eliminado exitosamente." });
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
