using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public LbAuthController(ILbAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
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
        /// Bootstrap — crea el primer usuario (GERENTE_GENERAL). Solo funciona si la tabla
        /// lb_usuario_sistema está vacía. TODO: quitar o proteger detrás de un flag antes de
        /// desplegar a un ambiente compartido.
        /// </summary>
        [HttpPost("seed-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedAdmin([FromBody] LbLoginRequestDto request)
        {
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
    }
}
