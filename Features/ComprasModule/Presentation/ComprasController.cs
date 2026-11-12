using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Application.Services;
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
        private readonly CompraControlService _control;

        public ComprasController(IComprasService service, CompraControlService control)
        {
            _service = service;
            _control = control;
        }

        private static readonly string[] PermisosDeLectura = { "COMPRA_CREAR", "COMPRA_RECIBIR", "PAGO_PROVEEDOR_REGISTRAR" };

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

        [HttpPost("ordenes/desde-pedidos")]
        public async Task<IActionResult> GenerarDesdePedidos([FromBody] GenerarOrdenCompraDesdePedidosDto dto)
        {
            if (!User.HasLbPermiso("COMPRA_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para crear órdenes de compra." });
            try { return Ok(await _service.GenerarDesdePedidos(dto, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("ordenes")]
        public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // Unión con COMPRA_RECIBIR: quien solo recibe mercadería (sin poder crear
                // órdenes) igual necesita verlas en la lista para saber qué recibir.
                var scope = User.GetProyectosPermitidosUnion("COMPRA_CREAR", "COMPRA_RECIBIR");
                return Ok(await _service.List(search, estado, scope.EsGlobal ? null : scope.ProyectoIds, page, pageSize));
            }
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
            try
            {
                var scope = User.GetProyectosPermitidos("COMPRA_RECIBIR");
                return Ok(await _service.RecibirItem(id, itemId, dto, CurrentUsuarioSistemaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>El proveedor no enviará el saldo pendiente: pasa a faltante y la orden se cierra.</summary>
        [HttpPost("ordenes/{id:long}/cerrar-saldo")]
        public async Task<IActionResult> CerrarSaldo(long id, [FromBody] CerrarSaldoDto dto)
        {
            if (!User.HasLbPermiso("COMPRA_RECIBIR"))
                return StatusCode(403, new { message = "No tienes permiso para cerrar el saldo de una compra." });
            try
            {
                var scope = User.GetProyectosPermitidos("COMPRA_RECIBIR");
                return Ok(await _service.CerrarSaldo(id, dto, CurrentUsuarioSistemaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Diferencias de compras (dañado, faltante, sobrante), abiertas o resueltas.</summary>
        [HttpGet("diferencias")]
        public async Task<IActionResult> ListarDiferencias([FromQuery] string? estado, [FromQuery] long? ordenId)
        {
            if (!PermisosDeLectura.Any(User.HasLbPermiso))
                return StatusCode(403, new { message = "No tienes permiso para ver las diferencias de compras." });
            try
            {
                var scope = User.GetProyectosPermitidosUnion(PermisosDeLectura);
                return Ok(await _control.Listar(estado, ordenId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("diferencias/{id:long}/resolver")]
        public async Task<IActionResult> ResolverDiferencia(long id, [FromBody] CompraDiferenciaResolverDto dto)
        {
            if (!User.HasLbPermiso("COMPRA_RECIBIR"))
                return StatusCode(403, new { message = "No tienes permiso para resolver diferencias de compras." });
            try
            {
                var scope = User.GetProyectosPermitidosUnion(PermisosDeLectura);
                return Ok(await _control.Resolver(id, dto, CurrentUsuarioSistemaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("ordenes/{id:long}/items/{itemId:long}/devolver")]
        public async Task<IActionResult> DevolverItem(long id, long itemId, [FromBody] DevolverItemDto dto)
        {
            if (!User.HasLbPermiso("COMPRA_RECIBIR"))
                return StatusCode(403, new { message = "No tienes permiso para devolver mercadería." });
            try
            {
                var scope = User.GetProyectosPermitidos("COMPRA_RECIBIR");
                return Ok(await _service.DevolverItem(id, itemId, dto, CurrentUsuarioSistemaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("ordenes/{id:long}/cancelar")]
        public async Task<IActionResult> Cancelar(long id)
        {
            if (!User.HasLbPermiso("COMPRA_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para cancelar órdenes de compra." });
            try
            {
                var scope = User.GetProyectosPermitidos("COMPRA_CREAR");
                return Ok(await _service.Cancelar(id, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
