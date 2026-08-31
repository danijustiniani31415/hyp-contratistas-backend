using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Interfaces;

namespace Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Presentation
{
    [ApiController]
    [Route("api/v1/holiday")]
    [Authorize]
    public class HolidayController : ControllerBase
    {
        private readonly IHolidayService _service;
        public HolidayController(IHolidayService service) => _service = service;

        /// <summary>Carga inicial: tipos (para los desplegables) + primera página de la tabla en una sola petición.</summary>
        [HttpGet("initial")]
        public async Task<IActionResult> GetInitial([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _service.GetInitial(page, pageSize);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _service.GetPaged(page, pageSize);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HolidayCreateDto dto)
        {
            try
            {
                await _service.Create(dto);
                return Ok(new { message = "Registro creado exitosamente." });
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
        public async Task<IActionResult> Update([FromBody] HolidayEditDto dto)
        {
            try
            {
                await _service.Update(dto);
                return Ok(new { message = "Registro actualizado exitosamente." });
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
                    return NotFound(new { message = "Registro no encontrado." });
                return Ok(new { message = "Registro eliminado exitosamente." });
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
