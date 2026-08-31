using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Interfaces;

namespace Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Presentation
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CostosCronogramaController : ControllerBase
    {
        private readonly ICostosCronogramaService _service;

        public CostosCronogramaController(ICostosCronogramaService service)
        {
            _service = service;
        }

        [HttpGet("form-data/{projectSubContractorId}")]
        public async Task<IActionResult> GetFormData(int projectSubContractorId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetFormData(projectSubContractorId);
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

        [HttpPost("actividad")]
        public async Task<IActionResult> CreateActividad([FromBody] CronogramaActividadCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _service.CreateActividad(dto, userId);
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

        [HttpPost("{projectSubContractorId}")]
        public async Task<IActionResult> Save(int projectSubContractorId, [FromBody] CronogramaSaveDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                await _service.Save(projectSubContractorId, dto, userId);
                return Ok(new { message = "Cronograma guardado exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CronogramaSave] {ex}");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
