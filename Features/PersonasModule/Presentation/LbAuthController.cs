using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Abril_Backend.Features.PersonasModule.Presentation
{
    /// <summary>
    /// Login de HP Constructores / Las Bravas contra el modelo lb_usuario_sistema +
    /// lb_usuario_asignacion — independiente del login viejo de Abril
    /// (Controllers/AuthController.cs), que sigue escrito contra el schema de Abril
    /// (feature/role_feature/user_role/workers/person/puesto) y no aplica acá.
    /// </summary>
    [ApiController]
    [Route("api/v1/lb-auth")]
    public class LbAuthController : ControllerBase
    {
        private readonly ILbAuthService _authService;
        private readonly IWebHostEnvironment _environment;

        public LbAuthController(ILbAuthService authService, IWebHostEnvironment environment)
        {
            _authService = authService;
            _environment = environment;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("lb-auth")]
        public async Task<IActionResult> Login([FromBody] LbLoginRequestDto request)
        {
            try
            {
                return Ok(await _authService.Login(request));
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

        /// <summary>
        /// Bootstrap — crea el primer usuario (GERENTE_GENERAL). Solo existe en Development
        /// (404 en cualquier otro ambiente, ni siquiera revela que el endpoint existe) — además
        /// de que solo funciona si lb_usuario_sistema está vacía. El primer admin de un ambiente
        /// compartido/producción se crea corriendo el backend en Development apuntando a esa
        /// base, o con un INSERT SQL directo — nunca dejando este endpoint alcanzable ahí.
        /// </summary>
        [HttpPost("seed-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedAdmin([FromBody] LbLoginRequestDto request)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            try
            {
                return Ok(await _authService.SeedAdmin(request));
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

        [HttpPost("solicitar-reset")]
        [AllowAnonymous]
        [EnableRateLimiting("lb-auth")]
        public async Task<IActionResult> SolicitarReset([FromBody] LbSolicitarResetDto request)
        {
            try
            {
                await _authService.SolicitarReset(request);
                // Mismo mensaje exista o no el email — evita revelar qué correos están registrados.
                return Ok(new { message = "Si el correo está registrado, te enviamos un enlace para restablecer tu contraseña." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("lb-auth")]
        public async Task<IActionResult> ResetPassword([FromBody] LbResetPasswordDto request)
        {
            try
            {
                await _authService.ResetPassword(request);
                return Ok(new { message = "Contraseña actualizada." });
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
