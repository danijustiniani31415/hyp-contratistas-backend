using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/configuracion/carpeta-adjuntos")]
    [Authorize]
    public class CarpetaAdjuntosController : ControllerBase
    {
        private readonly ICarpetaAdjuntosService _service;
        private readonly ILogger<CarpetaAdjuntosController> _logger;

        public CarpetaAdjuntosController(ICarpetaAdjuntosService service, ILogger<CarpetaAdjuntosController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Carpeta única configurada para guardar los adjuntos (null si aún no se configuró).</summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                if (GetUserId() == null) return Unauthorized(new { message = "Inicie sesión" });
                return Ok(await _service.GetSingleton());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CarpetaAdjuntosController.Get");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Configura/actualiza la carpeta única: recibe el link, lo detecta y lo guarda.</summary>
        [HttpPut]
        public async Task<IActionResult> Save([FromBody] GaAdjuntoFolderSaveDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión" });

                return Ok(await _service.Save(dto, userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CarpetaAdjuntosController.Save");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : null;
        }
    }
}
