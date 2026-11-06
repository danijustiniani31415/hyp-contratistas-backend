using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.ComprasModule.Presentation
{
    /// <summary>Comprobantes de compra leídos con IA: subir → leer → confirmar → guardar.</summary>
    [ApiController]
    [Route("api/v1/compras/comprobantes")]
    [Authorize]
    public class ComprobantesController : ControllerBase
    {
        private readonly IComprobanteCompraService _service;

        public ComprobantesController(IComprobanteCompraService service)
        {
            _service = service;
        }

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        /// <summary>Lee el archivo con IA y devuelve lo leído + avisos. No guarda nada todavía.</summary>
        [HttpPost("leer")]
        [RequestSizeLimit(16 * 1024 * 1024)]
        public async Task<IActionResult> Leer(IFormFile archivo)
        {
            if (!User.HasLbPermiso("COMPRA_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para cargar comprobantes de compra." });
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Adjunta el comprobante (PDF o foto)." });
            try
            {
                using var stream = archivo.OpenReadStream();
                return Ok(await _service.Leer(stream, archivo.FileName, archivo.ContentType, CurrentUsuarioSistemaId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Guarda el comprobante ya confirmado y aplica los ingresos al kardex.</summary>
        [HttpPost]
        public async Task<IActionResult> Guardar([FromBody] ComprobanteGuardarDto dto)
        {
            if (!User.HasLbPermiso("COMPRA_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para registrar comprobantes de compra." });
            try
            {
                var scope = User.GetProyectosPermitidos("COMPRA_RECIBIR");
                return Ok(await _service.Guardar(dto, CurrentUsuarioSistemaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
