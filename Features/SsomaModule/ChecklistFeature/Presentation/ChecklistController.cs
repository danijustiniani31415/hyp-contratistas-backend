using System.Security.Claims;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/checklist")]
    [Authorize]
    [RequireFeature("ssoma.gestion.checklist")]
    public class ChecklistController : ControllerBase
    {
        private readonly IChecklistService _service;

        public ChecklistController(IChecklistService service)
        {
            _service = service;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Sesión inválida.");
            return int.Parse(claim.Value);
        }

        // ─────────────────────────────────────────────────────────────────
        // PLANTILLAS (catálogo maestro)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Lista todas las plantillas de checklist.</summary>
        [HttpGet("plantillas")]
        public async Task<IActionResult> GetPlantillas()
        {
            try
            {
                var result = await _service.GetPlantillasAsync();
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Detalle de una plantilla con sus items.</summary>
        [HttpGet("plantillas/{plantillaId:int}")]
        public async Task<IActionResult> GetPlantillaDetalle(int plantillaId)
        {
            try
            {
                var result = await _service.GetPlantillaDetalleAsync(plantillaId);
                if (result == null) return NotFound(new { message = "Plantilla no encontrada." });
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Crea una nueva plantilla de checklist.</summary>
        [HttpPost("plantillas")]
        public async Task<IActionResult> CreatePlantilla([FromBody] ChecklistPlantillaUpsertDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _service.CreatePlantillaAsync(dto, userId);
                return CreatedAtAction(nameof(GetPlantillaDetalle), new { plantillaId = result.Id }, result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Edita una plantilla existente (nombre, descripción, configuración).</summary>
        [HttpPut("plantillas/{plantillaId:int}")]
        public async Task<IActionResult> UpdatePlantilla(int plantillaId, [FromBody] ChecklistPlantillaUpsertDto dto)
        {
            try
            {
                await _service.UpdatePlantillaAsync(plantillaId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Agrega un nuevo item a una plantilla (se propaga automáticamente a proyectos activos).</summary>
        [HttpPost("plantillas/{plantillaId:int}/items")]
        public async Task<IActionResult> AddItemToPlantilla(int plantillaId, [FromBody] ChecklistPlantillaItemCreateDto dto)
        {
            try
            {
                var result = await _service.AddItemToPlantillaAsync(plantillaId, dto);
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Edita un item de plantilla (descripción, adjunto, activo/inactivo).</summary>
        [HttpPut("plantillas/items/{itemId:int}")]
        public async Task<IActionResult> UpdatePlantillaItem(int itemId, [FromBody] ChecklistPlantillaItemEditDto dto)
        {
            try
            {
                await _service.UpdatePlantillaItemAsync(itemId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        // ─────────────────────────────────────────────────────────────────
        // CHECKLISTS DE PROYECTO
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Resumen de todos los checklists de un proyecto (para indicadores/dashboard).</summary>
        [HttpGet("proyecto/{proyectoId:int}/resumen")]
        public async Task<IActionResult> GetResumenProyecto(int proyectoId)
        {
            try
            {
                var result = await _service.GetResumenProyectoAsync(proyectoId);
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Detalle completo de un checklist de proyecto con el estado de cada item.</summary>
        [HttpGet("{checklistProyectoId:int}")]
        public async Task<IActionResult> GetChecklistDetalle(int checklistProyectoId)
        {
            try
            {
                var result = await _service.GetChecklistDetalleAsync(checklistProyectoId);
                if (result == null) return NotFound(new { message = "Checklist no encontrado." });
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>Activa manualmente un checklist en un proyecto (Torre Grúa, SUNAFIL, etc.).</summary>
        [HttpPost("proyecto/{proyectoId:int}/activar")]
        public async Task<IActionResult> ActivarChecklist(int proyectoId, [FromBody] ChecklistActivarDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _service.ActivarChecklistAsync(proyectoId, dto.PlantillaId, userId);
                return Ok(result);
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }

        /// <summary>
        /// Marca o desmarca un item como completado.
        /// Responde con el nuevo porcentaje y estado del checklist padre.
        /// Si el checklist llega a 100%, dispara automáticamente el email al Gerente.
        /// </summary>
        [HttpPatch("items/{checklistProyectoItemId:int}")]
        public async Task<IActionResult> ToggleItem(int checklistProyectoItemId, [FromBody] ChecklistItemToggleDto dto)
        {
            try
            {
                var userId = GetUserId();
                var (porcentaje, estado) = await _service.ToggleItemAsync(checklistProyectoItemId, dto, userId);
                return Ok(new { porcentaje, estado });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor." }); }
        }
    }
}
