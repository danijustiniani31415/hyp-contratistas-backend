using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.Paso.Dtos;
using Abril_Backend.Features.Ssoma.Paso.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Ssoma.Paso;

[ApiController]
[Route("api/v1/ssoma-paso")]
[Authorize]
public class PasoController : ControllerBase
{
    private readonly IPasoService _service;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasoController> _logger;

    public PasoController(IPasoService service, IConfiguration configuration, ILogger<PasoController> logger)
    {
        _service = service;
        _configuration = configuration;
        _logger = logger;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    [HttpGet("categorias")]
    public async Task<IActionResult> GetCategorias()
    {
        try { return Ok(await _service.GetCategoriasAsync()); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetCategorias"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int? anio)
    {
        try { return Ok(await _service.GetDashboardAsync(anio)); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetDashboard"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> GetAlertas()
    {
        try { return Ok(await _service.GetAlertasAsync()); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetAlertas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("cron/procesar")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcesarCron()
    {
        try
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                return Unauthorized();
            await _service.ProcesarCronAsync();
            return Ok(new { message = "OK" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.ProcesarCron"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] PasoListQuery q)
    {
        try { return Ok(await _service.GetListAsync(q)); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetList"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try
        {
            var r = await _service.GetDetalleAsync(id);
            return r is null ? NotFound(new { message = "PASO no encontrado." }) : Ok(r);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetDetalle"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearPasoRequest req)
    {
        try { return StatusCode(201, await _service.CrearAsync(req, GetUserId())); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.Crear"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarPasoRequest req)
    {
        try { return Ok(await _service.EditarAsync(id, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.Editar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("{id:int}/aprobar")]
    public async Task<IActionResult> Aprobar(int id)
    {
        try { return Ok(await _service.AprobarAsync(id, GetUserId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.Aprobar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("{id:int}/instanciar")]
    public async Task<IActionResult> Instanciar(int id, [FromBody] InstanciarPasoRequest req)
    {
        try { return StatusCode(201, await _service.InstanciarAsync(id, req, GetUserId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.Instanciar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("proyecto/{proyectoId:int}/historico")]
    public async Task<IActionResult> GetHistoricoProyecto(int proyectoId)
    {
        try { return Ok(await _service.GetHistoricoProyectoAsync(proyectoId)); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetHistoricoProyecto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}/spi")]
    public async Task<IActionResult> GetSpi(int id)
    {
        try { return Ok(await _service.GetSpiAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetSpi"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("actividad/{id:int}/auditoria")]
    public async Task<IActionResult> GetAuditoria(int id)
    {
        try { return Ok(await _service.GetAuditoriaAsync(id)); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetAuditoria"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("salud/actividades")]
    public async Task<IActionResult> GetActividadesSalud([FromQuery] PasoSaludListQuery q)
    {
        try { return Ok(await _service.GetActividadesSaludAsync(q)); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetActividadesSalud"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpGet("{id:int}/resumen-mes")]
    public async Task<IActionResult> GetResumenMes(int id, [FromQuery] int anio, [FromQuery] int mes)
    {
        try { return Ok(await _service.GetResumenMesAsync(id, anio, mes)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.GetResumenMes"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("actividad")]
    public async Task<IActionResult> CrearActividad([FromBody] CrearActividadRequest req)
    {
        try { return StatusCode(201, await _service.CrearActividadAsync(req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.CrearActividad"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPut("actividad/{id:int}")]
    public async Task<IActionResult> EditarActividad(int id, [FromBody] EditarActividadRequest req)
    {
        try { return Ok(await _service.EditarActividadAsync(id, req)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.EditarActividad"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpDelete("actividad/{id:int}")]
    public async Task<IActionResult> DeleteActividad(int id, [FromBody] EliminarActividadRequest req)
    {
        try { await _service.DeleteActividadAsync(id, req.Motivo, GetUserId()); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.DeleteActividad"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("ejecucion")]
    public async Task<IActionResult> RegistrarEjecucion([FromBody] RegistrarEjecucionRequest req)
    {
        try { return Ok(await _service.RegistrarEjecucionAsync(req, GetUserId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.RegistrarEjecucion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("ejecucion/programar")]
    public async Task<IActionResult> ProgramarEjecucion([FromBody] ProgramarEjecucionRequest req)
    {
        try { return Ok(await _service.ProgramarEjecucionAsync(req)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.ProgramarEjecucion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("ejecucion/{id:int}/reprogramar")]
    public async Task<IActionResult> ReprogramarEjecucion(int id, [FromBody] ReprogramarEjecucionRequest req)
    {
        try { return Ok(await _service.ReprogramarEjecucionAsync(id, req, GetUserId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.ReprogramarEjecucion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPatch("ejecucion/{id:int}/evidencia")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SubirEvidencia(int id, [FromForm] IFormFile file)
    {
        try { return Ok(await _service.SubirEvidenciaAsync(id, file, GetUserId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.SubirEvidencia"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpPost("ejecucion/{id:int}/archivos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AgregarArchivoEjecucion(int id, [FromForm] IFormFile file)
    {
        try { return StatusCode(201, await _service.AgregarArchivoEjecucionAsync(id, file, GetUserId())); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.AgregarArchivoEjecucion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }

    [HttpDelete("ejecucion/archivos/{archivoId:int}")]
    public async Task<IActionResult> EliminarArchivoEjecucion(int archivoId)
    {
        try { await _service.EliminarArchivoEjecucionAsync(archivoId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en PasoController.EliminarArchivoEjecucion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
    }
}
