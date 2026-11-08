using System.Security.Claims;
using System.Text;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.FacturacionElectronicaModule.Application.Dtos;
using Abril_Backend.Features.FacturacionElectronicaModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.FacturacionElectronicaModule.Presentation
{
    /// <summary>Facturación electrónica SUNAT: facturas, notas de crédito/débito y su baja. Ver DISENO-FACTURACION-SUNAT.md.</summary>
    [ApiController]
    [Route("api/v1/facturacion")]
    [Authorize]
    public class CpeController : ControllerBase
    {
        private readonly ICpeService _service;

        public CpeController(ICpeService service)
        {
            _service = service;
        }

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        private static readonly string[] PermisosDeLectura = { "FACTURACION_VER", "FACTURACION_CREAR", "FACTURACION_EMITIR" };

        private IActionResult? Denegar(params string[] permisos) =>
            permisos.Any(User.HasLbPermiso) ? null : StatusCode(403, new { message = "No tienes permiso para esta operación de facturación." });

        private async Task<IActionResult> Ejecutar<T>(Func<Task<T>> accion)
        {
            try { return Ok(await accion()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? estado, [FromQuery] string? tipo,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Denegar(PermisosDeLectura) ?? await Ejecutar(() => _service.List(search, estado, tipo, page, pageSize));

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get(long id)
            => Denegar(PermisosDeLectura) ?? await Ejecutar(() => _service.GetById(id));

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CpeCreateDto dto)
            => Denegar("FACTURACION_CREAR") ?? await Ejecutar(() => _service.Guardar(null, dto, CurrentUsuarioSistemaId));

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Actualizar(long id, [FromBody] CpeCreateDto dto)
            => Denegar("FACTURACION_CREAR") ?? await Ejecutar(() => _service.Guardar(id, dto, CurrentUsuarioSistemaId));

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Eliminar(long id)
            => Denegar("FACTURACION_CREAR") ?? await Ejecutar(async () => { await _service.Eliminar(id); return new { ok = true }; });

        [HttpPost("{id:long}/emitir")]
        public async Task<IActionResult> Emitir(long id)
            => Denegar("FACTURACION_EMITIR") ?? await Ejecutar(() => _service.Emitir(id, CurrentUsuarioSistemaId));

        [HttpPost("{id:long}/numero-nuevo")]
        public async Task<IActionResult> NumeroNuevo(long id)
            => Denegar("FACTURACION_EMITIR") ?? await Ejecutar(() => _service.PedirNumeroNuevo(id));

        [HttpPost("{id:long}/baja")]
        public async Task<IActionResult> Baja(long id, [FromBody] CpeBajaDto dto)
            => Denegar("FACTURACION_EMITIR") ?? await Ejecutar(() => _service.DarDeBaja(id, dto, CurrentUsuarioSistemaId));

        [HttpPost("{id:long}/consultar-baja")]
        public async Task<IActionResult> ConsultarBaja(long id)
            => Denegar("FACTURACION_EMITIR") ?? await Ejecutar(() => _service.ConsultarBaja(id));

        [HttpGet("{id:long}/xml")]
        public async Task<IActionResult> Xml(long id) => await Archivo(id, () => _service.ObtenerXml(id), "xml");

        [HttpGet("{id:long}/cdr")]
        public async Task<IActionResult> Cdr(long id) => await Archivo(id, () => _service.ObtenerCdr(id), "cdr.xml");

        private async Task<IActionResult> Archivo(long id, Func<Task<string>> leer, string extension)
        {
            var d = Denegar(PermisosDeLectura);
            if (d != null) return d;
            try
            {
                var contenido = await leer();
                return File(Encoding.UTF8.GetBytes(contenido), "application/xml", $"comprobante-{id}.{extension}");
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("series")]
        public async Task<IActionResult> Series()
            => Denegar(PermisosDeLectura) ?? await Ejecutar(() => _service.ListSeries());

        [HttpPut("series")]
        public async Task<IActionResult> GuardarSerie([FromBody] CpeSerieUpsertDto dto)
            => Denegar("FACTURACION_EMITIR") ?? await Ejecutar(() => _service.GuardarSerie(dto));
    }
}
