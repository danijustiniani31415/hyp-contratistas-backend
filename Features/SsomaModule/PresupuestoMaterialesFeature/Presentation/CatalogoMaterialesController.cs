using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Presentation
{
    /// <summary>
    /// Catálogo de Materiales SSOMA — 3 secciones: (1) catálogo normalizado editable,
    /// (2) materiales sin estandarizar de todos los proyectos con acción de estandarizar,
    /// (3) materiales que no pertenecen a SSOMA, para reportar a Oficina Técnica.
    /// </summary>
    [ApiController]
    [Route("api/v1/ssoma/presupuesto-materiales/catalogo")]
    [Authorize]
    [RequireFeature("ssoma.gestion.presupuesto-materiales")]
    public class CatalogoMaterialesController : ControllerBase
    {
        private readonly ICatalogoMaterialesService _service;
        private readonly IRevisionMaterialesService _revisionService;

        public CatalogoMaterialesController(ICatalogoMaterialesService service, IRevisionMaterialesService revisionService)
        {
            _service = service;
            _revisionService = revisionService;
        }

        private int UsuarioId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        /// <summary>Carga inicial/incremental del catálogo maestro (tipos, familias, items, alias) desde el CSV de materiales estandarizados.</summary>
        [HttpPost("seed")]
        public async Task<IActionResult> Seed([FromBody] SeedCatalogoRequestDto request)
        {
            try
            {
                var result = await _service.SeedCatalogoAsync(request);
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        // ─── Sección 1: catálogo normalizado ────────────────────────────────

        [HttpGet("tipos")]
        public async Task<IActionResult> ListarTipos()
        {
            try { return Ok(await _service.ListarTiposAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error al listar los tipos de material." }); }
        }

        [HttpGet("familias")]
        public async Task<IActionResult> ListarFamilias([FromQuery] string? q, [FromQuery] int? tipoId, [FromQuery] bool? perteneceSsoma)
        {
            try { return Ok(await _service.ListarFamiliasAsync(q, tipoId, perteneceSsoma)); }
            catch (Exception) { return StatusCode(500, new { message = "Error al listar el catálogo de familias." }); }
        }

        [HttpPut("familias/{id}")]
        public async Task<IActionResult> ActualizarFamilia(int id, [FromBody] ActualizarFamiliaDto dto)
        {
            try
            {
                await _service.ActualizarFamiliaAsync(id, dto);
                return Ok(new { message = "Familia actualizada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error al actualizar la familia." }); }
        }

        /// <summary>Alta manual de un ítem que de verdad no existe en el catálogo, desde el buscador de Revisión.</summary>
        [HttpPost("items")]
        public async Task<IActionResult> CrearItem([FromBody] CrearItemCatalogoDto dto)
        {
            try { return Ok(await _service.CrearItemAsync(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error al crear el ítem." }); }
        }

        /// <summary>Alta manual de una família nueva, cuando el material no encaja en ninguna categoría existente.</summary>
        [HttpPost("familias")]
        public async Task<IActionResult> CrearFamilia([FromBody] CrearFamiliaCatalogoDto dto)
        {
            try { return Ok(await _service.CrearFamiliaAsync(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error al crear la família." }); }
        }

        // ─── Sección 2: materiales sin estandarizar (global) ───────────────

        [HttpGet("sin-estandarizar")]
        public async Task<IActionResult> ObtenerSinEstandarizar()
        {
            try { return Ok(await _revisionService.ObtenerPendientesGlobalAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error al obtener los materiales sin estandarizar." }); }
        }

        [HttpPost("sin-estandarizar/procesar")]
        public async Task<IActionResult> ProcesarSinEstandarizar([FromBody] List<RevisionDecisionDto> decisiones)
        {
            try { return Ok(await _revisionService.ProcesarRevisionGlobalAsync(decisiones, UsuarioId)); }
            catch (Exception) { return StatusCode(500, new { message = "Error al procesar la estandarización." }); }
        }

        // ─── Sección 3: materiales que no pertenecen a SSOMA (reporte) ─────

        [HttpGet("no-ssoma")]
        public async Task<IActionResult> ObtenerNoSsoma()
        {
            try { return Ok(await _revisionService.ObtenerNoSsomaAsync()); }
            catch (Exception) { return StatusCode(500, new { message = "Error al obtener el reporte de materiales no-SSOMA." }); }
        }
    }
}
