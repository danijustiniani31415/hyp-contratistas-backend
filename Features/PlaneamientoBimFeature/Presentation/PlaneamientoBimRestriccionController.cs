using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Presentation
{
    [ApiController]
    [Route("api/v1/planeamiento-bim/restricciones")]
    [Authorize]
    [RequireFeature("planeamiento-bim.configuracion-inicial")]
    public class PlaneamientoBimRestriccionController : ControllerBase
    {
        private readonly IPlaneamientoBimRestriccionService _service;

        public PlaneamientoBimRestriccionController(IPlaneamientoBimRestriccionService service)
        {
            _service = service;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("oid");
            return claim != null ? int.Parse(claim.Value) : null;
        }

        [HttpGet("{projectId:int}")]
        public async Task<IActionResult> GetPaged(int projectId, [FromQuery] bool? soloActivos)
        {
            try
            {
                return Ok(await _service.GetPaged(projectId, soloActivos));
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

        [HttpPost("{projectId:int}")]
        public async Task<IActionResult> Create(int projectId, [FromBody] RestriccionCreateDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.Create(projectId, dto, userId.Value);
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] RestriccionUpdateDto dto)
        {
            try
            {
                return Ok(await _service.Update(id, dto));
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

        [HttpPut("{id:int}/cerrar")]
        public async Task<IActionResult> Cerrar(int id)
        {
            try
            {
                return Ok(await _service.Cerrar(id));
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
