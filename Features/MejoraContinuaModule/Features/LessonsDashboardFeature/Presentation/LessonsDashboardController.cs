using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Presentation
{
    [ApiController]
    [Route("api/v1/mejora-continua/lessons-dashboard")]
    public class LessonsDashboardController : ControllerBase
    {
        private readonly ILessonsDashboardService _service;

        public LessonsDashboardController(ILessonsDashboardService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("data")]
        public async Task<IActionResult> GetData(
            [FromQuery] DateTimeOffset? periodDate,
            [FromQuery] int? userId,
            [FromQuery] List<int>? lessonAreaIds,
            [FromQuery] int? obraOficinaStaffId,
            [FromQuery] List<int>? projectIds,
            [FromQuery] string? approvalStatus)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetData(periodDate, userId, lessonAreaIds, obraOficinaStaffId, projectIds, approvalStatus);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetFilters();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpPost("send-pdf")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendPdf(IFormFile pdf)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                await _service.SendPdfAsync(pdf);
                return Ok(new { message = "PDF enviado correctamente" });
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
