using Abril_Backend.Application.Exceptions;
using Abril_Backend.Shared.Services.Firma.Dtos;
using Abril_Backend.Shared.Services.Firma.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.ConfigurationModule.Features.FirmaPersonalFeature.Presentation
{
    /// <summary>
    /// La firma del usuario que está logueado. Es a propósito el mismo endpoint para todos: una
    /// persona tiene UNA firma (<c>person.signature_*</c>), la registre desde donde la registre
    /// (Contabilidad → Configuración → Firma, o Gestión Administrativa → Configuración → Tu firma),
    /// y esa misma firma es la que se estampa en las facturas, en la carta oferta y en la planilla
    /// de rendición de salidas.
    ///
    /// Sin restricción de rol: cualquier usuario autenticado registra la suya y solo la suya — el
    /// user id sale del token, nunca de la petición.
    ///
    /// Reemplaza al antiguo <c>api/v1/ManagerSignature</c>, que vivía dentro de Contabilidad pese a
    /// no ser de Contabilidad.
    /// </summary>
    [ApiController]
    [Route("api/v1/configuracion/mi-firma")]
    [Authorize]
    public class FirmaPersonalController : ControllerBase
    {
        private readonly IFirmaPersonalService _service;
        private readonly ILogger<FirmaPersonalController> _logger;

        public FirmaPersonalController(IFirmaPersonalService service, ILogger<FirmaPersonalController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>Firma del usuario actual (null si aún no la configuró).</summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null) return Unauthorized(new { message = "Inicie sesión" });

                return Ok(await _service.Get(userId.Value));
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en FirmaPersonalController.Get");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Guarda/actualiza la firma del usuario actual (PNG dibujado en el canvas).</summary>
        [HttpPut]
        public async Task<IActionResult> Save([FromBody] FirmaPersonalSaveDto dto)
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
                _logger.LogError(ex, "Error en FirmaPersonalController.Save");
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
