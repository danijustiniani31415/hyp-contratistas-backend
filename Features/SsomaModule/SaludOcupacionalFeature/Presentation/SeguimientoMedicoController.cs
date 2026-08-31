using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AltaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CitaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.EquipoPrestado;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.accidentes")]
    public class SeguimientoMedicoController : ControllerBase
    {
        private readonly ICitaMedicaService _citas;
        private readonly IEquipoPrestadoService _equipos;
        private readonly IAltaMedicaService _alta;
        private readonly ILogger<SeguimientoMedicoController> _logger;

        public SeguimientoMedicoController(
            ICitaMedicaService citas,
            IEquipoPrestadoService equipos,
            IAltaMedicaService alta,
            ILogger<SeguimientoMedicoController> logger)
        {
            _citas = citas;
            _equipos = equipos;
            _alta = alta;
            _logger = logger;
        }

        private int? CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        // ── CATÁLOGOS ──────────────────────────────────────────────────

        [HttpGet("accidentes/tipos-seguimiento")]
        public async Task<IActionResult> GetTiposSeguimiento()
        {
            try
            {
                var tiposCita = await _citas.GetTipos();
                var tiposEquipo = await _equipos.GetTipos();
                var tiposAlta = await _alta.GetTipos();
                return Ok(new
                {
                    tiposCita = tiposCita.Select(t => new { t.Id, t.Nombre }),
                    tiposEquipo = tiposEquipo.Select(t => new { t.Id, t.Nombre }),
                    tiposAlta = tiposAlta.Select(t => new { t.Id, t.Nombre }),
                });
            }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── CITAS MÉDICAS ──────────────────────────────────────────────

        [HttpGet("accidentes/{accidenteId:int}/citas")]
        public async Task<IActionResult> GetCitas(int accidenteId)
        {
            try { return Ok(await _citas.GetByAccidenteId(accidenteId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("accidentes/{accidenteId:int}/citas")]
        public async Task<IActionResult> CreateCita(int accidenteId, [FromBody] CitaMedicaCreateDto dto)
        {
            try
            {
                var id = await _citas.Create(accidenteId, dto, CurrentUserId());
                return Ok(new { id, message = "Cita médica registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("accidentes/citas/{id:int}")]
        public async Task<IActionResult> UpdateCita(int id, [FromBody] CitaMedicaUpdateDto dto)
        {
            try
            {
                await _citas.Update(id, dto);
                return Ok(new { message = "Cita médica actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("accidentes/citas/{id:int}")]
        public async Task<IActionResult> DeleteCita(int id)
        {
            try
            {
                await _citas.Delete(id);
                return Ok(new { message = "Cita médica eliminada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── EQUIPOS PRESTADOS ──────────────────────────────────────────

        [HttpGet("accidentes/{accidenteId:int}/equipos")]
        public async Task<IActionResult> GetEquipos(int accidenteId)
        {
            try { return Ok(await _equipos.GetByAccidenteId(accidenteId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("accidentes/{accidenteId:int}/equipos")]
        public async Task<IActionResult> CreateEquipo(int accidenteId, [FromBody] EquipoPrestadoCreateDto dto)
        {
            try
            {
                var id = await _equipos.Create(accidenteId, dto, CurrentUserId());
                return Ok(new { id, message = "Equipo prestado registrado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("accidentes/equipos/{id:int}/devolver")]
        public async Task<IActionResult> DevolverEquipo(int id, [FromBody] EquipoPrestadoDevolverDto dto)
        {
            try
            {
                await _equipos.Devolver(id, dto);
                return Ok(new { message = "Devolución de equipo registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("accidentes/equipos/{id:int}")]
        public async Task<IActionResult> DeleteEquipo(int id)
        {
            try
            {
                await _equipos.Delete(id);
                return Ok(new { message = "Equipo prestado eliminado exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ── ALTA MÉDICA ────────────────────────────────────────────────

        [HttpGet("accidentes/{accidenteId:int}/alta")]
        public async Task<IActionResult> GetAlta(int accidenteId)
        {
            try { return Ok(await _alta.GetByAccidenteId(accidenteId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("accidentes/{accidenteId:int}/alta")]
        public async Task<IActionResult> CreateAlta(int accidenteId, [FromBody] AltaMedicaCreateDto dto)
        {
            try
            {
                var id = await _alta.Create(accidenteId, dto, CurrentUserId());
                return Ok(new { id, message = "Alta médica registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("accidentes/{accidenteId:int}/alta")]
        public async Task<IActionResult> UpdateAlta(int accidenteId, [FromBody] AltaMedicaUpdateDto dto)
        {
            try
            {
                await _alta.Update(accidenteId, dto);
                return Ok(new { message = "Alta médica actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("accidentes/{accidenteId:int}/alta")]
        public async Task<IActionResult> DeleteAlta(int accidenteId)
        {
            try
            {
                await _alta.Delete(accidenteId);
                return Ok(new { message = "Alta médica eliminada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SeguimientoMedicoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
