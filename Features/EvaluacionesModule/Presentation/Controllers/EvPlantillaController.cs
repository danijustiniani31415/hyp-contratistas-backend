using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/evaluaciones/plantilla")]
    [Authorize]
    public class EvPlantillaController : ControllerBase
    {
        private readonly IEvPlantillaRepository _repo;
        private readonly ILogger<EvPlantillaController> _logger;

        public EvPlantillaController(IEvPlantillaRepository repo, ILogger<EvPlantillaController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try { return Ok(await _repo.GetAllActivasAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPlantillaController.GetAll"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("areas")]
        public async Task<IActionResult> GetAreas()
        {
            try { return Ok(await _repo.GetAreasAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPlantillaController.GetAreas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{area}")]
        public async Task<IActionResult> GetByArea(string area)
        {
            try { return Ok(await _repo.GetByAreaAsync(area)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPlantillaController.GetByArea"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EvPlantillaCreateDto dto)
        {
            try
            {
                var plantilla = new EvPlantilla
                {
                    AreaNombre = dto.AreaNombre,
                    Criterio = dto.Criterio,
                    Orden = dto.Orden
                };
                return StatusCode(201, await _repo.CreateAsync(plantilla));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPlantillaController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EvPlantillaUpdateDto dto)
        {
            try
            {
                var plantilla = await _repo.GetByIdAsync(id)
                    ?? throw new AbrilException("Criterio no encontrado.", 404);
                plantilla.Criterio = dto.Criterio;
                plantilla.Activo = dto.Activo;
                plantilla.Version++;
                await _repo.UpdateAsync(plantilla);
                return Ok(plantilla);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPlantillaController.Update"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
