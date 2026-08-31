using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionAdministrativa.Lugares.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/configuracion/lugares")]
    [Authorize]
    public class GaLugarController : ControllerBase
    {
        private readonly IGaLugarService _service;
        private readonly ILogger<GaLugarController> _logger;

        public GaLugarController(IGaLugarService service, ILogger<GaLugarController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try   { return Ok(await _service.GetAll()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaLugarController.GetAll");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBatch([FromBody] GaLugarCreateBatchDto dto)
        {
            try
            {
                await _service.CreateBatch(dto);
                return Ok(new { message = "Lugares guardados correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaLugarController.CreateBatch");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{id:int}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            try   { return Ok(new { activo = await _service.ToggleActivo(id) }); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaLugarController.Toggle");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("proyecto/{projectId:int}/toggle")]
        public async Task<IActionResult> ToggleProyecto(int projectId)
        {
            try   { return Ok(await _service.ToggleProyecto(projectId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaLugarController.ToggleProyecto");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Edit(int id, [FromBody] GaLugarEditDto dto)
        {
            try
            {
                await _service.Edit(id, dto);
                return Ok(new { message = "Lugar actualizado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaLugarController.Edit");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
