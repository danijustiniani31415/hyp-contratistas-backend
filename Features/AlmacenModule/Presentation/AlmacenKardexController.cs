using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.AlmacenModule.Presentation
{
    /// <summary>Almacén / Kardex multi-almacén — CONTEXT_LOGISTICA.md sección 7 (Fase 1, punto 3).</summary>
    [ApiController]
    [Route("api/v1/almacen")]
    [Authorize]
    public class AlmacenKardexController : ControllerBase
    {
        private readonly IAlmacenKardexService _service;

        public AlmacenKardexController(IAlmacenKardexService service)
        {
            _service = service;
        }

        private long? CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        [HttpGet("stock")]
        public async Task<IActionResult> ListStock([FromQuery] int? almacenId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try { return Ok(await _service.ListStock(almacenId, search, page, pageSize)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("movimientos")]
        public async Task<IActionResult> ListMovimientos([FromQuery] int? almacenId, [FromQuery] long? productoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try { return Ok(await _service.ListMovimientos(almacenId, productoId, page, pageSize)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("movimientos")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] RegistrarMovimientoDto dto)
        {
            try
            {
                await _service.RegistrarMovimiento(dto, CurrentUsuarioSistemaId);
                return Ok(new { message = "Movimiento registrado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("umbrales")]
        public async Task<IActionResult> AjustarUmbrales([FromBody] AjustarUmbralesDto dto)
        {
            try
            {
                await _service.AjustarUmbrales(dto);
                return Ok(new { message = "Umbrales actualizados." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
