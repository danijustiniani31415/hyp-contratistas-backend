using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Bandeja;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/bandeja")]
    [Authorize]
    public class BandejaController : ControllerBase
    {
        private readonly IBandejaRepository _repo;
        private readonly IInduccionRepository _induccionRepo;
        private readonly ILogger<BandejaController> _logger;

        public BandejaController(IBandejaRepository repo, IInduccionRepository induccionRepo, ILogger<BandejaController> logger)
        {
            _repo = repo;
            _induccionRepo = induccionRepo;
            _logger = logger;
        }

        private bool EsContratista() => User.FindFirst("tipo")?.Value == "CONTRATISTA";

        /// <summary>Bandeja es la cola de APROBACIÓN interna (revisor de Abril aprueba/rechaza lo
        /// que sube el contratista) — no tenía ningún control server-side: ni de empresa en las
        /// listas, ni de rol en las acciones de Aprobar*. Un contratista podía listar y aprobar
        /// entregables de cualquier otra empresa, o aprobarse a sí mismo.</summary>
        private int? GetEmpresaIdContratista() =>
            EsContratista() && int.TryParse(User.FindFirst("empresaId")?.Value, out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> GetPendientes(
            [FromQuery] string? tipo,
            [FromQuery] int? proyectoId,
            [FromQuery] int? empresaId,
            [FromQuery] string? responsable,
            [FromQuery] string? search,
            [FromQuery] int? areaScopeId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var empresaContratista = GetEmpresaIdContratista();
                if (empresaContratista.HasValue) empresaId = empresaContratista.Value;

                var proyectosFiltro = ContratistaProyectosHelper.GetProyectosFiltro(User);
                if (proyectosFiltro != null && proyectoId == null)
                {
                    if (proyectosFiltro.Count == 1)
                        proyectoId = proyectosFiltro[0];
                    else if (proyectosFiltro.Count == 0)
                        return Ok(new PagedResult<object> { Data = new() });
                    // Si tiene múltiples proyectos, por ahora no filtrar — se implementa después
                }

                var (items, total) = await _repo.GetPendientesAsync(tipo, proyectoId, empresaId, responsable, search, page, pageSize, areaScopeId);

                var result = new PagedResult<BandejaItemDto>
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
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.GetPendientes"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("proyectos")]
        public async Task<IActionResult> GetProyectosUnicos()
        {
            try
            {
                // Filtro auxiliar de la bandeja del revisor interno — un contratista no tiene uso
                // legítimo para esto (solo ve su propia empresa/proyectos igual).
                if (EsContratista()) return Forbid();
                return Ok(await _repo.GetProyectosUnicosAsync());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.GetProyectosUnicos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("empresas")]
        public async Task<IActionResult> GetEmpresasUnicas()
        {
            try
            {
                if (EsContratista()) return Forbid();
                return Ok(await _repo.GetEmpresasUnicasAsync());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.GetEmpresasUnicas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("cursor")]
        public async Task<IActionResult> GetPendientesCursor(
            [FromQuery] string? tipo,
            [FromQuery] int? proyectoId,
            [FromQuery] int? empresaId,
            [FromQuery] string? responsable,
            [FromQuery] string? search,
            [FromQuery] int? areaScopeId,
            [FromQuery] string? cursor,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var empresaContratista = GetEmpresaIdContratista();
                if (empresaContratista.HasValue) empresaId = empresaContratista.Value;

                var proyectosFiltro = ContratistaProyectosHelper.GetProyectosFiltro(User);
                if (proyectosFiltro != null && proyectoId == null)
                {
                    if (proyectosFiltro.Count == 1)
                        proyectoId = proyectosFiltro[0];
                    else if (proyectosFiltro.Count == 0)
                        return Ok(new PagedResult<object> { Data = new() });
                    // Si tiene múltiples proyectos, por ahora no filtrar — se implementa después
                }

                var result = await _repo.GetPendientesCursorAsync(tipo, proyectoId, empresaId, responsable, search, cursor, pageSize, areaScopeId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.GetPendientesCursor"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("trabajador/{id:int}")]
        public async Task<IActionResult> AprobarTrabajador(int id, [FromBody] BandejaAprobarDto dto)
        {
            try
            {
                if (EsContratista()) return Forbid();
                var userId = ParseUserIdOrZero();
                var entity = await _repo.AprobarTrabajadorAsync(id, dto, userId);
                if (entity is null) return NotFound(new { message = "Entregable no encontrado." });
                return Ok(entity);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.AprobarTrabajador"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("empresa/{id:int}")]
        public async Task<IActionResult> AprobarEmpresa(int id, [FromBody] BandejaAprobarDto dto)
        {
            try
            {
                if (EsContratista()) return Forbid();
                var userId = ParseUserIdOrZero();
                var entity = await _repo.AprobarEmpresaAsync(id, dto, userId);
                if (entity is null) return NotFound(new { message = "Entregable no encontrado." });
                return Ok(entity);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.AprobarEmpresa"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("induccion/{id:int}")]
        public async Task<IActionResult> AprobarInduccion(int id)
        {
            try
            {
                if (EsContratista()) return Forbid();
                await _induccionRepo.AprobarAsync(id);
                return Ok(new { message = "Inducción aprobada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.AprobarInduccion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("equipo/{id:int}")]
        public async Task<IActionResult> AprobarEquipo(int id, [FromBody] BandejaAprobarDto dto)
        {
            try
            {
                if (EsContratista()) return Forbid();
                var userId = ParseUserIdOrZero();
                var entity = await _repo.AprobarEquipoAsync(id, dto, userId);
                if (entity is null) return NotFound(new { message = "Entregable no encontrado." });
                return Ok(entity);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.AprobarEquipo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("bulk-aprobar")]
        public async Task<IActionResult> BulkAprobar([FromBody] BandejaBulkAprobarDto dto)
        {
            try
            {
                if (EsContratista()) return Forbid();
                if (dto.Ids == null || dto.Ids.Count == 0)
                    return BadRequest(new { message = "Debe enviar al menos un id." });

                var userId = ParseUserIdOrZero();
                var noEncontrados = new List<int>();
                var procesados = 0;
                var aprobarDto = new BandejaAprobarDto { Estado = "Aprobado" };

                switch (dto.Tipo?.ToUpper())
                {
                    case "TRABAJADOR":
                        foreach (var id in dto.Ids)
                        {
                            var r = await _repo.AprobarTrabajadorAsync(id, aprobarDto, userId);
                            if (r is null) noEncontrados.Add(id); else procesados++;
                        }
                        break;
                    case "EMPRESA":
                        foreach (var id in dto.Ids)
                        {
                            var r = await _repo.AprobarEmpresaAsync(id, aprobarDto, userId);
                            if (r is null) noEncontrados.Add(id); else procesados++;
                        }
                        break;
                    case "EQUIPO":
                        foreach (var id in dto.Ids)
                        {
                            var r = await _repo.AprobarEquipoAsync(id, aprobarDto, userId);
                            if (r is null) noEncontrados.Add(id); else procesados++;
                        }
                        break;
                    case "INDUCCION":
                        await _induccionRepo.AprobarBatchAsync(dto.Ids);
                        procesados = dto.Ids.Count;
                        break;
                    default:
                        return BadRequest(new { message = "Tipo inválido. Valores permitidos: TRABAJADOR, EMPRESA, EQUIPO, INDUCCION." });
                }

                return Ok(new BandejaBulkResultDto { Procesados = procesados, NoEncontrados = noEncontrados });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en BandejaController.BulkAprobar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private int ParseUserIdOrZero()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }
}
