using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.HorasHombreFeature.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma-horas-hombre")]
    [Authorize]
    [RequireFeature("ssoma.gestion.horas-hombre")]
    public class HorasHombreController : ControllerBase
    {
        private readonly IHorasHombreService _service;
        private readonly ILogger<HorasHombreController> _logger;

        public HorasHombreController(IHorasHombreService service, ILogger<HorasHombreController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("tabla")]
        public async Task<IActionResult> GetTabla(
            [FromQuery] int? proyectoId,
            [FromQuery] int? empresaId,
            [FromQuery] DateOnly? fechaDesde,
            [FromQuery] DateOnly? fechaHasta,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var filter = new HorasHombreFilterDto
                {
                    ProyectoId = proyectoId,
                    EmpresaId = empresaId,
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    Page = page,
                    PageSize = pageSize,
                };
                return Ok(await _service.GetTablaAsync(filter));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error GetTabla horas hombre"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard(
            [FromQuery] int? proyectoId,
            [FromQuery] int? mes,
            [FromQuery] int? anio)
        {
            try { return Ok(await _service.GetDashboardAsync(proyectoId, mes, anio)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error GetDashboard horas hombre"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
