using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.OptFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.OptFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.OptFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma-opt")]
[Authorize]
public class OptController : ControllerBase
{
    private readonly IOptService _service;
    private readonly ILogger<OptController> _logger;

    public OptController(IOptService service, ILogger<OptController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    private int? GetEmpresaIdContratista() =>
        User.FindFirst("tipo")?.Value == "CONTRATISTA"
            && int.TryParse(User.FindFirst("empresaId")?.Value, out var id)
            ? id
            : null;

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    [HttpGet("catalogos")]
    public async Task<IActionResult> GetCatalogos()
    {
        try
        {
            var pets      = await _service.GetPetsAsync();
            var criterios = await _service.GetCriteriosVerificacionAsync();
            return Ok(new { pets, criterios });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en OptController.GetCatalogos");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpGet]
    [RequireFeature("ssoma.gestion.opt.lista")]
    public async Task<IActionResult> GetList(
        [FromQuery] int? proyectoId,
        [FromQuery] int? petId,
        [FromQuery] string? tipoObservacion,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int? trabajadorId,
        [FromQuery] int? empresaObservadorId,
        [FromQuery] int? empresaTrabajadorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            return Ok(await _service.GetListAsync(
                proyectoId, petId, tipoObservacion, fechaDesde, fechaHasta, trabajadorId, page, pageSize,
                GetEmpresaIdContratista(), empresaObservadorId, empresaTrabajadorId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en OptController.GetList");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpGet("dashboard")]
    [RequireFeature("ssoma.gestion.opt.dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int? proyectoId,
        [FromQuery] int? anio)
    {
        try
        {
            return Ok(await _service.GetDashboardAsync(proyectoId, anio, GetEmpresaIdContratista()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en OptController.GetDashboard");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpGet("{id:int}")]
    [RequireFeature("ssoma.gestion.opt")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try
        {
            var detalle = await _service.GetDetalleAsync(id);
            var empresaId = GetEmpresaIdContratista();
            if (empresaId.HasValue && !detalle.Trabajadores.Any(t => t.EmpresaId == empresaId.Value))
                return Forbid();
            return Ok(detalle);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"OPT {id} no encontrado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en OptController.GetDetalle");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPost]
    [RequireFeature("ssoma.gestion.opt.nuevo")]
    public async Task<IActionResult> Crear([FromBody] CrearOptRequest request)
    {
        try
        {
            var id = await _service.CrearOptAsync(request, GetUserId());
            return StatusCode(201, new { id });
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en OptController.Crear");
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }
}
