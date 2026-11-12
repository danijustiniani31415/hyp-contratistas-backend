using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Services;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.ComprasModule.Presentation
{
    /// <summary>Cotizaciones comparativas: se piden precios a varios proveedores, se comparan y se adjudica generando las OC.</summary>
    [ApiController]
    [Route("api/v1/cotizaciones")]
    [Authorize]
    public class CotizacionesController : ControllerBase
    {
        private readonly CotizacionService _service;
        public CotizacionesController(CotizacionService service) => _service = service;

        private long UsuarioId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : throw new AbrilException("Token inválido.", 401);

        private IActionResult? SinPermiso() =>
            User.HasLbPermiso("COMPRA_CREAR") ? null : StatusCode(403, new { message = "No tienes permiso para manejar cotizaciones." });

        private async Task<IActionResult> Ejecutar(Func<Task<object>> accion)
        {
            if (SinPermiso() is { } no) return no;
            try { return Ok(await accion()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public Task<IActionResult> Listar([FromQuery] string? estado) => Ejecutar(async () => await _service.Listar(estado));

        [HttpGet("{id:long}")]
        public Task<IActionResult> Detalle(long id) => Ejecutar(async () => await _service.Detalle(id));

        [HttpPost]
        public Task<IActionResult> Crear([FromBody] CotizacionCrearDto dto) => Ejecutar(async () => await _service.Crear(dto, UsuarioId));

        [HttpPost("{id:long}/cotizaciones")]
        public Task<IActionResult> Agregar(long id, [FromBody] CotizacionAgregarDto dto) => Ejecutar(async () => await _service.AgregarCotizacion(id, dto, UsuarioId));

        [HttpDelete("{id:long}/cotizaciones/{cotizacionId:long}")]
        public Task<IActionResult> Eliminar(long id, long cotizacionId) => Ejecutar(async () => await _service.EliminarCotizacion(id, cotizacionId));

        [HttpPost("{id:long}/adjudicar")]
        public Task<IActionResult> Adjudicar(long id, [FromBody] CotizacionAdjudicarDto dto) => Ejecutar(async () => await _service.Adjudicar(id, dto, UsuarioId));

        [HttpPost("{id:long}/cancelar")]
        public Task<IActionResult> Cancelar(long id) => Ejecutar(async () => await _service.Cancelar(id));
    }
}
