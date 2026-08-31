using Abril_Backend.Application.Exceptions;
using Abril_Backend.Shared.Services.Notificaciones.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.NotificacionesModule.Presentation
{
    /// <summary>
    /// Campanita de notificaciones del encabezado. Cualquier usuario autenticado accede a SUS
    /// notificaciones (no requiere feature: quien no tiene eventos simplemente no ve ninguna).
    /// </summary>
    [ApiController]
    [Route("api/v1/notificaciones")]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionesService _service;
        private readonly ILogger<NotificacionesController> _logger;

        public NotificacionesController(INotificacionesService service, ILogger<NotificacionesController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        private int? CurrentUserId =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        /// <summary>Contador de no leídas + últimas notificaciones del usuario, en una sola petición.</summary>
        [HttpGet]
        public async Task<IActionResult> GetMisNotificaciones()
        {
            try
            {
                var userId = CurrentUserId;
                if (!userId.HasValue)
                    return Unauthorized(new { message = "No se pudo identificar al usuario." });

                return Ok(await _service.GetMisNotificaciones(userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en NotificacionesController.GetMisNotificaciones");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Marca una notificación como leída (click en el panel: "apaga sus colores").</summary>
        [HttpPatch("{id:int}/leida")]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            try
            {
                var userId = CurrentUserId;
                if (!userId.HasValue)
                    return Unauthorized(new { message = "No se pudo identificar al usuario." });

                await _service.MarcarLeida(id, userId.Value);
                return Ok(new { message = "Notificación marcada como leída." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en NotificacionesController.MarcarLeida");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Marca todas las notificaciones no leídas del usuario como leídas ("Marcar leídas").</summary>
        [HttpPatch("leidas")]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            try
            {
                var userId = CurrentUserId;
                if (!userId.HasValue)
                    return Unauthorized(new { message = "No se pudo identificar al usuario." });

                await _service.MarcarTodasLeidas(userId.Value);
                return Ok(new { message = "Notificaciones marcadas como leídas." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en NotificacionesController.MarcarTodasLeidas");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
