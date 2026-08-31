using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AuthModule.Role.Application.Dtos;
using Abril_Backend.Features.AuthModule.Role.Application.Interfaces;

namespace Abril_Backend.Features.AuthModule.Role.Presentation
{
    [ApiController]
    [Route("api/v1/role")]
    [Authorize]
    public class RoleFeatureController : ControllerBase
    {
        private readonly IRoleFeatureService _service;

        public RoleFeatureController(IRoleFeatureService service)
        {
            _service = service;
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetPaged(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                if (string.IsNullOrWhiteSpace(dto.RoleDescription))
                    return BadRequest(new { message = "La descripción del rol es obligatoria." });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Create(dto, userId);
                return Ok(new { message = "Rol creado exitosamente." });
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

        [HttpGet("features/all")]
        public async Task<IActionResult> GetAllFeatures()
        {
            try
            {
                var result = await _service.GetAllFeatures();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("{roleId:int}/features")]
        public async Task<IActionResult> GetRoleFeatures(int roleId)
        {
            try
            {
                var result = await _service.GetRoleFeatureIds(roleId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("{roleId:int}")]
        public async Task<IActionResult> UpdateRoleDescription(int roleId, [FromBody] RoleUpdateDescriptionDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.UpdateRoleDescription(roleId, dto, userId);
                return Ok(new { message = "Rol actualizado exitosamente." });
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

        [HttpPut("{roleId:int}/features")]
        public async Task<IActionResult> UpdateRoleFeatures(int roleId, [FromBody] RoleUpdateFeaturesDto dto)
        {
            try
            {
                await _service.UpdateRoleFeatures(roleId, dto.FeatureIds);
                return Ok(new { message = "Funcionalidades actualizadas exitosamente." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
