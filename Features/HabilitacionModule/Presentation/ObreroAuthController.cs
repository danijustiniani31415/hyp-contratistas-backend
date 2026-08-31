using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Shared.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    /// <summary>
    /// Login del tipo de sesión OBRERO — una cuenta por trabajador de campo, no por
    /// empresa contratista (ver <see cref="ContratistaAuthController"/>).
    /// </summary>
    [ApiController]
    [Route("api/v1/habilitacion/auth-obrero")]
    public class ObreroAuthController : ControllerBase
    {
        private readonly IObreroAuthService _service;
        private readonly ILogger<ObreroAuthController> _logger;

        public ObreroAuthController(IObreroAuthService service, ILogger<ObreroAuthController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] ObreroLoginDto dto)
        {
            try { return Ok(await _service.LoginAsync(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ObreroAuthController.Login"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Crea o resetea la contraseña de un obrero. Requiere estar logueado como staff
        /// con acceso al módulo de Habilitación (mismo feature que administra trabajadores).
        /// </summary>
        [HttpPost("credenciales")]
        [Authorize]
        [RequireFeature("habilitacion.trabajadores")]
        public async Task<IActionResult> SetPassword([FromBody] ObreroSetPasswordDto dto)
        {
            try { await _service.SetPasswordAsync(dto); return Ok(new { message = "Credenciales guardadas correctamente." }); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ObreroAuthController.SetPassword"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("cambiar-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword([FromBody] ObreroCambiarPasswordDto dto)
        {
            try
            {
                if (User.FindFirst("tipo")?.Value != "OBRERO")
                    return StatusCode(403, new { message = "Este endpoint es solo para la sesión de obrero." });

                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(claim, out var userId))
                    return StatusCode(401, new { message = "Token inválido." });

                await _service.CambiarPasswordAsync(userId, dto);
                return Ok(new { message = "Contraseña actualizada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ObreroAuthController.CambiarPassword"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
