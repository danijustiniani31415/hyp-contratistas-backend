using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/gestion-salidas")]
    [Authorize]
    public class GestionSalidaController : ControllerBase
    {
        private readonly IGestionSalidaService _service;
        private readonly ILogger<GestionSalidaController> _logger;

        public GestionSalidaController(IGestionSalidaService service, ILogger<GestionSalidaController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? workerId, [FromQuery] int? lugarProyectoId, [FromQuery] string? estadoRendicion, [FromQuery] string? estadoAprobacion, [FromQuery] string? estadoReembolso = null, [FromQuery] List<int>? areaScopeIds = null, [FromQuery] int page = 1, [FromQuery] string? sortBy = null, [FromQuery] string? sortDir = null, [FromQuery] bool soloHoy = false)
        {
            try
            {
                var currentUserId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;

                var filters = new GestionSalidaFiltersDto
                {
                    WorkerId            = workerId,
                    LugarProyectoId     = lugarProyectoId,
                    EstadoRendicion     = estadoRendicion,
                    EstadoAprobacion    = estadoAprobacion,
                    EstadoReembolso     = estadoReembolso,
                    FilterAreaScopeIds  = areaScopeIds,
                    SoloHoy             = soloHoy,
                    CurrentUserId       = currentUserId,
                    SeesAllOverride     = User.IsInRole(Roles.UsuarioRecepcion),
                    TieneRolTesorero    = User.IsInRole(Roles.Tesorero),
                    Page                = page < 1 ? 1 : page,
                    SortBy              = sortBy,
                    SortDir             = sortDir,
                };
                return Ok(await _service.GetPaged(filters));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.GetAll");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("exportar-excel")]
        public async Task<IActionResult> ExportarExcel([FromQuery] int? workerId, [FromQuery] int? lugarProyectoId, [FromQuery] string? estadoRendicion, [FromQuery] string? estadoAprobacion, [FromQuery] string? estadoReembolso = null, [FromQuery] List<int>? areaScopeIds = null, [FromQuery] bool soloHoy = false)
        {
            try
            {
                var currentUserId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;

                var filters = new GestionSalidaFiltersDto
                {
                    WorkerId           = workerId,
                    LugarProyectoId    = lugarProyectoId,
                    EstadoRendicion    = estadoRendicion,
                    EstadoAprobacion   = estadoAprobacion,
                    EstadoReembolso    = estadoReembolso,
                    FilterAreaScopeIds = areaScopeIds,
                    SoloHoy            = soloHoy,
                    CurrentUserId      = currentUserId,
                    SeesAllOverride    = User.IsInRole(Roles.UsuarioRecepcion),
                    TieneRolTesorero   = User.IsInRole(Roles.Tesorero),
                };
                var bytes = await _service.GetExcel(filters);
                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Gestion_Salidas.xlsx"
                );
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.ExportarExcel");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("{id:int}/detalle")]
        public async Task<IActionResult> GetDetalle(int id)
        {
            try
            {
                var detalle = await _service.GetDetalle(id);
                if (detalle == null)
                    return NotFound(new { message = "Solicitud no encontrada." });
                return Ok(detalle);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.GetDetalle");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("filter-data")]
        public async Task<IActionResult> GetFilterData()
        {
            try
            {
                var currentUserId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;

                return Ok(await _service.GetFilterData(currentUserId, User.IsInRole(Roles.UsuarioRecepcion), User.IsInRole(Roles.Tesorero)));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.GetFilterData");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{id:int}/aprobar")]
        public async Task<IActionResult> Aprobar(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                await _service.Aprobar(id, userId.Value);
                return Ok(new { message = "Solicitud aprobada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.Aprobar");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{id:int}/rechazar")]
        public async Task<IActionResult> Rechazar(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                await _service.Rechazar(id, userId.Value);
                return Ok(new { message = "Solicitud rechazada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.Rechazar");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{id:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                await _service.Cancelar(id, userId.Value);
                return Ok(new { message = "Solicitud cancelada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.Cancelar");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{id:int}/hora-salida-real")]
        public async Task<IActionResult> SetHoraSalidaReal(int id, [FromBody] RegistrarHoraSalidaRealDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                await _service.SetHoraSalidaReal(id, dto.HoraSalidaReal, userId.Value);
                return Ok(new { message = "Hora real registrada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.SetHoraSalidaReal");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("{id:int}/hora-retorno-real")]
        public async Task<IActionResult> SetHoraRetornoReal(int id, [FromBody] RegistrarHoraRetornoRealDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                await _service.SetHoraRetornoReal(id, dto.HoraRetornoReal, userId.Value);
                return Ok(new { message = "Hora real registrada." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.SetHoraRetornoReal");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("marcar-rendidas")]
        public async Task<IActionResult> MarcarRendidas([FromBody] MarcarRendidasBulkDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                if (dto?.Ids == null || dto.Ids.Count == 0)
                    return BadRequest(new { message = "Debes seleccionar al menos una solicitud." });

                var (pdfBytes, count) = await _service.RendirYGenerarPlanilla(dto.Ids, userId.Value);

                // Header custom para que el frontend pueda mostrar el contador en el toast.
                Response.Headers.Append("X-Rendidas-Count", count.ToString());
                Response.Headers.Append("Access-Control-Expose-Headers", "X-Rendidas-Count, Content-Disposition");

                var filename = $"Planilla_Rendicion_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(pdfBytes, "application/pdf", filename);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.MarcarRendidas");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Rinde de una vez TODAS las salidas del mes anterior que estén listas (aprobadas, no
        /// rendidas y con las capturas de todos sus trayectos) dentro del alcance de visibilidad del
        /// usuario, respetando los filtros de trabajador/área/proyecto que vengan en la query — la
        /// acción rinde lo que la pantalla está mostrando. Las que no cumplen se ignoran.
        /// </summary>
        [HttpPatch("rendir-mes-anterior")]
        public async Task<IActionResult> RendirMesAnterior(
            [FromQuery] int? workerId,
            [FromQuery] int? lugarProyectoId,
            [FromQuery] List<int>? areaScopeIds = null)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                var filters = new GestionSalidaFiltersDto
                {
                    WorkerId           = workerId,
                    LugarProyectoId    = lugarProyectoId,
                    FilterAreaScopeIds = areaScopeIds,
                    CurrentUserId      = userId.Value,
                    SeesAllOverride    = User.IsInRole(Roles.UsuarioRecepcion),
                };
                var (pdfBytes, count) = await _service.RendirMesAnterior(filters, userId.Value);

                Response.Headers.Append("X-Rendidas-Count", count.ToString());
                Response.Headers.Append("Access-Control-Expose-Headers", "X-Rendidas-Count, Content-Disposition");

                var filename = $"Planilla_Rendicion_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(pdfBytes, "application/pdf", filename);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.RendirMesAnterior");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Reembolso ────────────────────────────────────────────────────────

        /// <summary>
        /// Aprueba el reembolso de las salidas seleccionadas (rendidas y con Consolidado del S10).
        /// Avisa por correo a cada solicitante.
        /// </summary>
        [HttpPatch("reembolso/aprobar")]
        public async Task<IActionResult> AprobarReembolso([FromBody] ReembolsoBulkDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Usuario no autenticado." });
                if (dto?.Ids == null || dto.Ids.Count == 0)
                    return BadRequest(new { message = "Debes seleccionar al menos una salida." });

                return Ok(await _service.DecidirReembolso(dto.Ids, aprobar: true, observacion: null, userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.AprobarReembolso");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Rechaza el reembolso de las salidas seleccionadas con una observación. El trabajador la
        /// recibe por correo y la subsana volviendo a adjuntar el Consolidado del S10.
        /// </summary>
        [HttpPatch("reembolso/rechazar")]
        public async Task<IActionResult> RechazarReembolso([FromBody] RechazarReembolsoBulkDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Usuario no autenticado." });
                if (dto?.Ids == null || dto.Ids.Count == 0)
                    return BadRequest(new { message = "Debes seleccionar al menos una salida." });
                if (string.IsNullOrWhiteSpace(dto.Observacion))
                    return BadRequest(new { message = "Escribe la observación del rechazo." });

                return Ok(await _service.DecidirReembolso(dto.Ids, aprobar: false, dto.Observacion, userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.RechazarReembolso");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Firma la planilla de rendición de las salidas con reembolso aprobado. Responde 409
        /// cuando el usuario aún no registró su firma: el frontend usa ese código para abrir el
        /// modal donde la dibuja sin salir de la pantalla.
        /// </summary>
        [HttpPatch("reembolso/firmar")]
        public async Task<IActionResult> FirmarPlanillas([FromBody] ReembolsoBulkDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Usuario no autenticado." });
                if (dto?.Ids == null || dto.Ids.Count == 0)
                    return BadRequest(new { message = "Debes seleccionar al menos una salida." });

                return Ok(await _service.FirmarPlanillas(dto.Ids, userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.FirmarPlanillas");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Marca como pagadas las salidas firmadas seleccionadas. Es la acción de Tesorería: exige
        /// el rol TESORERO en el token; que además el puesto sea de categoría Tesorero lo valida el
        /// servicio al resolver la visibilidad.
        /// </summary>
        [HttpPatch("reembolso/pagar")]
        [Authorize(Roles = Roles.Tesorero)]
        public async Task<IActionResult> MarcarPagadas([FromBody] ReembolsoBulkDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Usuario no autenticado." });
                if (dto?.Ids == null || dto.Ids.Count == 0)
                    return BadRequest(new { message = "Debes seleccionar al menos una salida." });

                return Ok(await _service.MarcarPagadas(dto.Ids, userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.MarcarPagadas");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Adjunta (o reemplaza) el PDF Consolidado del S10 de una salida ya rendida.
        /// <c>ambito</c>: "Rendicion" (cubre toda la planilla, es el default de la pantalla) o
        /// "Solicitud" (cubre solo esta salida).
        /// </summary>
        [HttpPost("{id:int}/consolidado-s10")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
        public async Task<IActionResult> UploadConsolidadoS10(int id, [FromForm] IFormFile file, [FromForm] string? ambito)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                ConsolidadoS10Ambito ambitoEnum;
                if (string.IsNullOrWhiteSpace(ambito))
                    ambitoEnum = ConsolidadoS10Ambito.Rendicion;
                else if (!Enum.TryParse(ambito.Trim(), ignoreCase: true, out ambitoEnum) || !Enum.IsDefined(ambitoEnum))
                    return BadRequest(new { message = "Ámbito inválido: usa \"Rendicion\" o \"Solicitud\"." });

                return Ok(await _service.UploadConsolidadoS10(id, ambitoEnum, file, userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GestionSalidaController.UploadConsolidadoS10");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>UserId del token, o null si el claim no viene o no es numérico.</summary>
        private int? GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : (int?)null;
    }
}
