using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma-accidentes-incidentes")]
[Authorize]
[RequireFeature("ssoma.gestion.accidentes-incidentes")]
public class AccidenteIncidenteController : ControllerBase
{
    private readonly IAccidenteIncidenteService _service;
    private readonly ILogger<AccidenteIncidenteController> _logger;
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AccidenteIncidenteController(IAccidenteIncidenteService service,
        ILogger<AccidenteIncidenteController> logger,
        IDbContextFactory<AppDbContext> factory)
    {
        _service = service;
        _logger = logger;
        _factory = factory;
    }

    [HttpGet("inicializar")]
    public async Task<IActionResult> Inicializar()
    {
        try { return Ok(await _service.GetInicializarAsync()); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error inicializar flash report"); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int? proyectoId,
        [FromQuery] int? tipoId,
        [FromQuery] string? estado,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] bool? soloEnviados,
        [FromQuery] string? areaOrigen,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try { return Ok(await _service.GetListAsync(proyectoId, tipoId, estado, fechaDesde, fechaHasta, soloEnviados, areaOrigen, page, pageSize)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error lista flash reports"); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetalle(int id)
    {
        try { return Ok(await _service.GetDetalleAsync(id)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error detalle flash report {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearFlashReportRequest request)
    {
        try
        {
            var usuarioId = ObtenerUsuarioId();
            var id = await _service.CrearAsync(request, usuarioId);
            return Ok(new { id, message = "Flash Report creado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error crear flash report"); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarFlashReportRequest request)
    {
        try
        {
            await _service.ActualizarAsync(id, request);
            return Ok(new { message = "Flash Report actualizado correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error actualizar flash report {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPatch("{id:int}/severidad")]
    public async Task<IActionResult> ActualizarSeveridad(int id, [FromBody] ActualizarSeveridadRequest request)
    {
        try
        {
            await _service.ActualizarSeveridadAsync(id, request.ConsecuenciaRealPersonal, request.ConsecuenciaPotencialPersonal);
            return Ok(new { message = "Severidad actualizada correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error actualizar severidad flash report {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPost("{id:int}/enviar")]
    public async Task<IActionResult> Enviar(int id, [FromQuery] bool enviarEmail = true)
    {
        try
        {
            await _service.EnviarFlashReportAsync(id, enviarEmail);
            var mensaje = enviarEmail
                ? "Flash Report enviado correctamente. Se generó el PDF y se notificó por correo."
                : "Flash Report enviado correctamente. Se generó el PDF (sin notificación por correo).";
            return Ok(new { message = mensaje });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error enviar flash report {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _service.EliminarAsync(id);
            return Ok(new { message = "Flash Report eliminado." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error eliminar flash report {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    // ── Entregables ───────────────────────────────────────────────────────────

    [HttpGet("{id:int}/entregables")]
    public async Task<IActionResult> GetEntregables(int id)
    {
        try { return Ok(await _service.GetEntregablesAsync(id)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error get entregables {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPatch("entregables/{entregableId:int}")]
    public async Task<IActionResult> ActualizarEntregable(int entregableId, [FromBody] ActualizarEntregableRequest req)
    {
        try { await _service.ActualizarEntregableAsync(entregableId, req); return Ok(new { message = "Entregable actualizado." }); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error actualizar entregable {Id}", entregableId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPost("entregables/{entregableId:int}/archivo")]
    public async Task<IActionResult> SubirArchivoEntregable(int entregableId, IFormFile archivo)
    {
        try
        {
            var url = await _service.SubirArchivoEntregableAsync(entregableId, archivo);
            return Ok(new { url, message = "Archivo subido." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error subir archivo entregable {Id}", entregableId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpDelete("entregables/archivo/{archivoId:int}")]
    public async Task<IActionResult> EliminarArchivoEntregable(int archivoId)
    {
        try { await _service.EliminarArchivoEntregableAsync(archivoId); return Ok(new { message = "Archivo eliminado." }); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error eliminar archivo entregable {Id}", archivoId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    // ── RM-050 ───────────────────────────────────────────────────────────────

    [HttpGet("{id:int}/rm050")]
    public async Task<IActionResult> GetRm050(int id)
    {
        try { return Ok(await _service.GetRm050Async(id)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error get rm050 {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPut("{id:int}/rm050")]
    public async Task<IActionResult> GuardarRm050(int id, [FromBody] GuardarRm050Request req)
    {
        try { await _service.GuardarRm050Async(id, req); return Ok(new { message = "Investigación RM-050 guardada." }); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error guardar rm050 {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> GenerarPdf(int id)
    {
        try
        {
            var bytes = await _service.GenerarPdfAsync(id);
            var fr = await _service.GetDetalleAsync(id);
            var fileName = $"FlashReport_{fr.Codigo}.pdf";
            // inline → el browser abre el visor de PDF en lugar de forzar descarga
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
            return File(bytes, "application/pdf");
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error generando PDF flash report {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpGet("{id:int}/foto/{slot:int}")]
    public async Task<IActionResult> ObtenerFoto(int id, int slot)
    {
        try
        {
            var (bytes, contentType, fileName) = await _service.ObtenerFotoAsync(id, slot);
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
            return File(bytes, contentType);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error obteniendo foto {Slot} flash report {Id}", slot, id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpGet("acciones-correctivas/vencidas")]
    public async Task<IActionResult> GetAccionesVencidas()
    {
        try { return Ok(await _service.GetAccionesVencidasAsync()); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error obteniendo acciones correctivas vencidas"); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPost("acciones-correctivas/{accionId:int}/leccion")]
    public async Task<IActionResult> CrearLeccionDesdeAccion(int accionId, [FromBody] CrearLeccionDesdeAccionRequest req)
    {
        try
        {
            using var ctx = _factory.CreateDbContext();
            var accion = await ctx.Set<SsomaAccionCorrectiva>().FindAsync(accionId)
                ?? throw new AbrilException("Acción correctiva no encontrada.", 404);

            var today = DateTime.UtcNow;
            var period = $"{today.Month:D2}-{today.Year}";

            var lesson = new Lesson
            {
                Period = period,
                PeriodDate = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, TimeSpan.Zero),
                ProblemDescription = accion.Descripcion,
                ReasonDescription = accion.Tipo ?? "Acción correctiva derivada de accidente/incidente",
                LessonDescription = req.ImpactDescription ?? accion.Descripcion,
                ImpactDescription = req.ImpactDescription ?? string.Empty,
                ProjectId = req.ProyectoId,
                AreaId = req.AreaId,
                StateId = 1,
                ApprovalStatus = "PENDIENTE",
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = ObtenerUsuarioId() ?? 0,
                Active = true,
                State = true,
            };

            ctx.Set<Lesson>().Add(lesson);
            await ctx.SaveChangesAsync();

            return Ok(new { message = "Lección aprendida creada correctamente.", lessonId = lesson.LessonId });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creando lección desde acción {AccionId}", accionId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPost("{id:int}/reclasificar-accidente")]
    public async Task<IActionResult> ReclasificarComoAccidente(int id)
    {
        try
        {
            var accidenteId = await _service.ReclasificarComoAccidenteAsync(id, ObtenerUsuarioId());
            var msg = accidenteId > 0
                ? $"Flash Report reclasificado como Accidente. AccidenteTrabajo ID: {accidenteId}."
                : "Flash Report reclasificado como Accidente.";
            return Ok(new { accidenteTrabajoId = accidenteId > 0 ? accidenteId : (int?)null, message = msg });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error reclasificando flash report {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpGet("{id:int}/mintra")]
    public async Task<IActionResult> GenerarMintra(int id)
    {
        try
        {
            var bytes = await _service.GenerarMintraAsync(id);
            var fr = await _service.GetDetalleAsync(id);
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"MINTRA_{fr.Codigo}.pdf\"");
            return File(bytes, "application/pdf");
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error generando MINTRA {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPost("{id:int}/reclasificar")]
    public async Task<IActionResult> Reclasificar(int id)
    {
        try
        {
            var nuevoId = await _service.ReclasificarComoAccidenteAsync(id, ObtenerUsuarioId());
            return Ok(new { message = "Incidente reclasificado como Accidente correctamente.", accidenteTrabajoId = nuevoId });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error reclasificando flash report {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    // ── Medidas de control (acciones correctivas bidireccionales) ────────────

    [HttpGet("{id:int}/medidas")]
    public async Task<IActionResult> GetMedidas(int id)
    {
        try { return Ok(await _service.GetMedidasAsync(id)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error get medidas {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPost("{id:int}/medidas")]
    public async Task<IActionResult> AddMedida(int id, [FromBody] GuardarAccionCorrectivaRequest req)
    {
        try
        {
            var accionId = await _service.AddMedidaAsync(id, req);
            return Ok(new { id = accionId, message = "Medida de control añadida." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error add medida {Id}", id); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpPatch("medidas/{accionId:int}")]
    public async Task<IActionResult> UpdateMedida(int accionId, [FromBody] GuardarAccionCorrectivaRequest req)
    {
        try
        {
            await _service.UpdateMedidaAsync(accionId, req);
            return Ok(new { message = "Medida de control actualizada." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error update medida {AccionId}", accionId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    [HttpDelete("medidas/{accionId:int}")]
    public async Task<IActionResult> DeleteMedida(int accionId)
    {
        try
        {
            await _service.DeleteMedidaAsync(accionId);
            return Ok(new { message = "Medida de control eliminada." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error delete medida {AccionId}", accionId); return StatusCode(500, new { message = "Error del servidor." }); }
    }

    private int? ObtenerUsuarioId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
