using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/solicitud-salidas")]
    [Authorize]
    public class SolicitudSalidaController : ControllerBase
    {
        private readonly ISolicitudSalidaService _service;
        private readonly IGestionSalidaService _gestionSalidaService;
        private readonly ILogger<SolicitudSalidaController> _logger;

        public SolicitudSalidaController(
            ISolicitudSalidaService service,
            IGestionSalidaService gestionSalidaService,
            ILogger<SolicitudSalidaController> logger)
        {
            _service = service;
            _gestionSalidaService = gestionSalidaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMySolicitudes(
            [FromQuery] int? lugarProyectoId,
            [FromQuery] string? estadoAprobacion,
            [FromQuery] string? estadoRendicion)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                    ? id : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                var filters = new SolicitudSalidaFiltersDto
                {
                    LugarProyectoId  = lugarProyectoId,
                    EstadoAprobacion = estadoAprobacion,
                    EstadoRendicion  = estadoRendicion,
                };
                return Ok(await _service.GetByUserId(userId.Value, filters));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SolicitudSalidaController.GetMySolicitudes");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("filter-data")]
        public async Task<IActionResult> GetFilterData()
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                    ? id : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });
                return Ok(await _service.GetFilterData(userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SolicitudSalidaController.GetFilterData");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("form-data")]
        public async Task<IActionResult> GetFormData()
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                    ? id : (int?)null;
                return Ok(await _service.GetFormData(userId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SolicitudSalidaController.GetFormData");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Crea la solicitud. Multipart: <c>data</c> = JSON del SolicitudSalidaCreateDto;
        /// <c>adjuntos</c> + <c>adjuntosTrayectoIndex</c> = documentos adjuntos por índice de
        /// trayecto (0-based), obligatorios cuando el motivo tiene requiere_adjunto = true.
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB total
        public async Task<IActionResult> Create(
            [FromForm] string data,
            [FromForm] List<IFormFile>? adjuntos,
            [FromForm] List<string>? adjuntosTrayectoIndex)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                    ? id : (int?)null;

                SolicitudSalidaCreateDto? dto;
                try
                {
                    // Web defaults = mismas convenciones (camelCase, TimeOnly "HH:mm") que el body JSON anterior.
                    dto = JsonSerializer.Deserialize<SolicitudSalidaCreateDto>(
                        data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException)
                {
                    return BadRequest(new { message = "El campo 'data' no es un JSON válido." });
                }
                if (dto == null)
                    return BadRequest(new { message = "Datos de la solicitud no recibidos." });

                var files = adjuntos ?? new List<IFormFile>();
                var indices = adjuntosTrayectoIndex ?? new List<string>();
                if (files.Count != indices.Count)
                    return BadRequest(new { message = "La cantidad de adjuntos y de índices de trayecto debe coincidir." });

                var pares = new List<(int TrayectoIndex, IFormFile File)>(files.Count);
                for (int i = 0; i < files.Count; i++)
                {
                    if (!int.TryParse(indices[i], out var trayectoIndex))
                        return BadRequest(new { message = $"Índice de trayecto inválido en la posición {i + 1}: '{indices[i]}'." });
                    pares.Add((trayectoIndex, files[i]));
                }

                var newId = await _service.Create(dto, userId, pares);
                return Ok(new { id = newId, message = "Solicitud de salida registrada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SolicitudSalidaController.Create");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("{id:int}/detalle")]
        public async Task<IActionResult> GetDetalle(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                return Ok(await _service.GetDetalle(id, userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SolicitudSalidaController.GetDetalle");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("trayectos/{trayectoId:int}/capturas")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB total
        public async Task<IActionResult> UploadCapturasTrayecto(
            int trayectoId,
            [FromForm] List<IFormFile> files,
            [FromForm] List<string> montos)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                if (files == null || montos == null || files.Count != montos.Count)
                    return BadRequest(new { message = "La cantidad de archivos y montos debe coincidir." });

                var items = new List<(IFormFile File, decimal Monto)>(files.Count);
                for (int i = 0; i < files.Count; i++)
                {
                    if (!decimal.TryParse(montos[i], System.Globalization.NumberStyles.Number,
                                          System.Globalization.CultureInfo.InvariantCulture, out var monto))
                        return BadRequest(new { message = $"Monto inválido en la posición {i + 1}: '{montos[i]}'." });
                    items.Add((files[i], monto));
                }

                var creadas = await _service.UploadCapturasToTrayecto(trayectoId, items, userId.Value);
                return Ok(creadas);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SolicitudSalidaController.UploadCapturasTrayecto");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// El propio trabajador rinde sus solicitudes seleccionadas (aprobadas + con todas sus capturas)
        /// y descarga la planilla de gasto por movilidad. Reutiliza la misma lógica de Gestión de Salidas,
        /// pero restringida a solicitudes del propio usuario (guard de propiedad).
        /// </summary>
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

                var (pdfBytes, count) = await _gestionSalidaService.RendirYGenerarPlanilla(dto.Ids, userId.Value, ownerUserId: userId.Value);

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
                _logger.LogError(ex, "Error en SolicitudSalidaController.MarcarRendidas");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// El propio trabajador cancela una solicitud SUYA que esté Pendiente (registró una que no
        /// debía, o al final no salió). No se pueden cancelar solicitudes de otros trabajadores ni
        /// las ya aprobadas/rechazadas/canceladas.
        /// </summary>
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
                _logger.LogError(ex, "Error en SolicitudSalidaController.Cancelar");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Rinde de una vez TODAS las salidas propias del mes anterior que estén listas (aprobadas,
        /// no rendidas y con las capturas de todos sus trayectos) y descarga la planilla. Las que no
        /// cumplen se ignoran.
        /// </summary>
        [HttpPatch("rendir-mes-anterior")]
        public async Task<IActionResult> RendirMesAnterior()
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                // Mismo camino que MarcarRendidas: el servicio del autoservicio resuelve qué entra
                // (solo lo propio) y la planilla la genera Gestión de Salidas, con guard de propiedad.
                var ids = await _service.GetIdsRendiblesMesAnterior(userId.Value);
                var (pdfBytes, count) = await _gestionSalidaService.RendirYGenerarPlanilla(
                    ids, userId.Value, ownerUserId: userId.Value);

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
                _logger.LogError(ex, "Error en SolicitudSalidaController.RendirMesAnterior");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Adjunta (o reemplaza) el PDF Consolidado del S10 de una salida PROPIA ya rendida.
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

                if (!TryParseAmbito(ambito, out var ambitoEnum))
                    return BadRequest(new { message = "Ámbito inválido: usa \"Rendicion\" o \"Solicitud\"." });

                return Ok(await _service.UploadConsolidadoS10(id, ambitoEnum, file, userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SolicitudSalidaController.UploadConsolidadoS10");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Traduce el ámbito recibido del formulario. Vacío/ausente = Rendicion (el default).</summary>
        private static bool TryParseAmbito(string? ambito, out ConsolidadoS10Ambito parsed)
        {
            if (string.IsNullOrWhiteSpace(ambito))
            {
                parsed = ConsolidadoS10Ambito.Rendicion;
                return true;
            }
            return Enum.TryParse(ambito.Trim(), ignoreCase: true, out parsed)
                && Enum.IsDefined(parsed);
        }

        // ── Endpoints públicos invocados desde los links del email ──────────

        /// <summary>
        /// Avisa al jefe/revisor que el Consolidado del S10 de esa salida ya está adjunto y su
        /// reembolso espera revisión. Lo dispara el propio trabajador desde su pantalla.
        /// </summary>
        [HttpPatch("{id:int}/notificar-revisor")]
        public async Task<IActionResult> NotificarRevisor(int id)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid)
                    ? uid : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                var message = await _service.NotificarRevisorS10(id, userId.Value);
                return Ok(new { message });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SolicitudSalidaController.NotificarRevisor");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("aprobar")]
        [AllowAnonymous]
        public async Task<IActionResult> AprobarDesdeEmail([FromQuery] string token)
        {
            var html = await _service.ProcessAprobarFromEmail(token);
            return Content(html, "text/html; charset=utf-8");
        }

        [HttpGet("rechazar")]
        [AllowAnonymous]
        public IActionResult RechazarFormDesdeEmail([FromQuery] string token)
        {
            var html = _service.RenderRechazarForm(token);
            return Content(html, "text/html; charset=utf-8");
        }

        [HttpPost("rechazar")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> RechazarDesdeEmail([FromForm] string token, [FromForm] string? motivoRechazo)
        {
            var html = await _service.ProcessRechazarFromEmail(token, motivoRechazo);
            return Content(html, "text/html; charset=utf-8");
        }
    }
}
