using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/auth")]
    public class ContratistaAuthController : ControllerBase
    {
        private readonly IContratistaAuthService _service;
        private readonly ILogger<ContratistaAuthController> _logger;

        public ContratistaAuthController(IContratistaAuthService service, ILogger<ContratistaAuthController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] ContratistaLoginDto dto)
        {
            try { return Ok(await _service.LoginAsync(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaAuthController.Login"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("empresas")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEmpresas()
        {
            try { return Ok(await _service.GetEmpresasParaLoginAsync()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaAuthController.GetEmpresas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("activar")]
        [AllowAnonymous]
        public async Task<IActionResult> Activar([FromBody] ActivarCuentaDto dto)
        {
            try { return Ok(await _service.ActivarCuentaAsync(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaAuthController.Activar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("solicitar-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> SolicitarReset([FromBody] SolicitarResetDto dto)
        {
            try
            {
                await _service.SolicitarResetPasswordAsync(dto);
                return Ok(new { message = "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaAuthController.SolicitarReset"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                await _service.ResetPasswordAsync(dto);
                return Ok(new { message = "Contraseña restablecida correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaAuthController.ResetPassword"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("validar-migracion")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarMigracion([FromBody] ValidarMigracionDto dto)
        {
            try { return Ok(await _service.ValidarMigracionAsync(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaAuthController.ValidarMigracion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("activar-migracion")]
        [AllowAnonymous]
        public async Task<IActionResult> ActivarMigracion([FromBody] ActivarMigracionDto dto)
        {
            try
            {
                await _service.ActivarMigracionAsync(dto);
                return Ok(new { message = "Cuenta activada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaAuthController.ActivarMigracion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("cambiar-password")]
        [Authorize(Roles = Roles.Contratista)]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            try
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(claim, out var userId))
                    return StatusCode(401, new { message = "Token inválido." });

                await _service.CambiarPasswordAsync(userId, dto);
                return Ok(new { message = "Contraseña actualizada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaAuthController.CambiarPassword"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

    }
}
