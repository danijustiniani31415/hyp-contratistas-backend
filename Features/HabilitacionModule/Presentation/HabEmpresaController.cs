using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.HabEmpresa;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/empresas/{empresaId:int}")]
    [Authorize]
    public class HabEmpresaController : ControllerBase
    {
        private static readonly string[] RolesAprobadoresSsoma = [Roles.AdministradorSsoma, Roles.AdministradorUdp];
        private static readonly string[] RolesAprobadoresAdmin = [Roles.AdministradorAdministracion, Roles.AdministradorUdp];

        private readonly IHabEmpresaRepository _repo;
        private readonly ILogger<HabEmpresaController> _logger;

        public HabEmpresaController(IHabEmpresaRepository repo, ILogger<HabEmpresaController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet("entregables")]
        public async Task<IActionResult> GetEntregables(
            int empresaId,
            [FromQuery] int proyectoId,
            [FromQuery] int? mes,
            [FromQuery] int? anio)
        {
            try
            {
                var items = await _repo.GetEntregablesEmpresaAsync(empresaId, proyectoId, mes, anio);
                return Ok(items);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabEmpresaController.GetEntregables"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("entregables/{id:int}")]
        public async Task<IActionResult> UpdateEntregable(int empresaId, int id, [FromBody] EmpresaEntregableUpdateDto dto)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";

                // Solo forzar "Enviado" cuando el contratista realmente está subiendo/reemplazando
                // un archivo. Un PUT que solo actualiza observaciones no debe resubir un entregable
                // ya aprobado.
                if (esContratista && !string.IsNullOrWhiteSpace(dto.ArchivoUrl))
                {
                    dto.Estado = "Enviado";
                    dto.Vigencia = null;
                }

                var esAccionRestringida =
                    string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dto.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase) ||
                    dto.Vigencia.HasValue;

                if (!esContratista && esAccionRestringida)
                {
                    var responsable = await _repo.GetResponsableItemEmpresaAsync(id);
                    var rolesPermitidos = string.Equals(responsable, "SSOMA", StringComparison.OrdinalIgnoreCase)
                        ? RolesAprobadoresSsoma
                        : RolesAprobadoresAdmin;
                    var tienePermiso = User.FindAll(ClaimTypes.Role)
                        .Any(c => rolesPermitidos.Contains(c.Value, StringComparer.OrdinalIgnoreCase));
                    if (!tienePermiso)
                        return StatusCode(403, new { message = "No tienes permiso para modificar este documento." });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                int? userId = userIdClaim != null && int.TryParse(userIdClaim.Value, out var uid) ? uid : null;

                int? empresaIdClaim = null;
                if (esContratista)
                {
                    var ec = User.FindFirst("empresaId")?.Value;
                    if (int.TryParse(ec, out var eid)) empresaIdClaim = eid;
                    userId = null;
                }

                var actualizado = await _repo.UpdateEntregableEmpresaAsync(id, dto, userId, empresaIdClaim);
                return Ok(actualizado);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabEmpresaController.UpdateEntregable"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("entregables/{itemId:int}/versiones")]
        public async Task<IActionResult> GetVersionesEntregable(int empresaId, int itemId)
        {
            try
            {
                var versiones = await _repo.GetVersionesDocumentoEmpresaAsync(empresaId, itemId);
                return Ok(versiones);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabEmpresaController.GetVersionesEntregable"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("proyectos-disponibles")]
        public async Task<IActionResult> GetProyectosDisponibles(int empresaId)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";
                if (esContratista && !EmpresaJwtCoinside(empresaId))
                    return StatusCode(403, new { message = "Solo puede consultar su propia empresa." });

                var proyectos = await _repo.GetProyectosDisponiblesAsync(empresaId);
                return Ok(proyectos);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabEmpresaController.GetProyectosDisponibles"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("activar-proyecto")]
        public async Task<IActionResult> ActivarProyecto(int empresaId, [FromBody] ActivarProyectoDto dto)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";
                if (esContratista && !EmpresaJwtCoinside(empresaId))
                    return StatusCode(403, new { message = "Solo puede activar su propia empresa." });

                await _repo.ActivarProyectoAsync(empresaId, dto.ProyectoId);
                return Ok(new { message = "Empresa activada en proyecto correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabEmpresaController.ActivarProyecto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("desactivar-proyecto")]
        public async Task<IActionResult> DesactivarProyecto(int empresaId, [FromBody] ActivarProyectoDto dto)
        {
            try
            {
                var esContratista = User.FindFirst("tipo")?.Value == "CONTRATISTA";
                if (esContratista && !EmpresaJwtCoinside(empresaId))
                    return StatusCode(403, new { message = "Solo puede desactivar su propia empresa." });

                await _repo.DesactivarProyectoAsync(empresaId, dto.ProyectoId);
                return Ok(new { message = "Empresa desactivada del proyecto correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabEmpresaController.DesactivarProyecto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("entregables/{entregableId:int}/mes")]
        public async Task<IActionResult> AprobarMes(int empresaId, int entregableId, [FromBody] EmpresaEntregableUpdateDto dto)
        {
            try
            {
                var esAccionRestringida =
                    string.Equals(dto.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dto.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase) ||
                    dto.Vigencia.HasValue;

                if (esAccionRestringida)
                {
                    var responsable = await _repo.GetResponsableItemEmpresaAsync(entregableId);
                    var rolesPermitidos = string.Equals(responsable, "SSOMA", StringComparison.OrdinalIgnoreCase)
                        ? RolesAprobadoresSsoma
                        : RolesAprobadoresAdmin;
                    var tienePermiso = User.FindAll(ClaimTypes.Role)
                        .Any(c => rolesPermitidos.Contains(c.Value, StringComparer.OrdinalIgnoreCase));
                    if (!tienePermiso)
                        return StatusCode(403, new { message = "No tienes permiso para modificar este documento." });
                }

                var userId = ObtenerUserId();
                var result = await _repo.UpdateEntregableEmpresaAsync(entregableId, dto, userId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabEmpresaController.AprobarMes"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("archivos/{archivoId:int}")]
        public async Task<IActionResult> EliminarArchivo(int empresaId, int archivoId)
        {
            try
            {
                await _repo.EliminarArchivoVersionAsync(archivoId, empresaId);
                return Ok(new { message = "Archivo eliminado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en HabEmpresaController.EliminarArchivo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private int? ObtenerUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        private bool EmpresaJwtCoinside(int empresaIdRuta)
        {
            var claim = User.FindFirst("empresaId")?.Value;
            return int.TryParse(claim, out var jwtEmpresaId) && jwtEmpresaId == empresaIdRuta;
        }
    }
}
