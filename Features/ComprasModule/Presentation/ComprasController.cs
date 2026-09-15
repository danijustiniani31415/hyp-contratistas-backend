using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.ComprasModule.Presentation
{
    /// <summary>Compras — Fase 3 (CONTEXT_LOGISTICA.md sección 3, punto 7).</summary>
    [ApiController]
    [Route("api/v1/compras")]
    [Authorize]
    public class ComprasController : ControllerBase
    {
        private readonly IComprasService _service;

        public ComprasController(IComprasService service)
        {
            _service = service;
        }

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        [HttpGet("proveedores")]
        public async Task<IActionResult> ListProveedores()
        {
            try { return Ok(await _service.ListProveedores()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("proveedores")]
        public async Task<IActionResult> CrearProveedor([FromBody] ProveedorCreateDto dto)
        {
            if (!User.HasLbPermiso("COMPRA_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para registrar proveedores." });
            try { return Ok(await _service.CrearProveedor(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("ordenes")]
        public async Task<IActionResult> Crear([FromBody] OrdenCompraCreateDto dto)
        {
            if (!User.HasLbPermiso("COMPRA_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para crear órdenes de compra." });
            try { return Ok(await _service.Crear(dto, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("ordenes")]
        public async Task<IActionResult> List([FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try { return Ok(await _service.List(estado, page, pageSize)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("ordenes/{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try { return Ok(await _service.GetById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("ordenes/{id:long}/items/{itemId:long}/recibir")]
        public async Task<IActionResult> RecibirItem(long id, long itemId, [FromBody] RecibirItemDto dto)
        {
            if (!User.HasLbPermiso("COMPRA_RECIBIR"))
                return StatusCode(403, new { message = "No tienes permiso para registrar recepciones." });
            try { return Ok(await _service.RecibirItem(id, itemId, dto, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("ordenes/{id:long}/cancelar")]
        public async Task<IActionResult> Cancelar(long id)
        {
            if (!User.HasLbPermiso("COMPRA_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para cancelar órdenes de compra." });
            try { return Ok(await _service.Cancelar(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
