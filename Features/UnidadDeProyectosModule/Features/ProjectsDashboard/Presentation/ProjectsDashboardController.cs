using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Presentation
{
    [ApiController]
    [Route("api/v1/projects-dashboard")]
    public class ProjectsDashboardController : ControllerBase
    {
        private readonly IProjectsDashboardService _service;

        public ProjectsDashboardController(IProjectsDashboardService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            try
            {
                if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
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
        [HttpGet]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] int? proyectoId,
            [FromQuery] string? estado,
            [FromQuery] int? responsableId,
            [FromQuery] DateOnly? fechaDesde,
            [FromQuery] DateOnly? fechaHasta)
        {
            try
            {
                if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetDashboard(proyectoId, estado, responsableId, fechaDesde, fechaHasta);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [Authorize]
        [HttpGet("{proyectoId:int}")]
        public async Task<IActionResult> GetProyectoDetail(int proyectoId)
        {
            try
            {
                if (User.FindFirst(ClaimTypes.NameIdentifier) == null)
                    return Unauthorized(new { message = "Inicie sesión" });

                var result = await _service.GetProyectoDetail(proyectoId);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
