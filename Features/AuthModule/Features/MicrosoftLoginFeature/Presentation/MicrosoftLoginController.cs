using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Interfaces;
using Abril_Backend.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.AuthModule.MicrosoftLogin.Presentation
{
    [ApiController]
    [Route("api/v1/microsoft")]
    public class MicrosoftLoginController : ControllerBase
    {
        private readonly IMicrosoftLoginService _service;

        public MicrosoftLoginController(IMicrosoftLoginService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (string.IsNullOrWhiteSpace(token))
                    return Unauthorized(new { message = "Token de Microsoft requerido." });

                var result = await _service.Login(token);
                return Ok(result);
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex) when (DbConnectivity.IsUnavailable(ex, out var detalle))
            {
                return StatusCode(503, new
                {
                    message = "La base de datos no está disponible en este momento (sin conexiones libres). Intenta nuevamente en unos segundos.",
                    detalle
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
