using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma/presupuesto-materiales")]
[Authorize]
[RequireFeature("ssoma.gestion.presupuesto-materiales")]
public class HhCargaController : ControllerBase
{
    private readonly IHhCargaService _hhCargaService;

    public HhCargaController(IHhCargaService hhCargaService) => _hhCargaService = hhCargaService;

    private int UsuarioId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    /// <summary>
    /// Sube el Excel semanal de Horas Hombre (planilla/Tareo) de un proyecto — complementa al HH
    /// del Tareo de Control de Acceso para el driver del presupuesto. Acepta el acumulado completo
    /// en cada subida, igual que la carga de materiales.
    /// </summary>
    [HttpPost("proyectos/{projectId}/hh-cargas")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportarHh(int projectId, IFormFile archivo)
    {
        try
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Debes adjuntar un archivo Excel de Horas Hombre." });

            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { message = "Solo se aceptan archivos Excel (.xlsx, .xls)." });

            var resultado = await _hhCargaService.ImportarHhAsync(archivo, projectId, UsuarioId);
            return Ok(resultado);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error al procesar el archivo de Horas Hombre." }); }
    }

    /// <summary>Lista el historial de cargas de HH de un proyecto.</summary>
    [HttpGet("proyectos/{projectId}/hh-cargas")]
    public async Task<IActionResult> ListarCargas(int projectId)
    {
        try
        {
            var cargas = await _hhCargaService.ObtenerCargasAsync(projectId);
            return Ok(cargas);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener las cargas de Horas Hombre." }); }
    }
}
