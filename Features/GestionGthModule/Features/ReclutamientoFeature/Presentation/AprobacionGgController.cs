using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Presentation
{
    /// <summary>
    /// Aprobación de una solicitud de personal. Dos caras, ambas autenticadas:
    ///   • Gerencia: pantalla «Aprobaciones» (bandeja + historial), detalle y decisión.
    ///   • Solicitante: reenviar el correo de su propia solicitud.
    /// Los antiguos endpoints públicos por token ya no existen: el gerente entra desde el correo a
    /// la pantalla y, si no tiene sesión, el login lo devuelve a esa misma URL.
    ///
    /// El rol (<c>role_feature</c>) abre la pantalla; qué solicitudes se ven y con qué poder se
    /// decide lo resuelve el servicio desde la CATEGORÍA de la ficha de trabajador del usuario. El
    /// alcance tiene dos ejes que se aplican juntos:
    ///   • por ÁREA: Gerente General y GTH ven toda la empresa; el gerente de área, su nodo hacia
    ///     abajo; cualquier otra categoría, nada.
    ///   • por TIPO de vacante: el Gerente General solo las NUEVAS y las FFT; el gerente del área y
    ///     GTH solo los REEMPLAZOS. Es el corte de <c>RutaAprobacion</c>, y recorta lo que se
    ///     DEVUELVE (códigos, conteos, casillas y datos de cada vacante), no solo lo que se pinta.
    /// Por eso todos los endpoints de gerencia mandan el id del usuario autenticado.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/gestion-gth/aprobacion-gerencia")]
    public class AprobacionGgController : ControllerBase
    {
        private readonly IAprobacionGgService _service;
        private readonly ILogger<AprobacionGgController> _logger;

        public AprobacionGgController(IAprobacionGgService service, ILogger<AprobacionGgController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Id del usuario autenticado (claim NameIdentifier del JWT interno).</summary>
        private int? UserId =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;

        // ── Pantalla «Aprobaciones» (Gerencia) ─────────────────────────────────

        /// <summary>
        /// Pantalla completa en una sola petición: el nivel del usuario, las tarjetas de resumen y
        /// las solicitudes que alcanza (pendientes de su decisión e historial), cada una recortada a
        /// las vacantes de su ruta.
        /// </summary>
        [HttpGet("bandeja")]
        public async Task<IActionResult> GetBandeja()
        {
            try
            {
                return Ok(await _service.GetBandeja(UserId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AprobacionGgController.GetBandeja");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Detalle de una solicitud a decidir: cabecera + las vacantes de la ruta del usuario + las
        /// casillas de decisión. Si su nivel ya decidió, la respuesta lo indica y el modal se muestra
        /// en modo lectura (historial). 403 si la solicitud es de otra área o si no le toca decidir
        /// ninguna de sus vacantes (el enlace de un correo de la otra ruta).
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetalle(int id)
        {
            try
            {
                return Ok(await _service.GetDetalle(id, UserId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AprobacionGgController.GetDetalle");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Registra la decisión del usuario (aprobar todas, algunas o rechazar todas) en la casilla
        /// de SU nivel — el nivel no viaja en el payload, lo resuelve el backend. Si decide
        /// Gerencia General, las vacantes aprobadas pasan a VALIDACION_GTH y se notifican a GTH y
        /// las rechazadas quedan en RECHAZADO_GG; el visto bueno del gerente del área solo queda
        /// registrado. Cada nivel decide una sola vez.
        /// </summary>
        [HttpPost("{id:int}/decision")]
        public async Task<IActionResult> RegistrarDecision(int id, [FromBody] AprobacionGgDecisionDto dto)
        {
            try
            {
                return Ok(await _service.RegistrarDecision(id, dto, UserId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AprobacionGgController.RegistrarDecision");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Registra la MISMA decisión sobre varias solicitudes seleccionadas en la lista: se aprueban
        /// (o se rechazan) TODAS las vacantes de cada una, en la casilla del nivel del usuario —que,
        /// como en la decisión de una sola, resuelve el servicio y no viaja en el payload. Si decide
        /// Gerencia General, cada solicitud aprobada pasa a VALIDACION_GTH y se notifica a GTH y a TI.
        ///
        /// Devuelve 200 aunque alguna solicitud no se haya podido decidir (ya cerrada, fuera de
        /// alcance, dada de baja): esas vienen en <c>omitidas</c> con su motivo, para no tumbar el
        /// resto del lote por una.
        /// </summary>
        [HttpPost("decision-masiva")]
        public async Task<IActionResult> RegistrarDecisionMasiva([FromBody] AprobacionGgDecisionMasivaDto dto)
        {
            try
            {
                return Ok(await _service.RegistrarDecisionMasiva(dto, UserId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AprobacionGgController.RegistrarDecisionMasiva");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Solicitante ────────────────────────────────────────────────────────

        /// <summary>
        /// Reenvía el correo de aprobación a Gerencia General (para cuando el primer envío falló o
        /// se corrigieron los destinatarios). Solo el solicitante dueño de la solicitud, y solo
        /// mientras Gerencia no haya decidido. El enlace no cambia.
        /// </summary>
        [HttpPost("requerimiento/{id:int}/reenviar")]
        public async Task<IActionResult> Reenviar(int id)
        {
            try
            {
                return Ok(await _service.Reenviar(id, UserId));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AprobacionGgController.Reenviar");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
