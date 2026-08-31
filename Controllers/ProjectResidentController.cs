using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Abril_Backend.Application.Interfaces;

namespace Abril_Backend.Controllers
{

    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProjectResidentController : ControllerBase
    {
        IProjectResidentService _projectResidentService;
        public ProjectResidentController(IProjectResidentService projectResidentService)
        {
            _projectResidentService = projectResidentService;
        }

        [Authorize]
        [HttpGet("with-resident-by-userId")]
        public async Task<IActionResult> GetWithResidentByUserId()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);
                var result = await _projectResidentService.GetProjectByResidentUserId(userId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("projects")]
        public async Task<IActionResult> GetProjectsDescription()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var userId = int.Parse(userIdClaim.Value);

                var result = await _projectResidentService.GetProjectsDescription();
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}