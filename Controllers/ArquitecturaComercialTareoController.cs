using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Application.DTOs.ArquitecturaComercial;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Application.Interfaces;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/v1/arquitectura-comercial/tareo")]
    public class ArquitecturaComercialTareoController : ControllerBase
    {
        private readonly IArquitecturaComercialTareoService _service;
        private readonly ILogger<ArquitecturaComercialTareoController> _logger;

        public ArquitecturaComercialTareoController(
            IArquitecturaComercialTareoService service,
            ILogger<ArquitecturaComercialTareoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // El claim NameIdentifier lleva el User.Id (ver JwtService.GenerateToken), NO el Worker.Id
        // — son secuencias distintas. Para endpoints de auto-servicio (enrolar/marcar/mi-tareo) hay
        // que resolver el Worker.Id vía Person, igual que MiSaludRepository.ResolverWorkerIdAsync.
        // "Revisar" es la excepción: revisado_por referencia app_user(user_id), así que ahí SÍ se
        // usa el User.Id crudo (es quien revisó, no un trabajador).
        private int? CurrentUserId()
            => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;

        private bool EsGestorAc()
            => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Contains(Roles.GestorArquitecturaComercial);

        // ── Gestión de permisos (coordinador) ───────────────────────────────────
        // El correo corporativo de obra se comparte entre varios trabajadores, así que el
        // enrolamiento facial NO puede ser autoservicio: lo hace el coordinador, ligando
        // nombre+foto por cada uno de los ~40 obreros de Arquitectura Comercial.

        [HttpGet("enrolamiento/trabajadores")]
        public async Task<IActionResult> GetTrabajadoresParaEnrolar()
        {
            if (!EsGestorAc()) return Forbid();

            try
            {
                return Ok(await _service.GetTrabajadoresParaEnrolar());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando trabajadores para enrolamiento de tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("proyectos-geo")]
        public async Task<IActionResult> GetProyectosGeo()
        {
            if (!EsGestorAc()) return Forbid();

            try
            {
                return Ok(await _service.GetProyectosGeo());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando geolocalización de proyectos");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("proyectos-geo/{projectId:int}")]
        public async Task<IActionResult> SetProyectoGeo(int projectId, [FromBody] TareoProyectoGeoUpdateDTO body)
        {
            if (!EsGestorAc()) return Forbid();

            try
            {
                await _service.SetProyectoGeo(projectId, body);
                return Ok(new { message = "Geolocalización actualizada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando geolocalización de proyecto");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("autorizacion/{workerId:int}/pdf")]
        public async Task<IActionResult> GetAutorizacionPdf(int workerId)
        {
            if (!EsGestorAc()) return Forbid();

            try
            {
                var bytes = await _service.GenerarAutorizacionPdf(workerId);
                return File(bytes, "application/pdf", $"SSO-FO-150_{workerId}.pdf");
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando SSO-FO-150");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("autorizacion/{workerId:int}/documento")]
        public async Task<IActionResult> SubirAutorizacion(int workerId, [FromForm] IFormFile file)
        {
            if (!EsGestorAc()) return Forbid();

            try
            {
                if (file == null || file.Length == 0)
                    throw new AbrilException("El archivo escaneado es obligatorio.", 400);

                var userId = CurrentUserId();
                using var stream = file.OpenReadStream();
                var url = await _service.SubirAutorizacion(workerId, stream, file.FileName, userId);
                return Ok(new { url, message = "Autorización subida correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo autorización de tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("enrolamiento/trabajadores/{workerId:int}")]
        public async Task<IActionResult> EnrolarTrabajador(int workerId, [FromBody] TareoEnrolamientoRequestDTO body)
        {
            if (!EsGestorAc()) return Forbid();

            try
            {
                await _service.EnrolarWorker(workerId, body);
                return Ok(new { message = "Enrolamiento registrado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en enrolamiento de tareo (coordinador)");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Enrolamiento asistido (cuenta compartida de campo) ──────────────────
        // Autoservicio para el correo corporativo compartido (operarios): el trabajador elige su
        // nombre de una lista (solo quienes ya tienen el SSO-FO-150 subido) y se toma su propia
        // foto. A diferencia de "Gestión de permisos" (EsGestorAc), esto se autoriza por featureKey
        // — no da acceso a subir/descargar autorizaciones ni a geolocalización de proyectos.

        [HttpGet("enrolamiento/disponibles")]
        [RequireFeature("arquitectura-comercial.tareo.enrolamiento")]
        public async Task<IActionResult> GetTrabajadoresDisponiblesParaEnrolar()
        {
            try
            {
                return Ok(await _service.GetTrabajadoresDisponiblesParaEnrolar());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando trabajadores disponibles para enrolamiento asistido");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("enrolamiento/disponibles/{workerId:int}")]
        [RequireFeature("arquitectura-comercial.tareo.enrolamiento")]
        public async Task<IActionResult> EnrolarDisponible(int workerId, [FromBody] TareoEnrolamientoRequestDTO body)
        {
            try
            {
                // EnrolarWorker ya exige que el SSO-FO-150 esté subido (ver
                // ArquitecturaComercialTareoService.EnrolarWorker) — misma garantía server-side que
                // protege a EnrolarTrabajador del coordinador.
                await _service.EnrolarWorker(workerId, body);
                return Ok(new { message = "Enrolamiento registrado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en enrolamiento asistido de tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Identificación facial (usada por el flujo de marcado) ───────────────

        [HttpPost("identificar")]
        public async Task<IActionResult> Identificar([FromBody] TareoIdentificarRequestDTO body)
        {
            try
            {
                return Ok(await _service.Identificar(body.Embedding));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identificando trabajador por rostro");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("enrolamiento/estado")]
        public async Task<IActionResult> GetEnrolamientoEstado()
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var workerId = await _service.ResolverWorkerId(userId.Value);
                return Ok(await _service.GetEnrolamientoEstado(workerId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando estado de enrolamiento de tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("enrolamiento")]
        public async Task<IActionResult> Enrolar([FromBody] TareoEnrolamientoRequestDTO body)
        {
            var userId = CurrentUserId();
            if (userId == null) return Unauthorized();

            try
            {
                var workerId = await _service.ResolverWorkerId(userId.Value);
                await _service.EnrolarWorker(workerId, body);
                return Ok(new { message = "Enrolamiento registrado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en enrolamiento de tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("marcar")]
        public async Task<IActionResult> Marcar([FromBody] TareoMarcarRequestDTO body, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKeyHeader)
        {
            // No se resuelve workerId desde el login: el correo corporativo es compartido entre
            // varios trabajadores, así que la identidad de quien marca sale SIEMPRE del
            // reconocimiento facial (ver IArquitecturaComercialTareoService.Marcar).
            if (!Guid.TryParse(idempotencyKeyHeader, out var idempotencyKey))
                return BadRequest(new { message = "Falta el header Idempotency-Key (debe ser un GUID generado por el cliente)." });

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _service.Marcar(idempotencyKey, body, ip);
                return result.YaExistia ? Ok(result) : StatusCode(201, result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("mi-tareo/hoy")]
        public async Task<IActionResult> GetMiTareoHoy([FromQuery] int workerId)
        {
            // workerId llega del resultado de POST /identificar (nunca del login — ver Marcar).
            try
            {
                return Ok(await _service.GetMiTareoHoy(workerId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consultando tareo del día");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("registros")]
        public async Task<IActionResult> GetRegistros([FromQuery] TareoFiltroDTO filtro)
        {
            try
            {
                return Ok(await _service.GetRegistros(filtro));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listando registros de tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("registros/{id}/revisar")]
        public async Task<IActionResult> Revisar(int id, [FromBody] TareoRevisarRequestDTO body)
        {
            var revisorId = CurrentUserId();
            if (revisorId == null) return Unauthorized();

            try
            {
                var ok = await _service.Revisar(id, revisorId.Value, body);
                if (!ok) return NotFound(new { message = "Registro de tareo no encontrado." });
                return Ok(new { message = "Registro actualizado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revisando registro de tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpGet("reporte-semanal")]
        public async Task<IActionResult> GetReporteSemanal([FromQuery] int? proyectoId, [FromQuery] DateOnly semana)
        {
            try
            {
                return Ok(await _service.GetReporteSemanal(proyectoId, semana));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte semanal de tareo");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
