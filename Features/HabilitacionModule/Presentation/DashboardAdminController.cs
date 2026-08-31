using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/dashboard")]
    [Authorize]
    public class DashboardAdminController : ControllerBase
    {
        private readonly IDashboardHabRepository _repo;
        private readonly ILogger<DashboardAdminController> _logger;

        public DashboardAdminController(IDashboardHabRepository repo, ILogger<DashboardAdminController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetResumen([FromQuery] int proyectoId)
        {
            try
            {
                var result = await _repo.GetResumenAsync(proyectoId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en DashboardAdminController.GetResumen"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
