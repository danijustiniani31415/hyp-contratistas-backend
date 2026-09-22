using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.HerramientasModule.Application.Dtos;
using Abril_Backend.Features.HerramientasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.HerramientasModule.Presentation
{
    /// <summary>Herramientas y Equipos — Fase 2 (CONTEXT_LOGISTICA.md sección 3, punto 6).</summary>
    [ApiController]
    [Route("api/v1/prestamos")]
    [Authorize]
    public class PrestamoController : ControllerBase
    {
        private readonly IPrestamoService _service;

        public PrestamoController(IPrestamoService service)
        {
            _service = service;
        }

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] PrestamoCreateDto dto)
        {
            if (!User.HasLbPermiso("HERRAMIENTA_PRESTAR"))
                return StatusCode(403, new { message = "No tienes permiso para registrar préstamos." });
            try { return Ok(await _service.Crear(dto, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] bool soloAbiertos = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var scope = User.GetProyectosPermitidos("HERRAMIENTA_PRESTAR");
                return Ok(await _service.List(search, soloAbiertos, scope.EsGlobal ? null : scope.ProyectoIds, page, pageSize));
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

        [HttpPost("{id:long}/items/{itemId:long}/devolver")]
        public async Task<IActionResult> DevolverItem(long id, long itemId, [FromBody] DevolverItemDto dto)
        {
            if (!User.HasLbPermiso("HERRAMIENTA_DEVOLVER"))
                return StatusCode(403, new { message = "No tienes permiso para registrar devoluciones." });
            try
            {
                var scope = User.GetProyectosPermitidos("HERRAMIENTA_DEVOLVER");
                return Ok(await _service.DevolverItem(id, itemId, dto, CurrentUsuarioSistemaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
