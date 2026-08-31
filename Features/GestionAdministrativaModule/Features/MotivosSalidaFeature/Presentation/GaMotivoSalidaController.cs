using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/configuracion/motivos")]
    [Authorize]
    public class GaMotivoSalidaController : ControllerBase
    {
        private readonly IGaMotivoSalidaService _service;
        private readonly ILogger<GaMotivoSalidaController> _logger;

        public GaMotivoSalidaController(IGaMotivoSalidaService service, ILogger<GaMotivoSalidaController> logger)
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
                _logger.LogError(ex, "Error en GaMotivoSalidaController.GetAll");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GaMotivoSalidaCreateDto dto)
        {
            try
            {
                await _service.Create(dto);
                return Ok(new { message = "Motivo guardado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaMotivoSalidaController.Create");
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
                _logger.LogError(ex, "Error en GaMotivoSalidaController.Toggle");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Edit(int id, [FromBody] GaMotivoSalidaEditDto dto)
        {
            try
            {
                await _service.Edit(id, dto);
                return Ok(new { message = "Motivo actualizado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GaMotivoSalidaController.Edit");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
