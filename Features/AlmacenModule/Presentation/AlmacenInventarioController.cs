using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Services;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.AlmacenModule.Presentation
{
    /// <summary>Kardex valorizado y toma de inventario físico. Ver AlmacenInventarioService.</summary>
    [ApiController]
    [Route("api/v1/almacen")]
    [Authorize]
    public class AlmacenInventarioController : ControllerBase
    {
        private readonly AlmacenInventarioService _service;
        private readonly IDbContextFactory<AppDbContext> _factory;
        public AlmacenInventarioController(AlmacenInventarioService service, IDbContextFactory<AppDbContext> factory) { _service = service; _factory = factory; }

        private Task<HashSet<int>?> Alcance() => AlmacenAlcance.Resolver(_factory, User, "ALMACEN_VER", "ALMACEN_AJUSTAR");

        private long? UsuarioId => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        private async Task<IActionResult> Run<T>(Func<Task<T>> f)
        {
            try { return Ok(await f()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private IActionResult? SinPermiso() =>
            User.HasLbPermiso("ALMACEN_AJUSTAR") ? null : StatusCode(403, new { message = "No tienes permiso para ajustar el almacén." });

        [HttpGet("kardex")]
        public Task<IActionResult> Kardex([FromQuery] int almacenId, [FromQuery] long productoId, [FromQuery] string? talla, [FromQuery] string? color,
            [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta) =>
            Run(async () => await _service.Kardex(almacenId, productoId, talla ?? "", color ?? "", desde, hasta, await Alcance()));

        [HttpGet("inventarios")]
        public Task<IActionResult> Listar([FromQuery] int? almacenId) => Run(async () => await _service.Listar(almacenId, await Alcance()));

        [HttpGet("inventarios/{id:long}")]
        public Task<IActionResult> Obtener(long id) => Run(async () => await _service.Obtener(id, await Alcance()));

        [HttpPost("inventarios")]
        public async Task<IActionResult> Crear([FromBody] InventarioCrearDto dto) =>
            SinPermiso() ?? await Run(async () => await _service.Crear(dto, UsuarioId, await Alcance()));

        [HttpPut("inventarios/{id:long}/conteo")]
        public async Task<IActionResult> Conteo(long id, [FromBody] List<InventarioConteoItemDto> items) =>
            SinPermiso() ?? await Run(async () => await _service.GuardarConteo(id, items, await Alcance()));

        [HttpPost("inventarios/{id:long}/items")]
        public async Task<IActionResult> AgregarItem(long id, [FromBody] InventarioAgregarItemDto dto) =>
            SinPermiso() ?? await Run(async () => await _service.AgregarItem(id, dto, await Alcance()));

        [HttpPost("inventarios/{id:long}/cerrar")]
        public async Task<IActionResult> Cerrar(long id) =>
            SinPermiso() ?? await Run(async () => await _service.Cerrar(id, UsuarioId, User.HasLbPermiso("INVENTARIO_APROBAR"), await Alcance()));

        private IActionResult? SinPermisoAprobar() =>
            User.HasLbPermiso("INVENTARIO_APROBAR") ? null : StatusCode(403, new { message = "Solo gerencia puede aprobar o rechazar ajustes de inventario." });

        [HttpPost("inventarios/{id:long}/aprobar")]
        public async Task<IActionResult> Aprobar(long id, [FromBody] InventarioDecisionDto? dto) =>
            SinPermisoAprobar() ?? await Run(async () => await _service.Aprobar(id, UsuarioId, dto?.Motivo, await Alcance()));

        [HttpPost("inventarios/{id:long}/rechazar")]
        public async Task<IActionResult> Rechazar(long id, [FromBody] InventarioDecisionDto dto) =>
            SinPermisoAprobar() ?? await Run(async () => await _service.Rechazar(id, UsuarioId, dto.Motivo, await Alcance()));

        /// <summary>Monto desde el cual un ajuste de inventario exige aprobación. Lo lee cualquiera que vea el almacén; lo cambia quien aprueba.</summary>
        [HttpGet("inventarios/configuracion")]
        public Task<IActionResult> Umbral() => Run(async () => new InventarioUmbralDto { Umbral = await _service.ObtenerUmbral() });

        [HttpPut("inventarios/configuracion")]
        public async Task<IActionResult> GuardarUmbral([FromBody] InventarioUmbralDto dto) =>
            SinPermisoAprobar() ?? await Run(async () => new InventarioUmbralDto { Umbral = await _service.GuardarUmbral(dto.Umbral, UsuarioId) });

        [HttpPost("inventarios/{id:long}/anular")]
        public async Task<IActionResult> Anular(long id) =>
            SinPermiso() ?? await Run(async () => { await _service.Anular(id, await Alcance()); return new { message = "Inventario anulado." }; });
    }
}
