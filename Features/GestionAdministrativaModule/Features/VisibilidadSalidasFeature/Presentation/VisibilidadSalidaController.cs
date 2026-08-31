using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/configuracion/visibilidad-salidas")]
    [Authorize]
    public class VisibilidadSalidaController : ControllerBase
    {
        private readonly IVisibilidadSalidaService _service;
        private readonly ILogger<VisibilidadSalidaController> _logger;

        public VisibilidadSalidaController(IVisibilidadSalidaService service, ILogger<VisibilidadSalidaController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetInitialData()
        {
            try   { return Ok(await _service.GetInitialDataAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en VisibilidadSalidaController.GetInitialData");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("area-scope-tree")]
        public async Task<IActionResult> GetAreaTree()
        {
            try   { return Ok(await _service.GetAreaTreeAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en VisibilidadSalidaController.GetAreaTree");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("worker/{workerId:int}")]
        public async Task<IActionResult> GetWorkerAsignaciones(int workerId)
        {
            try   { return Ok(await _service.GetWorkerAsignacionesAsync(workerId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en VisibilidadSalidaController.GetWorkerAsignaciones");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("worker/{workerId:int}")]
        public async Task<IActionResult> UpdateWorkerAsignaciones(int workerId, [FromBody] VisibilidadUpdateDto dto)
        {
            try
            {
                await _service.UpdateWorkerAsignacionesAsync(workerId, dto?.Areas ?? new List<VisibilidadAsignacionDto>());
                return Ok(new { message = "Visibilidad actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en VisibilidadSalidaController.UpdateWorkerAsignaciones");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
