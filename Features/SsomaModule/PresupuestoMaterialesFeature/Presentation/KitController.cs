using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation;

/// <summary>
/// Kits/BOM de equipos SSOMA que se compran completos (Botiquín, Estación de Emergencia, etc.).
/// El usuario ingresa "cuántos kits" necesita el proyecto y el sistema multiplica la receta (BOM)
/// para obtener la cantidad inicial de cada material — sin inventar un driver por área/HH para
/// ítems que en realidad son de cantidad fija por kit.
/// </summary>
[ApiController]
[Route("api/v1/ssoma/presupuesto-materiales/kits")]
[Authorize]
[RequireFeature("ssoma.gestion.presupuesto-materiales")]
public class KitController : ControllerBase
{
    private readonly IKitService _service;
    public KitController(IKitService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? tipoId)
    {
        try { return Ok(await _service.ListarAsync(tipoId)); }
        catch (Exception) { return StatusCode(500, new { message = "Error al listar los kits." }); }
    }

    [HttpGet("{kitId}")]
    public async Task<IActionResult> Obtener(int kitId)
    {
        try
        {
            var kit = await _service.ObtenerAsync(kitId);
            if (kit == null) return NotFound(new { message = "Kit no encontrado." });
            return Ok(kit);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener el kit." }); }
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] KitCreateDto dto)
    {
        try
        {
            var id = await _service.CrearAsync(dto);
            return Ok(new { id });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error al crear el kit." }); }
    }

    /// <summary>Multiplica el BOM del kit por la cantidad de kits que necesita el proyecto.</summary>
    [HttpGet("{kitId}/calcular")]
    public async Task<IActionResult> Calcular(int kitId, [FromQuery] decimal cantidadKits)
    {
        try { return Ok(await _service.CalcularAsync(kitId, cantidadKits)); }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error al calcular el kit." }); }
    }
}
