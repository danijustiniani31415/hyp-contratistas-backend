using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Presentation
{
    [ApiController]
    [Route("api/v1/subarea")]
    public class SubAreaController : ControllerBase
    {
        private readonly ISubAreaService _service;

        public SubAreaController(ISubAreaService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("simple/all")]
        public async Task<IActionResult> GetAllSimple()
        {
            try
            {
                var result = await _service.GetAllSimpleAsync();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("simple")]
        public async Task<IActionResult> GetSimple([FromQuery] int areaId)
        {
            try
            {
                var result = await _service.GetSimpleByAreaAsync(areaId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int? areaId = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });
                if (page < 1) page = 1;
                var result = await _service.GetPaged(page, areaId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SubAreaCreateDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });
                if (string.IsNullOrWhiteSpace(dto.SubAreaDescription))
                    return BadRequest(new { message = "SubAreaDescription es obligatorio." });
                if (dto.AreaId == 0)
                    return BadRequest(new { message = "Debe seleccionar un área." });
                var userId = int.Parse(userIdClaim.Value);
                await _service.CreateAsync(dto, userId);
                return Ok(new { message = "Subárea creada exitosamente" });
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
        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] SubAreaEditDTO dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });
                if (string.IsNullOrWhiteSpace(dto.SubAreaDescription))
                    return BadRequest(new { message = "SubAreaDescription es obligatorio." });
                if (dto.AreaId == 0)
                    return BadRequest(new { message = "Debe seleccionar un área." });
                var userId = int.Parse(userIdClaim.Value);
                await _service.UpdateAsync(dto, userId);
                return Ok(new { message = "Subárea actualizada exitosamente." });
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
        /// Verifica si el área tiene scope configurado a nivel de área (sin subárea).
        /// El frontend lo usa para mostrar aviso antes de crear la primera subárea.
        /// </summary>
        [Authorize]
        [HttpGet("check-scope/{areaId}")]
        public async Task<IActionResult> CheckAreaScope(int areaId)
        {
            try
            {
                var hasScope = await _service.AreaHasScopeAsync(areaId);
                return Ok(new { hasScope });
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
                    return NotFound(new { message = "Subárea no encontrada." });
                return Ok(new { message = "Subárea eliminada exitosamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
