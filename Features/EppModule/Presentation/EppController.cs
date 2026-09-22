using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.EppModule.Application.Dtos;
using Abril_Backend.Features.EppModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.EppModule.Presentation
{
    /// <summary>EPP — Fase 2 (CONTEXT_LOGISTICA.md sección 3, punto 5).</summary>
    [ApiController]
    [Route("api/v1/epp")]
    [Authorize]
    public class EppController : ControllerBase
    {
        private readonly IEppService _service;

        public EppController(IEppService service)
        {
            _service = service;
        }

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] EntregaEppCreateDto dto)
        {
            if (!User.HasLbPermiso("EPP_ENTREGAR"))
                return StatusCode(403, new { message = "No tienes permiso para registrar entregas de EPP." });
            try { return Ok(await _service.Crear(dto, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int? personaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var scope = User.GetProyectosPermitidos("EPP_ENTREGAR");
                return Ok(await _service.List(search, personaId, scope.EsGlobal ? null : scope.ProyectoIds, page, pageSize));
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
    }
}
