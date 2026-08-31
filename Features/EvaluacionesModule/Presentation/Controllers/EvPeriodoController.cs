using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/evaluaciones/periodos")]
    [Authorize]
    public class EvPeriodoController : ControllerBase
    {
        private readonly IEvPeriodoRepository _repo;
        private readonly ILogger<EvPeriodoController> _logger;

        public EvPeriodoController(IEvPeriodoRepository repo, ILogger<EvPeriodoController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet("activo")]
        public async Task<IActionResult> GetActivo()
        {
            try
            {
                var p = await _repo.GetActivoAsync();
                if (p == null) return NotFound(new { message = "No hay período activo." });
                return Ok(MapToDto(p));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPeriodoController.GetActivo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Último período registrado, esté o no abierto para evaluar. Para pantallas de solo lectura (dashboards/resúmenes).</summary>
        [HttpGet("ultimo")]
        public async Task<IActionResult> GetUltimo()
        {
            try
            {
                var p = await _repo.GetUltimoAsync();
                if (p == null) return NotFound(new { message = "No hay períodos registrados." });
                return Ok(MapToDto(p));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPeriodoController.GetUltimo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var list = await _repo.GetAllAsync();
                return Ok(list.Select(MapToDto));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPeriodoController.GetAll"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EvPeriodoCreateDto dto)
        {
            try
            {
                var periodo = new EvPeriodo
                {
                    Mes = dto.Mes,
                    Anio = dto.Anio,
                    FechaApertura = dto.FechaApertura,
                    FechaCierre = dto.FechaCierre
                };
                var creado = await _repo.CreateAsync(periodo);
                return StatusCode(201, MapToDto(creado));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPeriodoController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}/activar")]
        public async Task<IActionResult> Activar(int id)
        {
            try
            {
                var p = await _repo.GetByIdAsync(id)
                    ?? throw new AbrilException("Período no encontrado.", 404);
                p.Activo = true;
                await _repo.UpdateAsync(p);
                return Ok(MapToDto(p));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPeriodoController.Activar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}/desactivar")]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                var p = await _repo.GetByIdAsync(id)
                    ?? throw new AbrilException("Período no encontrado.", 404);
                p.Activo = false;
                await _repo.UpdateAsync(p);
                return Ok(MapToDto(p));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPeriodoController.Desactivar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private static EvPeriodoDto MapToDto(EvPeriodo p) => new()
        {
            Id = p.Id,
            Mes = p.Mes,
            Anio = p.Anio,
            FechaApertura = p.FechaApertura,
            FechaCierre = p.FechaCierre,
            Activo = p.Activo
        };
    }
}
