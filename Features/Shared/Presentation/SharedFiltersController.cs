using Abril_Backend.Features.Shared.Application.Services;
using Abril_Backend.Features.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Shared.Presentation;

[ApiController]
[Route("api/v1/shared-filters")]
[AllowAnonymous]
public class SharedFiltersController : ControllerBase
{
    private readonly ISharedFiltersService _service;
    private readonly ILogger<SharedFiltersController> _logger;

    public SharedFiltersController(
        ISharedFiltersService service,
        ILogger<SharedFiltersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("proyectos")]
    [ResponseCache(Duration = 3600, VaryByHeader = "Origin")]
    [ProducesResponseType(typeof(IEnumerable<GenericSelectOptionDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<GenericSelectOptionDto>>> GetProyectos()
    {
        try
        {
            var result = await _service.GetProyectosAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo proyectos");
            return StatusCode(500, new { message = "Error al obtener proyectos" });
        }
    }

    [HttpGet("meses")]
    [ResponseCache(Duration = 86400, VaryByHeader = "Origin")]
    [ProducesResponseType(typeof(IEnumerable<GenericSelectOptionDto>), 200)]
    public ActionResult<IEnumerable<GenericSelectOptionDto>> GetMeses()
    {
        try
        {
            var result = _service.GetMeses();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo meses");
            return StatusCode(500, new { message = "Error al obtener meses" });
        }
    }

    [HttpGet("anios")]
    [ResponseCache(Duration = 86400, VaryByHeader = "Origin")]
    [ProducesResponseType(typeof(IEnumerable<GenericSelectOptionDto>), 200)]
    public ActionResult<IEnumerable<GenericSelectOptionDto>> GetAnios()
    {
        try
        {
            var result = _service.GetAnios();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo años");
            return StatusCode(500, new { message = "Error al obtener años" });
        }
    }

    [HttpGet("razones-sociales")]
    [ResponseCache(Duration = 3600, VaryByHeader = "Origin")]
    [ProducesResponseType(typeof(IEnumerable<GenericSelectOptionDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<GenericSelectOptionDto>>> GetRazonesSociales()
    {
        try
        {
            var result = await _service.GetRazonesSocialesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo razones sociales");
            return StatusCode(500, new { message = "Error al obtener razones sociales" });
        }
    }

    [HttpGet("estados")]
    [ResponseCache(Duration = 86400, VaryByHeader = "Origin")]
    [ProducesResponseType(typeof(IEnumerable<GenericSelectOptionDto>), 200)]
    public ActionResult<IEnumerable<GenericSelectOptionDto>> GetEstados()
    {
        try
        {
            var result = _service.GetEstados();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo estados");
            return StatusCode(500, new { message = "Error al obtener estados" });
        }
    }
}
