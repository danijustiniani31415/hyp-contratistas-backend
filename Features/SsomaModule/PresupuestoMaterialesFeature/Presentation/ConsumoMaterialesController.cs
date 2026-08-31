using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma/presupuesto-materiales")]
[Authorize]
[RequireFeature("ssoma.gestion.presupuesto-materiales")]
public class ConsumoMaterialesController : ControllerBase
{
    private readonly IConsumoService _consumoService;
    private readonly IEstandarizacionService _estandarizacionService;
    private readonly IRevisionMaterialesService _revisionService;

    public ConsumoMaterialesController(
        IConsumoService consumoService,
        IEstandarizacionService estandarizacionService,
        IRevisionMaterialesService revisionService)
    {
        _consumoService = consumoService;
        _estandarizacionService = estandarizacionService;
        _revisionService = revisionService;
    }

    private int UsuarioId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    // ─── Import S10 ───────────────────────────────────────────────────────────

    /// <summary>
    /// Sube el Kardex de materiales del proyecto (Movimiento/Nro. Guía/Partida de Control). Acepta
    /// el archivo acumulado completo en cada subida — compara línea por línea contra lo ya cargado
    /// (nuevas, regularizadas, dadas de baja) y dispara estandarización automática solo sobre las
    /// líneas nuevas.
    /// </summary>
    [HttpPost("proyectos/{projectId}/cargas")]
    [RequestSizeLimit(20_000_000)] // 20 MB
    public async Task<IActionResult> ImportarS10(int projectId, IFormFile archivo)
    {
        try
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Debes adjuntar un archivo Excel S10." });

            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { message = "Solo se aceptan archivos Excel (.xlsx, .xls)." });

            var resultado = await _consumoService.ImportarS10Async(archivo, projectId, UsuarioId);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error al procesar el archivo S10." }); }
    }

    /// <summary>Lista el historial de cargas S10 de un proyecto.</summary>
    [HttpGet("proyectos/{projectId}/cargas")]
    public async Task<IActionResult> ListarCargas(int projectId)
    {
        try
        {
            var cargas = await _consumoService.ObtenerCargasAsync(projectId);
            return Ok(cargas);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener las cargas." }); }
    }

    /// <summary>
    /// Asigna cada línea de consumo del proyecto al hito real del cronograma (/projects) que le
    /// corresponde según su fecha de guía. Se puede volver a correr cuando el cronograma cambie.
    /// </summary>
    [HttpPost("proyectos/{projectId}/asignar-hitos")]
    public async Task<IActionResult> AsignarHitos(int projectId)
    {
        try
        {
            var lineasActualizadas = await _consumoService.AsignarHitosAsync(projectId);
            return Ok(new { lineasActualizadas });
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al asignar hitos." }); }
    }

    // ─── Estandarización manual (re-procesar) ────────────────────────────────

    /// <summary>Re-ejecuta la estandarización de una carga (útil después de agregar aliases).</summary>
    [HttpPost("cargas/{cargaId}/estandarizar")]
    public async Task<IActionResult> Estandarizar(int cargaId)
    {
        try
        {
            var resultado = await _estandarizacionService.EstandarizarCargaAsync(cargaId);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error en el proceso de estandarización." }); }
    }

    /// <summary>Progreso en vivo de la estandarización de una carga, para pantallas que la disparan
    /// y quieren mostrar "línea X de Y" mientras corre (puede tardar minutos en lotes grandes).</summary>
    [HttpGet("cargas/{cargaId}/progreso")]
    public IActionResult ObtenerProgreso(int cargaId)
    {
        var progreso = _estandarizacionService.ObtenerProgreso(cargaId);
        if (progreso == null) return Ok(new { enProceso = false });
        return Ok(new { enProceso = true, procesadas = progreso.Value.Procesadas, total = progreso.Value.Total });
    }

    // ─── Revisión de materiales ───────────────────────────────────────────────

    /// <summary>Lista materiales pendientes de revisión SSOMA en un proyecto.</summary>
    [HttpGet("proyectos/{projectId}/revision/pendientes")]
    public async Task<IActionResult> ObtenerPendientes(int projectId)
    {
        try
        {
            var pendientes = await _revisionService.ObtenerPendientesAsync(projectId);
            return Ok(pendientes);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener materiales pendientes." }); }
    }

    /// <summary>Autoriza o rechaza materiales en lote. Los rechazados notifican a Oficina Técnica.</summary>
    [HttpPost("proyectos/{projectId}/revision/procesar")]
    public async Task<IActionResult> ProcesarRevision(int projectId, [FromBody] RevisionLoteDto dto)
    {
        try
        {
            dto.ProjectId = projectId;
            var resultado = await _revisionService.ProcesarRevisionAsync(dto, UsuarioId);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error al procesar la revisión." }); }
    }

    /// <summary>Búsqueda de items del catálogo para el selector de revisión.</summary>
    [HttpGet("catalogo/items/buscar")]
    public async Task<IActionResult> BuscarItems([FromQuery] string q)
    {
        try
        {
            var items = await _revisionService.BuscarItemsAsync(q);
            return Ok(items);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error en la búsqueda." }); }
    }
}
