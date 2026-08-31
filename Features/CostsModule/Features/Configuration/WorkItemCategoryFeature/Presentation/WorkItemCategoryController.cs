using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Interfaces;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Presentation
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class WorkItemCategoryController : ControllerBase
    {
        private readonly IWorkItemCategoryService _service;

        public WorkItemCategoryController(IWorkItemCategoryService service)
        {
            _service = service;
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? description,
            [FromQuery] bool? hasInstructivo,
            [FromQuery] bool? hasClause,
            [FromQuery] int? workSpecialtyId,
            [FromQuery] bool? active,
            [FromQuery] int page = 1)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var filter = new WorkItemCategoryFilterDto
                {
                    Description = description,
                    HasInstructivo = hasInstructivo,
                    HasClause = hasClause,
                    WorkSpecialtyId = workSpecialtyId,
                    Active = active,
                    Page = page
                };

                var result = await _service.GetPaged(filter);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("specialties")]
        public async Task<IActionResult> GetSpecialties()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetActiveSpecialties();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WorkItemCategoryCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Create(dto, userId);
                return Ok(new { message = "Partida de control creada exitosamente." });
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
        public async Task<IActionResult> Update([FromBody] WorkItemCategoryEditDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Update(dto, userId);
                return Ok(new { message = "Categoría de partida actualizada exitosamente." });
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

        [HttpPost("{workItemCategoryId}/upload-instructivo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadInstructivo(int workItemCategoryId, IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.UploadInstructivoAsync(workItemCategoryId, file, userId);
                return Ok(new { message = "Instructivo subido exitosamente." });
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

        [HttpPost("sync-instructivos")]
        public async Task<IActionResult> SyncInstructivos()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.SyncInstructivosAsync(userId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpDelete("{workItemCategoryId}")]
        public async Task<IActionResult> Delete(int workItemCategoryId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.Delete(workItemCategoryId, userId);
                if (!result)
                    return NotFound(new { message = "La categoría no existe." });

                return Ok(new { message = "Categoría de partida eliminada exitosamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
