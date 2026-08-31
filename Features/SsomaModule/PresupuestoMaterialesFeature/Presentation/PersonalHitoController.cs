using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation;

/// <summary>
/// Dotación de personal SSOMA (Prevencionista/Monitor/Vigía) planificada por hito crítico del
/// cronograma real del proyecto — el usuario decide manualmente cuánta gente y por cuántas
/// semanas corresponde a cada fase; el sistema solo calcula el total.
/// </summary>
[ApiController]
[Route("api/v1/ssoma/presupuesto-materiales/proyectos/{projectId}/personal-hitos")]
[Authorize]
[RequireFeature("ssoma.gestion.presupuesto-materiales")]
public class PersonalHitoController : ControllerBase
{
    private readonly IPersonalHitoService _service;
    public PersonalHitoController(IPersonalHitoService service) => _service = service;

    private int UsuarioId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    /// <summary>Lista los hitos críticos del cronograma vigente que aún no tienen personal asignado.</summary>
    [HttpGet("disponibles")]
    public async Task<IActionResult> ObtenerHitosCriticos(int projectId)
    {
        try
        {
            var hitos = await _service.ObtenerHitosCriticosAsync(projectId);
            return Ok(hitos);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener los hitos críticos del proyecto." }); }
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(int projectId)
    {
        try
        {
            var filas = await _service.ObtenerPorProyectoAsync(projectId);
            return Ok(filas);
        }
        catch (Exception) { return StatusCode(500, new { message = "Error al obtener la dotación de personal por hito." }); }
    }

    /// <summary>Reemplaza toda la dotación de personal del proyecto por la lista enviada.</summary>
    [HttpPut]
    public async Task<IActionResult> Guardar(int projectId, [FromBody] PersonalHitoGuardarDto dto)
    {
        try
        {
            await _service.GuardarAsync(projectId, dto, UsuarioId);
            return Ok(new { message = "Dotación de personal guardada correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (Exception) { return StatusCode(500, new { message = "Error al guardar la dotación de personal." }); }
    }
}
