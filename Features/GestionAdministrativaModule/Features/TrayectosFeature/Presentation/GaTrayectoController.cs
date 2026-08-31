using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionAdministrativa.Trayectos.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/configuracion/trayectos")]
    [Authorize]
    public class GaTrayectoController : ControllerBase
    {
        private readonly IGaTrayectoService _service;
        private readonly ILogger<GaTrayectoController> _logger;

        public GaTrayectoController(IGaTrayectoService service, ILogger<GaTrayectoController> logger)
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
                _logger.LogError(ex, "Error en GaTrayectoController.GetAll");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("lugares-activos")]
        public async Task<IActionResult> GetLugaresActivos()
        {
            try   { return Ok(await _service.GetLugaresActivos()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaTrayectoController.GetLugaresActivos");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GaTrayectoCreateDto dto)
        {
            try
            {
                await _service.Create(dto);
                return Ok(new { message = "Trayecto creado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaTrayectoController.Create");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{id:int}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            try   { return Ok(new { activo = await _service.Toggle(id) }); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaTrayectoController.Toggle");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Edit(int id, [FromBody] GaTrayectoEditDto dto)
        {
            try
            {
                await _service.Edit(id, dto);
                return Ok(new { message = "Trayecto actualizado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaTrayectoController.Edit");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
