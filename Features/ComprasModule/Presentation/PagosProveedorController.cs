using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Services;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.ComprasModule.Presentation
{
    /// <summary>Pagos a proveedores (cuentas por pagar). Todo exige PAGO_PROVEEDOR_REGISTRAR, que es opcional por rol.</summary>
    [ApiController]
    [Route("api/v1/pagos-proveedores")]
    [Authorize]
    public class PagosProveedorController : ControllerBase
    {
        private readonly PagosProveedorService _service;

        public PagosProveedorController(PagosProveedorService service) => _service = service;

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        private IActionResult? SinPermiso() =>
            User.HasLbPermiso("PAGO_PROVEEDOR_REGISTRAR") ? null : StatusCode(403, new { message = "No tienes permiso para gestionar pagos a proveedores." });

        private async Task<IActionResult> Run(Func<Task<object>> accion)
        {
            if (SinPermiso() is { } denegado) return denegado;
            try { return Ok(await accion()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public Task<IActionResult> Listar([FromQuery] string? search, [FromQuery] string? estadoPago, [FromQuery] int? proveedorId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
            Run(async () =>
            {
                var (data, total) = await _service.Listar(search, estadoPago, proveedorId, page, pageSize);
                return new { page, pageSize, totalRecords = total, totalPages = (int)Math.Ceiling(total / (double)pageSize), data };
            });

        [HttpGet("resumen")]
        public Task<IActionResult> Resumen() => Run(async () => await _service.Resumen());

        [HttpGet("{comprobanteId:long}")]
        public Task<IActionResult> Obtener(long comprobanteId) => Run(async () => await _service.Obtener(comprobanteId));

        [HttpPost("{comprobanteId:long}/pagos")]
        public Task<IActionResult> Registrar(long comprobanteId, [FromBody] PagoRegistrarDto dto) =>
            Run(async () => await _service.Registrar(comprobanteId, dto, CurrentUsuarioSistemaId));

        [HttpPost("pagos/{pagoId:long}/anular")]
        public Task<IActionResult> Anular(long pagoId, [FromBody] PagoAnularDto dto) =>
            Run(async () => await _service.Anular(pagoId, dto.Motivo, CurrentUsuarioSistemaId));
    }
}
