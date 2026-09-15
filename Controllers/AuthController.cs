using Abril_Backend.Application.Interfaces;
using Abril_Backend.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Application.Exceptions;

namespace Abril_Backend.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(
            IAuthService authService
        )
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            try
            {
                var result = await _authService.Login(dto);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // [REVISADO] Este login legacy de Abril (feature/role_feature/user_role/workers/person/
        // puesto) no aplica a HP Constructores / Las Bravas — este proyecto usa
        // /api/v1/lb-auth/login. Se corta acá con 401 limpio en vez de dejar que
        // _authService.Refresh() ejecute SQL crudo contra tablas que no existen en esta base
        // (relación «feature» no existe) — eso generaba un 500 con stacktrace de Npgsql en cada
        // intento en vez de un rechazo simple.
        [AllowAnonymous]
        [HttpPost("refresh")]
        public IActionResult Refresh(RefreshRequestDTO dto)
        {
            return Unauthorized(new { message = "El login legacy de Abril no aplica en este proyecto. Usa /api/v1/lb-auth/login." });
        }

        [HttpPost("set-password")]
        public async Task<IActionResult> SetPassword(SetPasswordDTO dto)
        {
            try
            {
                await _authService.SetPassword(dto);

                return Ok(new { message = "Cuenta activada correctamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO dto)
        {
            try
            {
                await _authService.ForgotPassword(dto);
                return Ok(new { message = "Si el correo existe, recibirás un enlace para restablecer tu contraseña." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}