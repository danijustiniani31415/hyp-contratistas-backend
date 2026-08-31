using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Equipos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/equipos")]
    [Authorize]
    public class EquipoController : ControllerBase
    {
        private static readonly string[] RolesAprobadores = [Roles.AdministradorSsoma, Roles.AdministradorUdp, Roles.AdministradorAdministracion];

        private readonly IEquipoRepository _repo;
        private readonly ILogger<EquipoController> _logger;

        public EquipoController(IEquipoRepository repo, ILogger<EquipoController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int? proyectoId,
            [FromQuery] int? empresaId,
            [FromQuery] string? search,
            [FromQuery] bool? activo,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";

                if (esContratista)
                {
                    var ec = User.FindFirst("empresaId")?.Value;
                    if (int.TryParse(ec, out var eid)) empresaId = eid;
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

                var (items, total) = await _repo.GetPagedAsync(proyectoId, empresaId, search, activo, page, pageSize);
                var result = new PagedResult<EquipoListDto>
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
            catch (Exception ex) { _logger.LogError(ex, "Error en EquipoController.GetPaged"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("entregables/{id:int}/versiones")]
        public async Task<IActionResult> GetVersiones(int id)
        {
            try { return Ok(await _repo.GetVersionesAsync(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EquipoController.GetVersiones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}/entregables")]
        public async Task<IActionResult> GetEntregables(int id)
        {
            try { return Ok(await _repo.GetEntregablesAsync(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EquipoController.GetEntregables"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EquipoCreateDto dto)
        {
            try
            {
                var creado = await _repo.CreateAsync(dto);
                return StatusCode(201, creado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EquipoController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EquipoCreateDto dto)
        {
            try { return Ok(await _repo.UpdateAsync(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EquipoController.Update"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("entregables/{id:int}")]
        public async Task<IActionResult> UpdateEntregable(int id, [FromBody] EquipoEntregableUpdateDto dto)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";
                var esAprobador = User.FindAll(ClaimTypes.Role).Any(c => RolesAprobadores.Contains(c.Value, StringComparer.OrdinalIgnoreCase));

                // Solo forzar "Enviado" cuando el contratista realmente está subiendo/reemplazando
                // un archivo. Un PUT que solo actualiza observaciones no debe resubir un entregable
                // ya aprobado.
                if (esContratista && !string.IsNullOrWhiteSpace(dto.ArchivoUrl))
                {
                    dto.Estado = "Enviado";
                    dto.Vigencia = null;
                }

                if (string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase) && !esAprobador)
                    return StatusCode(403, new { message = "Solo ADMINISTRADOR SSOMA o ADMINISTRADOR DE UDP pueden aprobar." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int? userId = userIdClaim != null && int.TryParse(userIdClaim.Value, out var uid) ? uid : null;

                int? empresaIdClaim = null;
                if (esContratista)
                {
                    var ec = User.FindFirst("empresaId")?.Value;
                    if (int.TryParse(ec, out var eid)) empresaIdClaim = eid;
                    userId = null;
                }

                return Ok(await _repo.UpdateEntregableAsync(id, dto, userId, empresaIdClaim));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EquipoController.UpdateEntregable"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
