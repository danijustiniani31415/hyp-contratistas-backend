using Abril_Backend.Application.Exceptions;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.Habilitacion.Application;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Inducciones;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/inducciones")]
    [Authorize]
    public class InduccionController : ControllerBase
    {
        private readonly IInduccionRepository _repo;
        private readonly ILogger<InduccionController> _logger;
        private readonly IConfiguration _configuration;

        public InduccionController(IInduccionRepository repo, ILogger<InduccionController> logger, IConfiguration configuration)
        {
            _repo = repo;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InduccionCreateDto dto)
        {
            try
            {
                var userId = ParseUserIdOrZero();
                var ids = await _repo.CreateAsync(dto, userId);
                return StatusCode(201, new { ids, total = ids.Count });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InduccionController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("trabajadores-por-programar")]
        [HttpGet("/api/v1/inducciones/trabajadores-por-programar")]
        public async Task<IActionResult> GetTrabajadoresPorProgramar(
            [FromQuery] int proyectoId,
            [FromQuery] int? empresaId,
            [FromQuery] string? search)
        {
            try
            {
                if (proyectoId <= 0)
                    return BadRequest(new { message = "proyectoId es requerido." });

                var items = await _repo.GetTrabajadoresPorProgramarAsync(empresaId, proyectoId, search);
                return Ok(items);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InduccionController.GetTrabajadoresPorProgramar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] int? proyectoId,
            [FromQuery] int? empresaId,
            [FromQuery] string? estado,
            [FromQuery] DateTime? fechaDesde,
            [FromQuery] DateTime? fechaHasta)
        {
            try
            {
                if (User.FindFirst("tipo")?.Value == "CONTRATISTA")
                {
                    if (!int.TryParse(User.FindFirst("empresaId")?.Value, out var empresaJwt))
                        return StatusCode(403, new { message = "Token de contratista inválido." });
                    empresaId = empresaJwt;

                    var systemRoles = User.FindFirst("systemRoles")?.Value ?? "";
                    if (systemRoles.Split(',').Contains("49"))
                        empresaId = null;
                }

                var proyectosFiltro = ContratistaProyectosHelper.GetProyectosFiltro(User);
                if (proyectosFiltro != null && proyectoId == null)
                {
                    if (proyectosFiltro.Count == 1)
                        proyectoId = proyectosFiltro[0];
                    else if (proyectosFiltro.Count == 0)
                        return Ok(new List<object>());
                    // Si tiene múltiples proyectos, por ahora no filtrar — se implementa después
                }

                _logger.LogInformation("GetInducciones — empresaId={EmpresaId}, systemRoles={SystemRoles}",
                    empresaId, User.FindFirst("systemRoles")?.Value);

                if (fechaDesde.HasValue) fechaDesde = DateTime.SpecifyKind(fechaDesde.Value, DateTimeKind.Utc);
                if (fechaHasta.HasValue) fechaHasta = DateTime.SpecifyKind(fechaHasta.Value, DateTimeKind.Utc);

                var items = await _repo.GetAsync(proyectoId, empresaId, estado, fechaDesde, fechaHasta);
                return Ok(items);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InduccionController.GetList"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/aprobar")]
        public async Task<IActionResult> Aprobar(int id)
        {
            try
            {
                await _repo.AprobarAsync(id);
                return Ok(new { message = "Inducción aprobada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InduccionController.Aprobar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/rechazar")]
        public async Task<IActionResult> Rechazar(int id)
        {
            try
            {
                await _repo.RechazarAsync(id);
                return Ok(new { message = "Inducción rechazada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InduccionController.Rechazar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("aprobar-batch")]
        public async Task<IActionResult> AprobarBatch([FromBody] InduccionBatchAprobarDto dto)
        {
            try
            {
                if (dto.Ids.Count == 0)
                    return BadRequest(new { message = "Debe enviar al menos un id." });

                await _repo.AprobarBatchAsync(dto.Ids);
                return Ok(new { message = $"{dto.Ids.Count} inducción(es) aprobada(s)." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InduccionController.AprobarBatch"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [AllowAnonymous]
        [HttpPost("cron/reset-falta")]
        public async Task<IActionResult> ResetFalta()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}") return Unauthorized();

                var afectados = await _repo.ResetFaltaAsync();
                return Ok(new { afectados, mensaje = $"Se marcaron {afectados} inducciones como FALTA." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en cron reset-falta inducciones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private int ParseUserIdOrZero()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }
}
