using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Application.Interfaces;

namespace Abril_Backend.Controllers
{

    [ApiController]
    [Route("api/v1/[controller]")]
    public class ResidentMonitoringController : ControllerBase
    {
        IResidentMonitoringService _residentMonitoringService;
        public ResidentMonitoringController(IResidentMonitoringService residentMonitoringService)
        {
            _residentMonitoringService = residentMonitoringService;
        }

        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] TrackingQueryDto query)
        {
            try
            {
                /*var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });*/

                var result = await _residentMonitoringService.GetTrackingAsync(query);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters([FromQuery] TrackingQueryDto query)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _residentMonitoringService.GetFilters();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}