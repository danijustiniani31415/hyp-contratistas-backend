using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Presentation
{
    [ApiController]
    [Route("api/v1/project")]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProjectController(IProjectService service)
        {
            _service = service;
        }

        [HttpGet("company-lookup/{ruc}")]
        [EnableRateLimiting("sunat-ruc")]
        public async Task<IActionResult> CompanyLookup(string ruc)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.GetOrCreateCompanyByRuc(ruc, userId);

                if (result == null)
                    return NotFound(new { message = "No se encontró información para el RUC proporcionado." });

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 200,
            [FromQuery] string? ruc = null,
            [FromQuery] string? razonSocial = null,
            [FromQuery] string? projectDescription = null,
            [FromQuery] bool? active = null)
        {
            try
            {
                // Token interno: NameIdentifier = userId. Token Microsoft (AzureAd): oid como fallback.
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("oid");
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetPaged(page, pageSize, ruc, razonSocial, projectDescription, active);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                if (string.IsNullOrWhiteSpace(dto.ProjectDescription))
                    return BadRequest(new { message = "La descripción del proyecto es obligatoria." });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Create(dto, userId);
                return Ok(new { message = "Proyecto creado exitosamente." });
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

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ProjectEditDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                if (string.IsNullOrWhiteSpace(dto.ProjectDescription))
                    return BadRequest(new { message = "La descripción del proyecto es obligatoria." });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Update(dto, userId);
                return Ok(new { message = "Proyecto actualizado exitosamente." });
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

        [HttpDelete("{projectId}")]
        public async Task<IActionResult> Delete(int projectId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.DeleteSoftAsync(projectId, userId);

                if (!result)
                    return NotFound(new { message = "Proyecto no encontrado." });

                return Ok(new { message = "Proyecto eliminado exitosamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Proyectos asignados al usuario logueado (tabla `user_project`), para
        /// preseleccionar el proyecto en dashboards que hoy arrancan sin nada elegido.</summary>
        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var projectIds = await _service.GetMyProjectIds(userId);
                return Ok(projectIds);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("responsables")]
        public async Task<IActionResult> GetResponsables([FromQuery] string tipo)
        {
            try
            {
                var result = await _service.GetResponsables(tipo);
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
        [HttpGet("{id}/emails")]
        public async Task<IActionResult> GetEmails(int id)
        {
            try
            {
                var emails = await _service.GetEmails(id);
                if (emails == null)
                    return NotFound(new { message = "Proyecto no encontrado." });

                return Ok(emails);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPatch("{id}/emails")]
        public async Task<IActionResult> UpdateEmails(int id, [FromBody] ProjectEmailsUpdateDto dto)
        {
            try
            {
                // var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                //
                // if (userIdClaim == null)
                //     return Unauthorized(new { message = "Inicie sesión" });

                await _service.UpdateEmails(id, dto);

                return Ok(new { message = "Emails actualizados correctamente." });
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

        /// <summary>
        /// Activa/desactiva rápidamente si el proyecto aparece en los selectores de
        /// Arquitectura Comercial (Observaciones, etc.) sin pasar por el formulario
        /// completo de edición de proyecto.
        /// </summary>
        [Authorize]
        [RequireFeature("arquitectura-comercial.observaciones")]
        [HttpPatch("{id}/arquitectura-comercial")]
        public async Task<IActionResult> ToggleArquitecturaComercial(int id)
        {
            try
            {
                var nuevoValor = await _service.ToggleArquitecturaComercial(id);
                if (nuevoValor == null)
                    return NotFound(new { message = "Proyecto no encontrado." });

                return Ok(new { tieneArquitecturaComercial = nuevoValor.Value });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
