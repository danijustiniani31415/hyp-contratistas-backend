using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Features.PersonasModule;

namespace Abril_Backend.Features.PersonasModule.Presentation
{
    /// <summary>Catálogo genérico de listas fijas (banco, tipo AFP/ONP, categoría laboral, ...) —
    /// "todo debe ser modificable desde el frontend", ver CatalogoValor.cs.</summary>
    [ApiController]
    [Route("api/v1/catalogo-valores")]
    [Authorize]
    public class CatalogoValorController : ControllerBase
    {
        private readonly ICatalogoValorService _service;

        public CatalogoValorController(ICatalogoValorService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string tipo)
        {
            try { return Ok(await _service.List(tipo)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CatalogoValorCreateDto dto)
        {
            if (!User.HasLbPermiso("CATALOGO_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para administrar catálogos." });
            try { return Ok(await _service.Crear(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] CatalogoValorUpdateDto dto)
        {
            if (!User.HasLbPermiso("CATALOGO_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para administrar catálogos." });
            try { return Ok(await _service.Actualizar(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
