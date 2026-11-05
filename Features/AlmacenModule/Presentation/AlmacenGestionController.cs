using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.AlmacenModule.Presentation
{
    /// <summary>Datos de cada almacén que SUNAT ve en la guía de remisión (dirección, ubigeo, código de establecimiento).</summary>
    [ApiController]
    [Route("api/v1/almacenes")]
    [Authorize]
    public class AlmacenGestionController : ControllerBase
    {
        private readonly IAlmacenGestionService _service;
        private readonly IAlmacenCargaMasivaService _cargaMasiva;

        public AlmacenGestionController(IAlmacenGestionService service, IAlmacenCargaMasivaService cargaMasiva)
        {
            _service = service;
            _cargaMasiva = cargaMasiva;
        }

        private long? CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        /// <summary>Ingreso masivo de stock (p. ej. desde un Excel), creando en el catálogo los productos que falten.</summary>
        [HttpPost("carga-masiva")]
        public async Task<IActionResult> CargaMasiva([FromBody] CargaMasivaDto dto)
        {
            if (!User.HasLbPermiso("ALMACEN_AJUSTAR"))
                return StatusCode(403, new { message = "No tienes permiso para registrar ingresos de almacén." });
            try { return Ok(await _cargaMasiva.Cargar(dto, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            try { return Ok(await _service.Listar()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] AlmacenActualizarDto dto)
        {
            if (!User.HasLbPermiso("ALMACEN_AJUSTAR"))
                return StatusCode(403, new { message = "No tienes permiso para editar almacenes." });
            try { return Ok(await _service.Actualizar(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
