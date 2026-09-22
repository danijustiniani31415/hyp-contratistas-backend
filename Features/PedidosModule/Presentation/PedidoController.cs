using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PedidosModule.Application.Dtos;
using Abril_Backend.Features.PedidosModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.PedidosModule.Presentation
{
    /// <summary>
    /// Pedidos — Fase 2 (CONTEXT_LOGISTICA.md sección 3, punto 4). Primer flujo multi-rol real
    /// de Las Bravas: cada acción exige el permiso correspondiente (lb_permisos del JWT), no el
    /// código de rol — ver LbClaimsExtensions.
    /// </summary>
    [ApiController]
    [Route("api/v1/pedidos")]
    [Authorize]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _service;

        public PedidoController(IPedidoService service)
        {
            _service = service;
        }

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] PedidoCreateDto dto)
        {
            if (!User.HasLbPermiso("PEDIDO_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para crear pedidos." });
            try { return Ok(await _service.Crear(dto, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? estado, [FromQuery] int? proyectoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var soloPropios = !User.HasLbPermiso("PEDIDO_VER_TODOS");
                var scope = User.GetProyectosPermitidos("PEDIDO_VER_TODOS");
                var proyectosPermitidos = soloPropios || scope.EsGlobal ? null : scope.ProyectoIds;
                return Ok(await _service.List(estado, proyectoId, soloPropios, proyectosPermitidos, CurrentUsuarioSistemaId, page, pageSize));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try { return Ok(await _service.GetById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("destinatarios")]
        public async Task<IActionResult> GetDestinatarios([FromQuery] int proyectoId)
        {
            try { return Ok(await _service.GetDestinatarios(proyectoId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:long}/visar")]
        public async Task<IActionResult> Visar(long id)
        {
            if (!User.HasLbPermiso("PEDIDO_VISAR"))
                return StatusCode(403, new { message = "No tienes permiso para visar pedidos." });
            try { return Ok(await _service.Visar(id, CurrentUsuarioSistemaId, User.GetProyectosPermitidos("PEDIDO_VISAR"))); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:long}/rechazar-visado")]
        public async Task<IActionResult> RechazarVisado(long id, [FromBody] RechazarPedidoDto dto)
        {
            if (!User.HasLbPermiso("PEDIDO_VISAR"))
                return StatusCode(403, new { message = "No tienes permiso para rechazar pedidos en visado." });
            try { return Ok(await _service.RechazarVisado(id, CurrentUsuarioSistemaId, dto, User.GetProyectosPermitidos("PEDIDO_VISAR"))); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:long}/aprobar")]
        public async Task<IActionResult> Aprobar(long id)
        {
            if (!User.HasLbPermiso("PEDIDO_APROBAR"))
                return StatusCode(403, new { message = "No tienes permiso para aprobar pedidos." });
            try { return Ok(await _service.Aprobar(id, CurrentUsuarioSistemaId, User.GetProyectosPermitidos("PEDIDO_APROBAR"))); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:long}/rechazar")]
        public async Task<IActionResult> Rechazar(long id, [FromBody] RechazarPedidoDto dto)
        {
            if (!User.HasLbPermiso("PEDIDO_APROBAR"))
                return StatusCode(403, new { message = "No tienes permiso para rechazar pedidos." });
            try { return Ok(await _service.Rechazar(id, CurrentUsuarioSistemaId, dto, User.GetProyectosPermitidos("PEDIDO_APROBAR"))); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:long}/entregar")]
        public async Task<IActionResult> Entregar(long id)
        {
            if (!User.HasLbPermiso("PEDIDO_ENTREGAR"))
                return StatusCode(403, new { message = "No tienes permiso para entregar pedidos." });
            try { return Ok(await _service.Entregar(id, CurrentUsuarioSistemaId, User.GetProyectosPermitidos("PEDIDO_ENTREGAR"))); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:long}/cancelar")]
        public async Task<IActionResult> Cancelar(long id)
        {
            try { return Ok(await _service.Cancelar(id, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
