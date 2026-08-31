using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application;
using Abril_Backend.Features.Habilitacion.Application.Dtos.SctrVidaley;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/sctr-vidaley")]
    [Authorize]
    public class SctrVidaLeyController : ControllerBase
    {
        private static readonly string[] RolesAprobadores = [Roles.AdministradorSsoma, Roles.AdministradorUdp, Roles.AdministradorAdministracion];

        private readonly ISctrVidaLeyRepository _repo;
        private readonly ILogger<SctrVidaLeyController> _logger;

        public SctrVidaLeyController(ISctrVidaLeyRepository repo, ILogger<SctrVidaLeyController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int? empresaId,
            [FromQuery] int? proyectoId,
            [FromQuery] string? tipo,
            [FromQuery] int? mes,
            [FromQuery] int? anio,
            [FromQuery] string? estado,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (User.FindFirst("tipo")?.Value == "CONTRATISTA")
                {
                    if (!int.TryParse(User.FindFirst("empresaId")?.Value, out var contraId))
                        return StatusCode(403, new { message = "Token de contratista inválido." });
                    empresaId = contraId;
                }

                var proyectosFiltro = ContratistaProyectosHelper.GetProyectosFiltro(User);
                if (proyectosFiltro != null && proyectoId == null)
                {
                    if (proyectosFiltro.Count == 1)
                        proyectoId = proyectosFiltro[0];
                    else if (proyectosFiltro.Count == 0)
                        return Ok(new PagedResult<object> { Data = new() });
                    // Si tiene múltiples proyectos, por ahora no filtrar — se implementa después
                }

                var (items, total) = await _repo.GetPagedAsync(empresaId, proyectoId, tipo, mes, anio, estado, page, pageSize);
                var result = new PagedResult<SctrVidaLeyDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Data = items
                };
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrVidaLeyController.GetPaged"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _repo.GetByIdAsync(id);
                if (dto is null) return NotFound(new { message = "SCTR/VidaLey no encontrado." });
                return Ok(dto);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrVidaLeyController.GetById"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("por-trabajador/{workerId:int}")]
        public async Task<IActionResult> GetPorTrabajador(int workerId)
        {
            try { return Ok(await _repo.GetPorTrabajadorAsync(workerId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrVidaLeyController.GetPorTrabajador"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("proximos-vencer")]
        public async Task<IActionResult> GetProximosVencer([FromQuery] int dias = 30)
        {
            try { return Ok(await _repo.GetProximosVencerAsync(dias)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrVidaLeyController.GetProximosVencer"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SctrVidaLeyCreateDto dto, [FromQuery] int? empresaId)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";

                int empresaResolved;
                if (esContratista)
                {
                    var empresaClaim = User.FindFirst("empresaId")?.Value;
                    if (!int.TryParse(empresaClaim, out empresaResolved))
                        return StatusCode(403, new { message = "Token de contratista inválido." });
                }
                else
                {
                    if (!empresaId.HasValue)
                        return BadRequest(new { message = "empresaId es requerido para usuarios internos." });
                    empresaResolved = empresaId.Value;
                }

                var creado = await _repo.CreateAsync(dto, empresaResolved);
                return StatusCode(201, creado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                // Volcar toda la cadena de inner exceptions para diagnosticar errores de PG (FK, NOT NULL, etc.)
                var inner = ex;
                var detalle = new System.Text.StringBuilder();
                while (inner != null)
                {
                    detalle.AppendLine($"[{inner.GetType().FullName}] {inner.Message}");
                    inner = inner.InnerException;
                }
                _logger.LogError(ex, "Error en SctrVidaLeyController.Create. Cadena:\n{Detalle}", detalle.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("trabajadores-por-empresa")]
        public async Task<IActionResult> GetTrabajadoresPorEmpresa(
            [FromQuery] int? empresaId,
            [FromQuery] int? proyectoId,
            [FromQuery] string? tipo,
            [FromQuery] string? tipoPoliza,
            [FromQuery] string? estadoSctr,
            [FromQuery] string? estadoVidaLey,
            [FromQuery] string? obraOficina,
            [FromQuery] int? areaScopeId)
        {
            try
            {
                var result = await _repo.GetTrabajadoresPorEmpresaAsync(
                    empresaId, proyectoId, tipo, tipoPoliza, estadoSctr, estadoVidaLey, obraOficina, areaScopeId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrVidaLeyController.GetTrabajadoresPorEmpresa"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] SctrVidaLeyCreateDto dto, [FromQuery] int? empresaId)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";

                int empresaResolved;
                if (esContratista)
                {
                    var empresaClaim = User.FindFirst("empresaId")?.Value;
                    if (!int.TryParse(empresaClaim, out empresaResolved))
                        return StatusCode(403, new { message = "Token de contratista inválido." });
                }
                else
                {
                    if (!empresaId.HasValue)
                        return BadRequest(new { message = "empresaId es requerido para usuarios internos." });
                    empresaResolved = empresaId.Value;
                }

                var actualizado = await _repo.UpdateAsync(id, dto, empresaResolved);
                return Ok(actualizado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrVidaLeyController.Update"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("recalcular-estados")]
        public async Task<IActionResult> RecalcularEstados()
        {
            try
            {
                await _repo.RecalcularEstadoPolizasAsync();
                return Ok("Estados recalculados.");
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrVidaLeyController.RecalcularEstados"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/aprobar")]
        public async Task<IActionResult> Aprobar(int id, [FromBody] SctrVidaLeyAprobarDto dto)
        {
            try
            {
                var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var esAprobador = roles.Any(r => RolesAprobadores.Contains(r, StringComparer.OrdinalIgnoreCase));
                if (!esAprobador)
                    return StatusCode(403, new { message = "Solo ADMINISTRADOR SSOMA o ADMINISTRADOR DE UDP pueden aprobar." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim?.Value, out var userId))
                    return StatusCode(401, new { message = "Inicie sesión." });

                var actualizado = await _repo.AprobarAsync(id, dto, userId);
                return Ok(actualizado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en SctrVidaLeyController.Aprobar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
