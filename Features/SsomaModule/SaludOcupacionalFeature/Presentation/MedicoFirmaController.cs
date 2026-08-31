using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    /// <summary>
    /// Configuración del PIN de firma del médico ocupacional (SSO-FO-149). Separado de
    /// <see cref="CatalogosController"/> a propósito: ese controller exige el rol de
    /// administrador SSOMA para todo, pero el PIN lo define el propio médico sobre su
    /// registro — solo necesita estar autenticado, la coincidencia de email la valida
    /// el servicio.
    /// </summary>
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/catalogos")]
    [Authorize]
    public class MedicoFirmaController : ControllerBase
    {
        private readonly ICatalogosService _service;
        private readonly ILogger<MedicoFirmaController> _logger;

        public MedicoFirmaController(ICatalogosService service, ILogger<MedicoFirmaController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("medicos/{id:int}/pin-firma")]
        public async Task<IActionResult> SetPinFirma(int id, [FromBody] PinFirmaSetDto dto)
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                await _service.SetPinFirmaAsync(id, dto.Pin, email);
                return Ok(new { message = "PIN de firma configurado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error configurando PIN de firma"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("medicos/{id:int}/firma-digital")]
        public async Task<IActionResult> SetFirmaDigital(int id, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new AbrilException("El archivo de firma es obligatorio.", 400);

                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                using var stream = file.OpenReadStream();
                var url = await _service.SetFirmaDigitalAsync(id, stream, file.FileName, email);
                return Ok(new { url, message = "Firma digital registrada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error registrando firma digital"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("medicos/{id:int}/autorizacion-firma/documento")]
        public async Task<IActionResult> SetAutorizacionFirmada(int id, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new AbrilException("El archivo escaneado es obligatorio.", 400);

                var email = User.FindFirst(ClaimTypes.Email)?.Value;
                using var stream = file.OpenReadStream();
                var url = await _service.SetAutorizacionFirmadaAsync(id, stream, file.FileName, email);
                return Ok(new { url, message = "Autorización de firma escaneada subida correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error subiendo autorización de firma escaneada"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // Movido desde CatalogosController: ese controller exige rol de administrador SSOMA
        // para todo, pero el propio médico (rol "Médico Ocupacional", sin ese permiso) también
        // necesita poder descargar su propia autorización de firma.
        [HttpGet("medicos/{id:int}/autorizacion-firma/pdf")]
        public async Task<IActionResult> GetAutorizacionFirmaPdf(int id)
        {
            try
            {
                var bytes = await _service.GenerarAutorizacionFirmaPdfAsync(id);
                return File(bytes, "application/pdf", $"Autorizacion_Firma_Medico_{id}.pdf");
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error generando PDF de autorización de firma"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
