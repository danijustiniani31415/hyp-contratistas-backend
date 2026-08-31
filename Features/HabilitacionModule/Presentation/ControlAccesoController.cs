using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.ControlAcceso;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/control-acceso")]
    [Authorize]
    public class ControlAccesoController : ControllerBase
    {
        private readonly IControlAccesoRepository _repo;
        private readonly ILogger<ControlAccesoController> _logger;

        public ControlAccesoController(IControlAccesoRepository repo, ILogger<ControlAccesoController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet("consulta")]
        public async Task<IActionResult> GetConsulta(
            [FromQuery] string? search,
            [FromQuery] int? proyectoId)
        {
            try
            {
                _logger.LogInformation("Consultar: search={search}, proyectoId={proyectoId}", search, proyectoId);
                var result = await _repo.GetConsultaAsync(search, proyectoId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.GetConsulta"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("no-autorizados")]
        public async Task<IActionResult> GetNoAutorizados(
            [FromQuery] int proyectoId,
            [FromQuery] string? estadoHabilitacion)
        {
            try
            {
                var result = await _repo.GetNoAutorizadosAsync(proyectoId, estadoHabilitacion);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.GetNoAutorizados"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("oficina-central")]
        public async Task<IActionResult> GetOficinaCentral([FromQuery] int? proyectoId)
        {
            try
            {
                var result = await _repo.GetOficinaCentralAsync(proyectoId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.GetOficinaCentral"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("inducciones-hoy")]
        [AllowAnonymous] // TODO: Quitar [AllowAnonymous] cuando se confirme fix de fechas Lima UTC-5
        public async Task<IActionResult> GetInduccionesHoy()
        {
            try
            {
                var result = await _repo.GetInduccionesHoyAsync();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.GetInduccionesHoy"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("inducciones/{induccionId:int}/confirmar-ingreso")]
        public async Task<IActionResult> ConfirmarIngreso(int induccionId)
        {
            try
            {
                await _repo.ConfirmarIngresoAsync(induccionId);
                return NoContent();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.ConfirmarIngreso"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("tareo/partidas")]
        public async Task<IActionResult> GetTareoPartidas()
        {
            try
            {
                var result = await _repo.GetPartidasAsync();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.GetTareoPartidas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("tareo/empresas")]
        public async Task<IActionResult> GetTareoEmpresas([FromQuery] int proyectoId)
        {
            try
            {
                var result = await _repo.GetEmpresasContratistasByProyectoAsync(proyectoId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.GetTareoEmpresas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("tareo")]
        public async Task<IActionResult> GetTareo(
            [FromQuery] int proyectoId,
            [FromQuery] string fecha)
        {
            try
            {
                if (!DateOnly.TryParse(fecha, out var fechaParsed))
                    return BadRequest(new { message = "Formato de fecha inválido. Use YYYY-MM-DD." });

                var result = await _repo.GetTareoAsync(proyectoId, fechaParsed);
                if (result == null) return NotFound(new { message = "Tareo no encontrado." });
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.GetTareo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("tareo")]
        public async Task<IActionResult> CreateTareo([FromBody] TareoCreateDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? userId = int.TryParse(userIdClaim, out var uid) ? uid : null;

                var result = await _repo.CreateTareoAsync(dto, userId);
                return StatusCode(201, result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.CreateTareo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("tareo/{id:int}")]
        public async Task<IActionResult> UpdateTareo(int id, [FromBody] TareoCreateDto dto)
        {
            try
            {
                var result = await _repo.UpdateTareoAsync(id, dto);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ControlAccesoController.UpdateTareo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
