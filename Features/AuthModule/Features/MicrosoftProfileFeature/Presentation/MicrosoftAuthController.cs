using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.AuthModule.MicrosoftProfile.Presentation
{
    [ApiController]
    [Route("api/v1/microsoft")]
    [Authorize(AuthenticationSchemes = "AzureAd")]
    public class MicrosoftAuthController : ControllerBase
    {
        private readonly IMicrosoftProfileService _service;

        public MicrosoftAuthController(IMicrosoftProfileService service)
        {
            _service = service;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                var profile = await _service.GetProfile(token);

                if (profile is null)
                    return StatusCode(502, new { message = "No se pudo obtener el perfil desde Microsoft Graph." });

                return Ok(profile);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
