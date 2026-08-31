using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/dashboard")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.dashboard", "clinica.agenda")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _service;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService service, ILogger<DashboardController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            try { return Ok(await _service.GetDashboard()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetDashboard");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
