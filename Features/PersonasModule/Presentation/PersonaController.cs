using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.PersonasModule.Presentation
{
    [ApiController]
    [Route("api/v1/personas")]
    [Authorize]
    public class PersonaController : ControllerBase
    {
        private readonly IPersonaService _service;

        public PersonaController(IPersonaService service)
        {
            _service = service;
        }

        private long? CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        [HttpGet("catalogos")]
        public async Task<IActionResult> GetCatalogos()
        {
            try
            {
                return Ok(await _service.GetCatalogos());
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                return Ok(await _service.List(search, page, pageSize));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                return Ok(await _service.GetById(id));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PersonaCreateDto dto)
        {
            try
            {
                return Ok(await _service.Create(dto));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:int}/vinculos")]
        public async Task<IActionResult> NuevoVinculo(int id, [FromBody] NuevoVinculoDto dto)
        {
            try
            {
                return Ok(await _service.NuevoVinculo(id, dto));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:int}/usuario")]
        public async Task<IActionResult> CrearUsuario(int id, [FromBody] CrearUsuarioDto dto)
        {
            try
            {
                return Ok(await _service.CrearUsuario(id, dto, CurrentUsuarioSistemaId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:int}/asignaciones")]
        public async Task<IActionResult> NuevaAsignacion(int id, [FromBody] NuevaAsignacionDto dto)
        {
            try
            {
                return Ok(await _service.NuevaAsignacion(id, dto, CurrentUsuarioSistemaId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:int}/asignaciones/{asignacionId:long}/revocar")]
        public async Task<IActionResult> RevocarAsignacion(int id, long asignacionId)
        {
            try
            {
                return Ok(await _service.RevocarAsignacion(id, asignacionId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
