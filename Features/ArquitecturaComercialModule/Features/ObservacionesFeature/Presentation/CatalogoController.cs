using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Presentation;

/// <summary>
/// Catálogos de Observaciones (Partida, Área Responsable). Ruta {tipo} acepta
/// "partidas" o "areas-responsables".
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/arquitectura-comercial/catalogos")]
[RequireFeature("arquitectura-comercial.observaciones")]
public class CatalogoController : ControllerBase
{
    private readonly ICatalogoService _service;

    public CatalogoController(ICatalogoService service) => _service = service;

    [HttpGet("{tipo}")]
    public async Task<IActionResult> GetByTipo(string tipo, [FromQuery] bool soloActivos = true)
    {
        try
        {
            return Ok(await _service.GetByTipo(tipo, soloActivos));
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPost("{tipo}")]
    public async Task<IActionResult> Create(string tipo, [FromBody] CreateCatalogoItemDTO body)
    {
        try
        {
            return Ok(await _service.Create(tipo, body));
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpPut("items/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCatalogoItemDTO body)
    {
        try
        {
            var result = await _service.Update(id, body);
            if (result == null) return NotFound(new { message = "No se encontró el ítem de catálogo." });
            return Ok(result);
        }
        catch (AbrilException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _service.Delete(id);
            if (!ok) return NotFound(new { message = "No se encontró el ítem de catálogo." });
            return Ok(new { message = "Eliminado correctamente." });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
        }
    }
}
