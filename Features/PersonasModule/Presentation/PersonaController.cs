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

        [HttpGet("reniec/{dni}")]
        public async Task<IActionResult> BuscarPorDni(string dni)
        {
            try
            {
                var persona = await _service.BuscarPorDni(dni);
                if (persona is null) return NotFound(new { message = "DNI no encontrado en RENIEC." });
                return Ok(persona);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("dashboard-planilla")]
        public async Task<IActionResult> GetDashboardPlanilla()
        {
            try { return Ok(await _service.GetDashboardPlanilla()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("cargos")]
        public async Task<IActionResult> ListCargos()
        {
            try { return Ok(await _service.ListCargos()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("cargos")]
        public async Task<IActionResult> CrearCargo([FromBody] CargoCreateDto dto)
        {
            try { return Ok(await _service.CrearCargo(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("cargos/{id:int}")]
        public async Task<IActionResult> ActualizarCargo(int id, [FromBody] CargoUpdateDto dto)
        {
            try { return Ok(await _service.ActualizarCargo(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

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
        public async Task<IActionResult> List(
            [FromQuery] string? search,
            [FromQuery] int? cargoId,
            [FromQuery] short? tipoVinculoId,
            [FromQuery] string? estado,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                return Ok(await _service.List(search, cargoId, tipoVinculoId, estado, page, pageSize));
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

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarDatos(int id, [FromBody] PersonaUpdateDto dto)
        {
            try { return Ok(await _service.ActualizarDatos(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}/planilla")]
        public async Task<IActionResult> ActualizarPlanilla(int id, [FromBody] PersonaPlanillaDto dto)
        {
            try { return Ok(await _service.ActualizarPlanilla(id, dto)); }
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

        [HttpPut("{id:int}/usuario/email")]
        public async Task<IActionResult> CambiarEmail(int id, [FromBody] CambiarEmailDto dto)
        {
            try
            {
                return Ok(await _service.CambiarEmail(id, dto));
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
