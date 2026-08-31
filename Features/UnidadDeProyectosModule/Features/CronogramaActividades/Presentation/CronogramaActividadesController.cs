using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Presentation
{
    [ApiController]
    [Route("api/v1/cronograma-actividades")]
    [Authorize]
    public class CronogramaActividadesController : ControllerBase
    {
        private readonly ICronogramaActividadesService _service;

        public CronogramaActividadesController(ICronogramaActividadesService service)
        {
            _service = service;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // GET /api/v1/cronograma-actividades/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] int? responsableId, [FromQuery] string? estado)
        {
            try
            {
                var result = await _service.GetDashboardAsync(responsableId, estado);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // GET /api/v1/cronograma-actividades/proyectos
        [HttpGet("proyectos")]
        public async Task<IActionResult> GetProyectos()
        {
            try
            {
                var result = await _service.GetProyectosAsync();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // GET /api/v1/cronograma-actividades/{proyectoId}/actividades
        [HttpGet("{proyectoId:int}/actividades")]
        public async Task<IActionResult> GetActividades(int proyectoId, [FromQuery] string tipoCronograma = "ANTEPROYECTO")
        {
            try
            {
                var result = await _service.GetActividadesAsync(proyectoId, tipoCronograma);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // POST /api/v1/cronograma-actividades/{proyectoId}/actividades
        [HttpPost("{proyectoId:int}/actividades")]
        public async Task<IActionResult> CrearActividad(int proyectoId, [FromBody] CrearActividadRequest request)
        {
            try
            {
                var result = await _service.CrearActividadAsync(proyectoId, request, GetUserId());
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // PUT /api/v1/cronograma-actividades/actividades/{projectActivityId}
        [HttpPut("actividades/{projectActivityId:int}")]
        public async Task<IActionResult> EditarActividad(int projectActivityId, [FromBody] EditarActividadRequest request)
        {
            try
            {
                var result = await _service.EditarActividadAsync(projectActivityId, request, GetUserId());
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // PATCH /api/v1/cronograma-actividades/actividades/{projectActivityId}/culminar
        [HttpPatch("actividades/{projectActivityId:int}/culminar")]
        public async Task<IActionResult> CulminarActividad(int projectActivityId)
        {
            try
            {
                var result = await _service.CulminarActividadAsync(projectActivityId, GetUserId());
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // GET /api/v1/cronograma-actividades/debug-proyectos  — TEMPORAL, quitar tras diagnóstico
        [HttpGet("debug-proyectos")]
        public async Task<IActionResult> DebugProyectos()
        {
            try
            {
                var result = await _service.GetDebugProyectosAsync();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // DELETE /api/v1/cronograma-actividades/actividades/{projectActivityId}
        [HttpDelete("actividades/{projectActivityId:int}")]
        public async Task<IActionResult> EliminarActividad(int projectActivityId)
        {
            try
            {
                await _service.EliminarActividadAsync(projectActivityId, GetUserId());
                return NoContent();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // PATCH /api/v1/cronograma-actividades/{proyectoId}/actividades/reordenar
        [HttpPatch("{proyectoId:int}/actividades/reordenar")]
        public async Task<IActionResult> ReordenarActividades(int proyectoId, [FromBody] List<ReordenarItem> items)
        {
            try
            {
                var result = await _service.ReordenarActividadesAsync(proyectoId, items);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // PATCH /api/v1/cronograma-actividades/{proyectoId}/actividades/{actividadId}/subir-nivel
        [HttpPatch("{proyectoId:int}/actividades/{actividadId:int}/subir-nivel")]
        public async Task<IActionResult> SubirNivel(int proyectoId, int actividadId)
        {
            try
            {
                var result = await _service.SubirNivelAsync(proyectoId, actividadId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // PATCH /api/v1/cronograma-actividades/{proyectoId}/actividades/{actividadId}/bajar-nivel
        [HttpPatch("{proyectoId:int}/actividades/{actividadId:int}/bajar-nivel")]
        public async Task<IActionResult> BajarNivel(int proyectoId, int actividadId)
        {
            try
            {
                var result = await _service.BajarNivelAsync(proyectoId, actividadId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // PUT /api/v1/cronograma-actividades/{proyectoId}/cambiar-jerarquia
        [HttpPut("{proyectoId:int}/cambiar-jerarquia")]
        public async Task<IActionResult> CambiarJerarquia(int proyectoId, [FromBody] CambiarJerarquiaRequest request)
        {
            try
            {
                var result = await _service.CambiarJerarquiaAsync(proyectoId, request);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // PATCH /api/v1/cronograma-actividades/actividades/{id}/linea-base
        [HttpPatch("actividades/{id:int}/linea-base")]
        public async Task<IActionResult> ActualizarLineaBase(int id, [FromBody] ActualizarLineaBaseRequest request)
        {
            try
            {
                var result = await _service.ActualizarLineaBaseAsync(id, request, GetUserId());
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ─────────────────────────── Feriados ───────────────────────────

        // GET /api/v1/cronograma-actividades/feriados
        [HttpGet("feriados")]
        public async Task<IActionResult> GetFeriados()
        {
            try
            {
                var result = await _service.GetFeriadosAsync();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // POST /api/v1/cronograma-actividades/feriados
        [HttpPost("feriados")]
        public async Task<IActionResult> CrearFeriado([FromBody] CrearFeriadoRequest request)
        {
            try
            {
                var result = await _service.CrearFeriadoAsync(request);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // DELETE /api/v1/cronograma-actividades/feriados/{id}
        [HttpDelete("feriados/{id:int}")]
        public async Task<IActionResult> EliminarFeriado(int id)
        {
            try
            {
                await _service.EliminarFeriadoAsync(id);
                return NoContent();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ─────────────────────────── Predecesoras + cascada ───────────────────────────

        // PUT /api/v1/cronograma-actividades/actividades/{projectActivityId}/predecesoras
        [HttpPut("actividades/{projectActivityId:int}/predecesoras")]
        public async Task<IActionResult> ActualizarPredecesoras(int projectActivityId, [FromBody] ActualizarPredecesorasRequest request)
        {
            try
            {
                var result = await _service.ActualizarPredecesorasAsync(projectActivityId, request.PredecessorIds);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // POST /api/v1/cronograma-actividades/{proyectoId}/recalcular-cascada/preview
        [HttpPost("{proyectoId:int}/recalcular-cascada/preview")]
        public async Task<IActionResult> PreviewCascada(int proyectoId)
        {
            try
            {
                var result = await _service.PreviewCascadaAsync(proyectoId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // POST /api/v1/cronograma-actividades/{proyectoId}/recalcular-cascada/aplicar
        [HttpPost("{proyectoId:int}/recalcular-cascada/aplicar")]
        public async Task<IActionResult> AplicarCascada(int proyectoId)
        {
            try
            {
                var result = await _service.AplicarCascadaAsync(proyectoId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // POST /api/v1/cronograma-actividades/{proyectoId}/importar-mpp
        [HttpPost("{proyectoId:int}/importar-mpp")]
        [RequestSizeLimit(52_428_800)] // 50 MB
        public async Task<IActionResult> ImportarMpp(int proyectoId, IFormFile archivo, [FromQuery] string tipoCronograma = "ANTEPROYECTO")
        {
            try
            {
                var result = await _service.ImportarMppAsync(proyectoId, archivo, GetUserId(), tipoCronograma);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // POST /api/v1/cronograma-actividades/{proyectoId}/actividades-masivo
        [HttpPost("{proyectoId:int}/actividades-masivo")]
        public async Task<IActionResult> CrearActividadesMasivo(int proyectoId, [FromBody] CrearActividadesMasivoRequest request)
        {
            try
            {
                var result = await _service.CrearActividadesMasivoAsync(proyectoId, request, GetUserId());
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ─────────────────────────── Última pestaña ───────────────────────────

        // GET /api/v1/cronograma-actividades/{proyectoId}/ultima-pestana
        [HttpGet("{proyectoId:int}/ultima-pestana")]
        public async Task<IActionResult> GetUltimaPestana(int proyectoId)
        {
            try
            {
                var result = await _service.GetUltimaPestanaAsync(proyectoId, GetUserId());
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // PATCH /api/v1/cronograma-actividades/{proyectoId}/ultima-pestana
        [HttpPatch("{proyectoId:int}/ultima-pestana")]
        public async Task<IActionResult> ActualizarUltimaPestana(int proyectoId, [FromBody] ActualizarUltimaPestanaRequest request)
        {
            try
            {
                await _service.ActualizarUltimaPestanaAsync(proyectoId, GetUserId(), request);
                return NoContent();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ─────────────────────────── Plantilla ───────────────────────────

        // POST /api/v1/cronograma-actividades/{proyectoId}/aplicar-plantilla
        [HttpPost("{proyectoId:int}/aplicar-plantilla")]
        public async Task<IActionResult> AplicarPlantilla(int proyectoId, [FromBody] AplicarPlantillaRequest request)
        {
            try
            {
                var result = await _service.AplicarPlantillaAsync(proyectoId, request, GetUserId());
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
