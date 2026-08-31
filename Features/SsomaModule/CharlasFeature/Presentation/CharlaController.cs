using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma-charlas")]
[Authorize]
[RequireFeature("ssoma.gestion.charlas")]
public class CharlaController : ControllerBase
{
    private readonly ICharlaService _svc;
    private readonly ILogger<CharlaController> _logger;

    public CharlaController(ICharlaService svc, ILogger<CharlaController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    // ── Project detection ─────────────────────────────────────────────────────

    [HttpGet("mi-proyecto")]
    public async Task<IActionResult> GetMiProyecto([FromQuery] int userId)
    {
        try
        {
            var result = await _svc.GetMiProyectoAsync(userId);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener el proyecto."); return StatusCode(500, new { message = "Error al obtener el proyecto." }); }
    }

    [HttpGet("proyectos")]
    public async Task<IActionResult> GetProyectos()
    {
        try
        {
            var result = await _svc.GetTodosProyectosAsync();
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener proyectos."); return StatusCode(500, new { message = "Error al obtener proyectos." }); }
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> GetResumen([FromQuery] int proyectoId, [FromQuery] int mes, [FromQuery] int anio)
    {
        try
        {
            var result = await _svc.GetResumenAsync(proyectoId, mes, anio);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener resumen."); return StatusCode(500, new { message = "Error al obtener resumen." }); }
    }

    // ── Staff ─────────────────────────────────────────────────────────────────

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff([FromQuery] int proyectoId)
    {
        try
        {
            var result = await _svc.GetStaffProyectoAsync(proyectoId);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener staff."); return StatusCode(500, new { message = "Error al obtener staff." }); }
    }

    // ── Tab 1: Asistencia ─────────────────────────────────────────────────────

    [HttpGet("charlas")]
    public async Task<IActionResult> GetCharlas([FromQuery] int proyectoId, [FromQuery] int mes, [FromQuery] int anio)
    {
        try
        {
            var result = await _svc.GetCharlasAsync(proyectoId, mes, anio);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener charlas."); return StatusCode(500, new { message = "Error al obtener charlas." }); }
    }

    [HttpPost("charlas")]
    public async Task<IActionResult> CrearCharla([FromQuery] int userId, [FromBody] CrearCharlaDto dto)
    {
        try
        {
            var result = await _svc.CrearCharlaAsync(dto, userId);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al crear charla."); return StatusCode(500, new { message = "Error al crear charla." }); }
    }

    [HttpDelete("charlas/{id}")]
    public async Task<IActionResult> EliminarCharla(int id)
    {
        try
        {
            await _svc.EliminarCharlaAsync(id);
            return NoContent();
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al eliminar charla."); return StatusCode(500, new { message = "Error al eliminar charla." }); }
    }

    [HttpGet("charlas/{charlaId}/asistencia")]
    public async Task<IActionResult> GetAsistencia(int charlaId)
    {
        try
        {
            var result = await _svc.GetAsistenciaAsync(charlaId);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener asistencia."); return StatusCode(500, new { message = "Error al obtener asistencia." }); }
    }

    [HttpPost("charlas/{charlaId}/asistencia")]
    public async Task<IActionResult> GuardarAsistencia(int charlaId, [FromQuery] int userId, [FromBody] GuardarAsistenciaDto dto)
    {
        try
        {
            await _svc.GuardarAsistenciaAsync(charlaId, dto, userId);
            return NoContent();
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al guardar asistencia."); return StatusCode(500, new { message = "Error al guardar asistencia." }); }
    }

    // ── Tab 2: Capacitaciones Staff ───────────────────────────────────────────

    [HttpGet("capacitaciones")]
    public async Task<IActionResult> GetCapacitaciones([FromQuery] int proyectoId, [FromQuery] int? mes = null, [FromQuery] int? anio = null)
    {
        try
        {
            var result = await _svc.GetCapacitacionesAsync(proyectoId, mes, anio);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener capacitaciones."); return StatusCode(500, new { message = "Error al obtener capacitaciones." }); }
    }

    [HttpPost("capacitaciones/mi-evidencia")]
    public async Task<IActionResult> SubirMiCapacitacion([FromQuery] int userId, [FromForm] DateTime fecha, [FromForm] string tema, IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _svc.SubirMiCapacitacionAsync(userId, fecha, tema, stream, file.FileName);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al subir capacitación."); return StatusCode(500, new { message = "Error al subir capacitación." }); }
    }

    [HttpPost("capacitaciones/mi-evidencia-multi")]
    public async Task<IActionResult> SubirMiCapacitacionMulti([FromQuery] int userId, [FromForm] DateTime fecha, [FromForm] string tema, IFormFileCollection files)
    {
        try
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "Debe adjuntar al menos un archivo." });

            var archivos = new List<(Stream Stream, string FileName)>();
            foreach (var f in files)
                archivos.Add((f.OpenReadStream(), f.FileName));

            var result = await _svc.SubirMiCapacitacionMultiAsync(userId, fecha, tema, archivos);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al subir capacitación."); return StatusCode(500, new { message = "Error al subir capacitación." }); }
    }

    [HttpPost("capacitaciones/{workerId}")]
    public async Task<IActionResult> SubirCapacitacion(int workerId, [FromQuery] int userId, [FromForm] DateTime fecha, [FromForm] string tema, IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _svc.SubirCapacitacionAsync(workerId, fecha, tema, stream, file.FileName, userId);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al subir capacitación."); return StatusCode(500, new { message = "Error al subir capacitación." }); }
    }

    [HttpPut("capacitaciones/{id}/estado")]
    [RequireFeature("ssoma.charlas.aprobar")]
    public async Task<IActionResult> CambiarEstado(int id, [FromQuery] int userId, [FromBody] CambiarEstadoDto dto)
    {
        try
        {
            var result = await _svc.CambiarEstadoAsync(id, dto.Estado, userId);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al cambiar estado."); return StatusCode(500, new { message = "Error al cambiar estado." }); }
    }

    [HttpDelete("capacitaciones/{id}")]
    public async Task<IActionResult> EliminarCapacitacion(int id)
    {
        try
        {
            await _svc.EliminarCapacitacionAsync(id);
            return NoContent();
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al eliminar capacitación."); return StatusCode(500, new { message = "Error al eliminar capacitación." }); }
    }

    // ── NEW: Tab 1 — Dashboard Asistencia Supervisores ────────────────────────

    [HttpGet("dashboard-supervisores")]
    public async Task<IActionResult> GetDashboardSupervisores([FromQuery] int proyectoId, [FromQuery] int mes, [FromQuery] int anio)
    {
        try
        {
            var result = await _svc.GetDashboardSupervisoresAsync(proyectoId, mes, anio);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener dashboard de supervisores."); return StatusCode(500, new { message = "Error al obtener dashboard de supervisores." }); }
    }

    // ── NEW: Tab 2 — Comparativo ──────────────────────────────────────────────

    [HttpGet("comparativo")]
    public async Task<IActionResult> GetComparativo([FromQuery] int proyectoId, [FromQuery] int anio)
    {
        try
        {
            var result = await _svc.GetComparativoAsync(proyectoId, anio);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener comparativo."); return StatusCode(500, new { message = "Error al obtener comparativo." }); }
    }

    // ── NEW: Tab 3 — Crear nueva charla ──────────────────────────────────────

    [HttpPost("nueva")]
    public async Task<IActionResult> CrearNueva([FromQuery] int userId, [FromBody] NuevaCharlaCreateDto dto)
    {
        try
        {
            var result = await _svc.CrearNuevaCharlaAsync(dto, userId);
            return StatusCode(201, result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al crear charla."); return StatusCode(500, new { message = "Error al crear charla." }); }
    }

    [HttpPut("charlas/{charlaId:int}")]
    public async Task<IActionResult> EditarCharla(int charlaId, [FromQuery] int userId, [FromBody] EditarCharlaDto dto)
    {
        try
        {
            await _svc.EditarCharlaAsync(charlaId, dto, userId);
            return NoContent();
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al editar charla."); return StatusCode(500, new { message = "Error al editar charla." }); }
    }

    // ── NEW: Tab 4 — Lista paginada ───────────────────────────────────────────

    [HttpGet("lista")]
    public async Task<IActionResult> GetLista(
        [FromQuery] int? proyectoId,
        [FromQuery] string? estado,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _svc.GetListaAsync(proyectoId, estado, page, pageSize);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener lista de charlas."); return StatusCode(500, new { message = "Error al obtener lista de charlas." }); }
    }

    [HttpGet("{id:int}/detalle")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try
        {
            var result = await _svc.GetDetalleAsync(id);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener detalle."); return StatusCode(500, new { message = "Error al obtener detalle." }); }
    }

    [HttpPut("{id:int}/aprobar")]
    [RequireFeature("ssoma.charlas.aprobar")]
    public async Task<IActionResult> Aprobar(int id, [FromQuery] int userId)
    {
        try
        {
            await _svc.AprobarAsync(id, userId);
            return Ok(new { message = "Charla aprobada." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al aprobar charla."); return StatusCode(500, new { message = "Error al aprobar charla." }); }
    }

    [HttpPut("{id:int}/rechazar")]
    [RequireFeature("ssoma.charlas.aprobar")]
    public async Task<IActionResult> Rechazar(int id, [FromQuery] int userId, [FromBody] RechazarCharlaDto dto)
    {
        try
        {
            await _svc.RechazarAsync(id, dto.Motivo, userId);
            return Ok(new { message = "Charla rechazada." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al rechazar charla."); return StatusCode(500, new { message = "Error al rechazar charla." }); }
    }

    [HttpGet("dashboard-personal")]
    public async Task<IActionResult> GetDashPersonal([FromQuery] int proyectoId, [FromQuery] int mes, [FromQuery] int anio)
    {
        try { return Ok(await _svc.GetDashPersonalAsync(proyectoId, mes, anio)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener dashboard personal."); return StatusCode(500, new { message = "Error al obtener dashboard personal." }); }
    }

    [HttpGet("dashboard-proyectos")]
    public async Task<IActionResult> GetDashProyectos([FromQuery] int mes, [FromQuery] int anio)
    {
        try { return Ok(await _svc.GetDashProyectosAsync(mes, anio)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener dashboard proyectos."); return StatusCode(500, new { message = "Error al obtener dashboard proyectos." }); }
    }

    [HttpPut("charlas/{charlaId}/asistencia")]
    public async Task<IActionResult> EditarAsistencia(int charlaId, [FromQuery] int userId, [FromBody] GuardarAsistenciaDto dto)
    {
        try
        {
            await _svc.GuardarAsistenciaAsync(charlaId, dto, userId);
            return NoContent();
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al editar asistencia."); return StatusCode(500, new { message = "Error al editar asistencia." }); }
    }

    [HttpGet("charlas-proyecto")]
    public async Task<IActionResult> GetCharlasProyecto([FromQuery] int proyectoId, [FromQuery] int mes, [FromQuery] int anio)
    {
        try
        {
            var result = await _svc.GetCharlasProyectoAsync(proyectoId, mes, anio);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener charlas del proyecto."); return StatusCode(500, new { message = "Error al obtener charlas del proyecto." }); }
    }

    // ── NEW: Mis capacitaciones ───────────────────────────────────────────────

    [HttpGet("capacitaciones/mis")]
    public async Task<IActionResult> GetMisCapacitaciones([FromQuery] int userId)
    {
        try
        {
            var result = await _svc.GetMisCapacitacionesAsync(userId);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener mis capacitaciones."); return StatusCode(500, new { message = "Error al obtener mis capacitaciones." }); }
    }

    // ── NEW: Supervisor search ────────────────────────────────────────────────

    [HttpGet("supervisores")]
    public async Task<IActionResult> GetSupervisores([FromQuery] string? search = null)
    {
        try
        {
            var result = await _svc.GetSupervisoresAsync(search);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error en CharlaController: {Message}", "Error al obtener supervisores."); return StatusCode(500, new { message = "Error al obtener supervisores." }); }
    }
}
