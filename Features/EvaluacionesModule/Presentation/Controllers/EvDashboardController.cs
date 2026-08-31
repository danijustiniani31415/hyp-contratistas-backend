using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/evaluaciones/dashboard")]
    [Authorize]
    public class EvDashboardController : ControllerBase
    {
        private readonly IEvDashboardRepository _dashRepo;
        private readonly IEvPeriodoRepository _periodoRepo;
        private readonly ILogger<EvDashboardController> _logger;

        public EvDashboardController(
            IEvDashboardRepository dashRepo,
            IEvPeriodoRepository periodoRepo,
            ILogger<EvDashboardController> logger)
        {
            _dashRepo = dashRepo;
            _periodoRepo = periodoRepo;
            _logger = logger;
        }

        // Nota: estos endpoints son de solo lectura (resumen/dashboard) — a falta de periodoId
        // explícito, deben caer al ÚLTIMO período registrado (GetUltimoAsync), no al período
        // "activo" (que solo existe durante la ventana de evaluación, día 25 -> día 4). El
        // dashboard debe poder verse todo el mes, no solo esos días.

        [HttpGet("gerencia")]
        public async Task<IActionResult> GetGerencia([FromQuery] int? periodoId)
        {
            try
            {
                var pid = periodoId ?? (await _periodoRepo.GetUltimoAsync())?.Id;
                if (pid == null) return NotFound(new { message = "No hay períodos registrados." });
                return Ok(await _dashRepo.GetDashboardGerenciaAsync(pid.Value));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvDashboardController.GetGerencia"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("residentes")]
        public async Task<IActionResult> GetResidentes([FromQuery] int? periodoId)
        {
            try
            {
                var pid = periodoId ?? (await _periodoRepo.GetUltimoAsync())?.Id;
                if (pid == null) return NotFound(new { message = "No hay períodos registrados." });
                return Ok(await _dashRepo.GetResidentesResumenAsync(pid.Value));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvDashboardController.GetResidentes"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas([FromQuery] int? periodoId)
        {
            try
            {
                var pid = periodoId ?? (await _periodoRepo.GetUltimoAsync())?.Id;
                if (pid == null) return NotFound(new { message = "No hay períodos registrados." });
                return Ok(await _dashRepo.GetPromediosPorAreaAsync(pid.Value));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvDashboardController.GetAreas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("tendencia")]
        public async Task<IActionResult> GetTendencia()
        {
            try { return Ok(await _dashRepo.GetTendenciaAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvDashboardController.GetTendencia"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("pendientes")]
        public async Task<IActionResult> GetPendientes([FromQuery] int? periodoId)
        {
            try
            {
                var pid = periodoId ?? (await _periodoRepo.GetUltimoAsync())?.Id;
                if (pid == null) return NotFound(new { message = "No hay períodos registrados." });
                return Ok(await _dashRepo.GetPendientesAsync(pid.Value));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvDashboardController.GetPendientes"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
