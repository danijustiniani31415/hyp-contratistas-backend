using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CatalogoModule.Application.Dtos;
using Abril_Backend.Features.CatalogoModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Features.PersonasModule;

namespace Abril_Backend.Features.CatalogoModule.Presentation
{
    /// <summary>Catálogo Maestro de Las Bravas — CONTEXT_LOGISTICA.md sección 6 (Fase 1, punto 2).</summary>
    [ApiController]
    [Route("api/v1/catalogo")]
    [Authorize]
    public class CatalogoController : ControllerBase
    {
        private readonly ICatalogoService _service;

        public CatalogoController(ICatalogoService service)
        {
            _service = service;
        }

        [HttpGet("categorias")]
        public async Task<IActionResult> ListCategorias()
        {
            try { return Ok(await _service.ListCategorias()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("categorias")]
        public async Task<IActionResult> CrearCategoria([FromBody] CategoriaCreateDto dto)
        {
            if (!User.HasLbPermiso("CATALOGO_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para administrar el catálogo." });
            try { return Ok(await _service.CrearCategoria(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("tallas")]
        public async Task<IActionResult> ListTallas([FromQuery] string tipo)
        {
            try { return Ok(await _service.ListTallas(tipo)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("productos")]
        public async Task<IActionResult> ListProductos([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try { return Ok(await _service.ListProductos(search, page, pageSize)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("productos/sugerencias")]
        public async Task<IActionResult> SugerirProductos([FromQuery] string nombre)
        {
            try { return Ok(await _service.SugerirProductos(nombre)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("productos/{id:long}")]
        public async Task<IActionResult> GetProducto(long id)
        {
            try { return Ok(await _service.GetProductoById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("productos")]
        public async Task<IActionResult> CrearProducto([FromBody] ProductoCreateDto dto)
        {
            if (!User.HasLbPermiso("CATALOGO_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para administrar el catálogo." });
            try { return Ok(await _service.CrearProducto(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("productos/{id:long}")]
        public async Task<IActionResult> ActualizarProducto(long id, [FromBody] ProductoUpdateDto dto)
        {
            if (!User.HasLbPermiso("CATALOGO_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para administrar el catálogo." });
            try { return Ok(await _service.ActualizarProducto(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
