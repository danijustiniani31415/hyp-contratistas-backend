using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Interfaces;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Presentation
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class WorkItemController : ControllerBase
    {
        private readonly IWorkItemService _service;

        public WorkItemController(IWorkItemService service)
        {
            _service = service;
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? description,
            [FromQuery] bool? hasValorizationForm,
            [FromQuery] int? workItemCategoryId,
            [FromQuery] bool? active,
            [FromQuery] int page = 1)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var filter = new WorkItemFilterDto
                {
                    Description = description,
                    HasValorizationForm = hasValorizationForm,
                    WorkItemCategoryId = workItemCategoryId,
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

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetActiveCategories();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.SyncPartidasAsync(userId);
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WorkItemCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Create(dto, userId);
                return Ok(new { message = "Partida creada exitosamente." });
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
        public async Task<IActionResult> Update([FromBody] WorkItemEditDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Update(dto, userId);
                return Ok(new { message = "Partida actualizada exitosamente." });
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

        [HttpDelete("{workItemId}")]
        public async Task<IActionResult> Delete(int workItemId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.Delete(workItemId, userId);
                if (!result)
                    return NotFound(new { message = "La partida no existe." });

                return Ok(new { message = "Partida eliminada exitosamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
