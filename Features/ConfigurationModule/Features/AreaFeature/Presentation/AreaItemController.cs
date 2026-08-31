using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Interfaces;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Presentation
{
    [ApiController]
    [Route("api/v1/area-item")]
    [Authorize]
    public class AreaItemController : ControllerBase
    {
        private readonly IAreaItemService _service;
        public AreaItemController(IAreaItemService service) => _service = service;

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? areaTypeId = null,
            [FromQuery] bool? active = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await _service.GetPaged(new AreaItemFilterDto
                {
                    Page = page,
                    PageSize = pageSize,
                    AreaTypeId = areaTypeId,
                    Active = active,
                    Search = search
                });
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("simple")]
        public async Task<IActionResult> GetSimple([FromQuery] int? areaTypeId = null)
        {
            try
            {
                var result = await _service.GetSimple(areaTypeId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AreaItemCreateDto dto)
        {
            try
            {
                await _service.Create(dto);
                return Ok(new { message = "Área creada exitosamente." });
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
        public async Task<IActionResult> Update([FromBody] AreaItemEditDto dto)
        {
            try
            {
                await _service.Update(dto);
                return Ok(new { message = "Área actualizada exitosamente." });
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteSoftAsync(id);
                if (!result)
                    return NotFound(new { message = "Área no encontrada." });
                return Ok(new { message = "Área eliminada exitosamente." });
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
