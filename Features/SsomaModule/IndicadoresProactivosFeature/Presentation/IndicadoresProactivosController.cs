using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Presentation;

[ApiController]
[Route("api/v1/ssoma-indicadores-proactivos")]
[Authorize]
[RequireFeature("ssoma.gestion.indicadores-proactivos")]
public class IndicadoresProactivosController : ControllerBase
{
    private readonly IIndicadoresProactivosService _service;
    private readonly IMemoryCache _cache;
    private readonly ReactivosCacheVersion _reactivosCacheVersion;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public IndicadoresProactivosController(
        IIndicadoresProactivosService service, IMemoryCache cache, ReactivosCacheVersion reactivosCacheVersion)
    {
        _service = service;
        _cache = cache;
        _reactivosCacheVersion = reactivosCacheVersion;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    /// <summary>
    /// Quién puede VER y USAR el botón de ocultar/mostrar empresas (aplica a todas
    /// las tarjetas por igual) — Coordinadores SSOMA, administradores del sistema, o Samuel.
    /// </summary>
    private async Task<bool> PuedeOcultarAsync()
    {
        if (User.IsInRole(Roles.AdministradorSistema)) return true;
        return await _service.EsCoordinadorSsomaAsync(GetUserId());
    }

    [HttpPatch("empresas/{empresaId:int}/ocultar")]
    public async Task<IActionResult> OcultarEmpresa(int empresaId, [FromBody] OcultarEmpresaRequest? req)
    {
        try
        {
            if (!await PuedeOcultarAsync()) return Forbid();
            await _service.OcultarEmpresaAsync(empresaId, req?.Motivo, GetUserId());
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPatch("empresas/{empresaId:int}/mostrar")]
    public async Task<IActionResult> MostrarEmpresa(int empresaId)
    {
        try
        {
            if (!await PuedeOcultarAsync()) return Forbid();
            await _service.MostrarEmpresaAsync(empresaId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ── CATÁLOGOS ─────────────────────────────────────────────────────────────

    /// <summary>Lista los 32 tipos de inspección del catálogo.</summary>
    [HttpGet("tipos-inspeccion")]
    public async Task<IActionResult> GetTiposInspeccion()
    {
        try
        {
            var result = await _service.GetTiposInspeccionAsync();
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al obtener los tipos de inspección." }); }
    }

    // ── PROGRAMACIÓN DE INSPECCIONES ─────────────────────────────────────────

    /// <summary>
    /// Devuelve la configuración de tipos de inspección por empresa
    /// para un proyecto y mes dado. Incluye todas las empresas del tareo.
    /// </summary>
    [HttpGet("programacion")]
    public async Task<IActionResult> GetProgramacion(
        [FromQuery] int proyectoId,
        [FromQuery] int mes,
        [FromQuery] int anio)
    {
        try
        {
            var result = await _service.GetProgInspeccionAsync(proyectoId, mes, anio);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al obtener la programación de inspecciones." }); }
    }

    /// <summary>Guarda (reemplaza) los tipos de inspección aplicables para una empresa en el mes.</summary>
    [HttpPost("programacion")]
    public async Task<IActionResult> GuardarProgramacion([FromBody] GuardarProgInspeccionRequest request)
    {
        try
        {
            await _service.GuardarProgInspeccionAsync(request, GetUserId());
            return Ok(new { message = "Programación guardada correctamente." });
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al guardar la programación." }); }
    }

    // ── INDICADORES POR PROYECTO ──────────────────────────────────────────────

    /// <summary>
    /// Indicadores proactivos de un proyecto para un mes/año.
    /// Incluye metas y actuals por empresa.
    /// </summary>
    [HttpGet("proyecto/{proyectoId:int}")]
    public async Task<IActionResult> GetIndicadoresProyecto(
        int proyectoId,
        [FromQuery] int mes,
        [FromQuery] int anio)
    {
        try
        {
            var key = $"ind_proy_{proyectoId}_{mes}_{anio}";
            var result = await _cache.GetOrCreateAsync(key, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = CacheTtl;
                return await _service.GetIndicadoresProyectoAsync(proyectoId, mes, anio);
            });
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al calcular los indicadores." }); }
    }

    // ── SEGUIMIENTO TODOS LOS PROYECTOS ──────────────────────────────────────

    /// <summary>
    /// Dashboard competitivo: indicadores proactivos de todos los proyectos activos.
    /// Ordenado por % cumplimiento general descendente.
    /// </summary>
    [HttpGet("seguimiento")]
    public async Task<IActionResult> GetSeguimiento(
        [FromQuery] int mes,
        [FromQuery] int anio,
        [FromQuery] bool incluirOcultos = false)
    {
        try
        {
            var cacheado = await GetOrCreateSeguimientoAsync(mes, anio);
            var excluidoIds = await _service.GetEmpresaExcluidaIdsAsync();
            var puedeOcultar = await PuedeOcultarAsync();

            var result = cacheado.Select(p => p with
            {
                Empresas = p.Empresas
                    .Where(e => incluirOcultos || !e.EmpresaId.HasValue || !excluidoIds.Contains(e.EmpresaId.Value))
                    .Select(e => e with
                    {
                        EsOculto = e.EmpresaId.HasValue && excluidoIds.Contains(e.EmpresaId.Value),
                        PuedeOcultarse = puedeOcultar,
                    })
                    .ToList(),
            }).ToList();

            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al obtener el seguimiento." }); }
    }

    /// <summary>
    /// Comparte una sola entrada de caché entre "seguimiento" y "puntaje" para evitar
    /// recalcular las 8 bulk queries del seguimiento dos veces en la misma carga de dashboard.
    /// </summary>
    private Task<List<Application.Dtos.IndicadorProactivoProyectoDto>> GetOrCreateSeguimientoAsync(int mes, int anio)
    {
        var key = $"ind_seguimiento_{mes}_{anio}";
        return _cache.GetOrCreateAsync(key, async e =>
        {
            e.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _service.GetSeguimientoTodosProyectosAsync(mes, anio);
        })!;
    }

    // ── PUNTAJE DEL MES ───────────────────────────────────────────────────────

    /// <summary>Puntaje del mes (max 110 pts) para un proyecto.</summary>
    [HttpGet("puntaje/{proyectoId:int}")]
    public async Task<IActionResult> GetPuntaje(
        int proyectoId,
        [FromQuery] int mes,
        [FromQuery] int anio)
    {
        try
        {
            var key = $"ind_puntaje_{proyectoId}_{mes}_{anio}";
            var result = await _cache.GetOrCreateAsync(key, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = CacheTtl;
                return await _service.GetPuntajeMesAsync(proyectoId, mes, anio);
            });
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al calcular el puntaje." }); }
    }

    /// <summary>Puntaje del mes para todos los proyectos activos (ranking).</summary>
    [HttpGet("puntaje")]
    public async Task<IActionResult> GetPuntajeTodos(
        [FromQuery] int mes,
        [FromQuery] int anio)
    {
        try
        {
            var seguimiento = await GetOrCreateSeguimientoAsync(mes, anio);

            var key = $"ind_puntaje_todos_{mes}_{anio}";
            var result = await _cache.GetOrCreateAsync(key, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = CacheTtl;
                return await _service.GetPuntajeTodosProyectosAsync(mes, anio, seguimiento);
            });
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al calcular los puntajes." }); }
    }

    // ── INDICADORES REACTIVOS IF / IG / IA ────────────────────────────────────

    /// <summary>
    /// Indicadores reactivos (IF, IG, IA) para un proyecto y período.
    /// IF = accidentes × 10⁶ / HHT  |  IG = días perdidos × 10⁶ / HHT  |  IA = IF × IG / 1000
    /// </summary>
    [HttpGet("reactivos/{proyectoId:int}")]
    public async Task<IActionResult> GetReactivosProyecto(
        int proyectoId,
        [FromQuery] int mes,
        [FromQuery] int anio)
    {
        try
        {
            var key = $"ind_reactivos_{proyectoId}_{mes}_{anio}_v{_reactivosCacheVersion.Current}";
            var result = await _cache.GetOrCreateAsync(key, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = CacheTtl;
                return await _service.GetIndicadoresReactivosAsync(proyectoId, mes, anio);
            });
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al calcular indicadores reactivos." }); }
    }

    /// <summary>Indicadores reactivos de todos los proyectos activos para el período.</summary>
    [HttpGet("reactivos")]
    public async Task<IActionResult> GetReactivosTodos(
        [FromQuery] int mes,
        [FromQuery] int anio)
    {
        try
        {
            var key = $"ind_reactivos_todos_{mes}_{anio}_v{_reactivosCacheVersion.Current}";
            var result = await _cache.GetOrCreateAsync(key, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = CacheTtl;
                return await _service.GetIndicadoresReactivosTodosAsync(mes, anio);
            });
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al calcular indicadores reactivos." }); }
    }

    // ── Meta anual de reactivos ───────────────────────────────────────────────

    /// <summary>Meta anual configurada (IF/IG/IA) para el año dado, si existe.</summary>
    [HttpGet("meta-anual/{anio}")]
    public async Task<IActionResult> GetMetaAnual(int anio)
    {
        try
        {
            var result = await _service.GetMetaAnualAsync(anio);
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al obtener la meta anual." }); }
    }

    /// <summary>Crea o actualiza la meta anual de IF/IG/IA para un año.</summary>
    [HttpPut("meta-anual")]
    public async Task<IActionResult> GuardarMetaAnual([FromBody] GuardarMetaAnualRequest request)
    {
        try
        {
            var result = await _service.GuardarMetaAnualAsync(request, GetUserId());
            return Ok(result);
        }
        catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Error al guardar la meta anual." }); }
    }
}

public class OcultarEmpresaRequest
{
    public string? Motivo { get; set; }
}
