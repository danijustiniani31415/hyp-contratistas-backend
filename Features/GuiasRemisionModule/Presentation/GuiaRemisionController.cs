using System.Linq;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;
using Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GuiasRemisionModule.Presentation
{
    /// <summary>Guías de Remisión Electrónica — Fase 3, punto 8 (emisión real ante el SEE de SUNAT).</summary>
    [ApiController]
    [Route("api/v1/guias-remision")]
    [Authorize]
    public class GuiaRemisionController : ControllerBase
    {
        private readonly IGuiaRemisionService _service;

        public GuiaRemisionController(IGuiaRemisionService service)
        {
            _service = service;
        }

        private long CurrentUsuarioSistemaId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : throw new AbrilException("Token inválido.", 401);

        /// <summary>Cualquiera de estos permisos habilita ver el listado/detalle — no solo
        /// GUIA_REMISION_VER. Bug real 2026-09-25: el rol RESIDENTE tiene GUIA_REMISION_CONFIRMAR
        /// pero no GUIA_REMISION_VER, así que no podía ni abrir la guía que debía confirmar.</summary>
        private static readonly string[] PermisosDeLectura =
            { "GUIA_REMISION_VER", "GUIA_REMISION_CREAR", "GUIA_REMISION_ENVIAR", "GUIA_REMISION_CONFIRMAR" };

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] GuiaRemisionCreateDto dto)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para crear guías de remisión." });
            try { return Ok(await _service.Crear(dto, CurrentUsuarioSistemaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!PermisosDeLectura.Any(User.HasLbPermiso))
                return StatusCode(403, new { message = "No tienes permiso para ver guías de remisión." });
            try
            {
                var scope = User.GetProyectosPermitidosUnion(PermisosDeLectura);
                return Ok(await _service.List(search, estado, scope.EsGlobal ? null : scope.ProyectoIds, page, pageSize));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (!PermisosDeLectura.Any(User.HasLbPermiso))
                return StatusCode(403, new { message = "No tienes permiso para ver guías de remisión." });
            try { return Ok(await _service.GetById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:long}/enviar")]
        public async Task<IActionResult> Enviar(long id)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_ENVIAR"))
                return StatusCode(403, new { message = "No tienes permiso para transmitir guías de remisión a SUNAT." });
            try
            {
                var scope = User.GetProyectosPermitidos("GUIA_REMISION_ENVIAR");
                return Ok(await _service.Enviar(id, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                var detalle = ex.InnerException != null ? $"{ex.Message} — {ex.InnerException.Message}" : ex.Message;
                return StatusCode(500, new { message = $"Error al transmitir a SUNAT: {detalle}" });
            }
        }

        [HttpPost("{id:long}/confirmar-recepcion")]
        public async Task<IActionResult> ConfirmarRecepcion(long id, [FromBody] ConfirmarRecepcionDto dto)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_CONFIRMAR"))
                return StatusCode(403, new { message = "No tienes permiso para confirmar recepción de guías de remisión." });
            try
            {
                var scope = User.GetProyectosPermitidos("GUIA_REMISION_CONFIRMAR");
                return Ok(await _service.ConfirmarRecepcion(id, dto, CurrentUsuarioSistemaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:long}/consultar-estado")]
        public async Task<IActionResult> ConsultarEstado(long id)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_ENVIAR"))
                return StatusCode(403, new { message = "No tienes permiso para consultar el estado ante SUNAT." });
            try
            {
                var scope = User.GetProyectosPermitidos("GUIA_REMISION_ENVIAR");
                return Ok(await _service.ConsultarEstado(id, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = $"Error al consultar SUNAT: {ex.Message}" }); }
        }
    }
}
