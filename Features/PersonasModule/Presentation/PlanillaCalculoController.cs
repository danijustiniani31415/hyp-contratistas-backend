using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.PersonasModule.Presentation
{
    /// <summary>Motor de cálculo de Planillas — Fase 3 (conceptos configurables + períodos).</summary>
    [ApiController]
    [Route("api/v1/planillas")]
    [Authorize]
    public class PlanillaCalculoController : ControllerBase
    {
        private readonly IPlanillaCalculoService _service;

        public PlanillaCalculoController(IPlanillaCalculoService service)
        {
            _service = service;
        }

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        [HttpGet("conceptos")]
        public async Task<IActionResult> ListConceptos()
        {
            try { return Ok(await _service.ListConceptos()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("conceptos")]
        public async Task<IActionResult> CrearConcepto([FromBody] ConceptoPlanillaCreateDto dto)
        {
            if (!User.HasLbPermiso("PLANILLA_CONFIGURAR"))
                return StatusCode(403, new { message = "No tienes permiso para configurar conceptos de planilla." });
            try { return Ok(await _service.CrearConcepto(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("conceptos/{id:int}")]
        public async Task<IActionResult> ActualizarConcepto(int id, [FromBody] ConceptoPlanillaUpdateDto dto)
        {
            if (!User.HasLbPermiso("PLANILLA_CONFIGURAR"))
                return StatusCode(403, new { message = "No tienes permiso para configurar conceptos de planilla." });
            try { return Ok(await _service.ActualizarConcepto(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("periodos")]
        public async Task<IActionResult> ListPeriodos()
        {
            try { return Ok(await _service.ListPeriodos()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("periodos")]
        public async Task<IActionResult> CrearPeriodo([FromBody] PlanillaPeriodoCreateDto dto)
        {
            if (!User.HasLbPermiso("PLANILLA_CALCULAR"))
                return StatusCode(403, new { message = "No tienes permiso para crear períodos de planilla." });
            try { return Ok(await _service.CrearPeriodo(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("periodos/{id:int}")]
        public async Task<IActionResult> GetPeriodo(int id)
        {
            try { return Ok(await _service.GetPeriodo(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("periodos/{id:int}/calcular")]
        public async Task<IActionResult> Calcular(int id)
        {
            if (!User.HasLbPermiso("PLANILLA_CALCULAR"))
                return StatusCode(403, new { message = "No tienes permiso para calcular planillas." });
            try { return Ok(await _service.Calcular(id, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("periodos/{id:int}/cerrar")]
        public async Task<IActionResult> Cerrar(int id)
        {
            if (!User.HasLbPermiso("PLANILLA_CALCULAR"))
                return StatusCode(403, new { message = "No tienes permiso para cerrar períodos de planilla." });
            try { return Ok(await _service.Cerrar(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
