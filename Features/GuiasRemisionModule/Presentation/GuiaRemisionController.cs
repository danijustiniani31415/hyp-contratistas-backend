using System.Linq;
using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;
using Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces;
using Abril_Backend.Features.GuiasRemisionModule.Application.Services;
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
        private readonly RecepcionGuiaService _recepcion;

        public GuiaRemisionController(IGuiaRemisionService service, RecepcionGuiaService recepcion)
        {
            _service = service;
            _recepcion = recepcion;
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

        /// <summary>Elimina una guía en BORRADOR. Reusa GUIA_REMISION_CREAR a propósito: quien puede
        /// crear guías puede deshacer las que aún no se transmitieron, sin permiso nuevo en BD.</summary>
        [HttpPost("{id:long}/anular")]
        public async Task<IActionResult> Anular(long id, [FromBody] AnularGuiaDto dto)
        {
            // GUIA_REMISION_ENVIAR es el permiso de "transmitir y anular" — anular es más delicado que crear.
            if (!User.HasLbPermiso("GUIA_REMISION_ENVIAR"))
                return StatusCode(403, new { message = "No tienes permiso para anular guías de remisión." });
            try
            {
                var scope = User.GetProyectosPermitidos("GUIA_REMISION_ENVIAR");
                await _service.Anular(id, CurrentUsuarioSistemaId, scope, dto);
                return Ok(new { message = "Guía anulada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Eliminar(long id)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_CREAR"))
                return StatusCode(403, new { message = "No tienes permiso para eliminar guías de remisión." });
            try
            {
                var scope = User.GetProyectosPermitidos("GUIA_REMISION_CREAR");
                await _service.Eliminar(id, CurrentUsuarioSistemaId, scope);
                return NoContent();
            }
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

        /// <summary>Registra un viaje de recepción en el almacén de destino (llegó bien / dañado / al cerrar, faltante).</summary>
        [HttpPost("{id:long}/recepciones")]
        public async Task<IActionResult> RegistrarRecepcion(long id, [FromBody] RecepcionRegistrarDto dto)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_CONFIRMAR"))
                return StatusCode(403, new { message = "No tienes permiso para recibir guías de remisión." });
            try
            {
                var scope = User.GetProyectosPermitidos("GUIA_REMISION_CONFIRMAR");
                return Ok(await _recepcion.Registrar(id, dto, CurrentUsuarioSistemaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("recepciones/{recepcionId:long}/fotos")]
        [RequestSizeLimit(7 * 1024 * 1024)]
        public async Task<IActionResult> SubirFoto(long recepcionId, IFormFile foto)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_CONFIRMAR"))
                return StatusCode(403, new { message = "No tienes permiso para recibir guías de remisión." });
            if (foto == null || foto.Length == 0) return BadRequest(new { message = "Adjunta una foto." });
            try
            {
                using var ms = new MemoryStream();
                await foto.CopyToAsync(ms);
                return Ok(new { id = await _recepcion.GuardarFoto(recepcionId, foto.FileName, foto.ContentType, ms.ToArray(), CurrentUsuarioSistemaId) });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("recepciones/fotos/{fotoId:long}")]
        public async Task<IActionResult> VerFoto(long fotoId)
        {
            if (!PermisosDeLectura.Any(User.HasLbPermiso))
                return StatusCode(403, new { message = "No tienes permiso para ver guías de remisión." });
            try
            {
                var (contenido, mime) = await _recepcion.ObtenerFoto(fotoId);
                return File(contenido, mime);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Diferencias de recepción (faltantes y dañados), abiertas o resueltas, acotadas a los proyectos del usuario.</summary>
        [HttpGet("diferencias")]
        public async Task<IActionResult> ListarDiferencias([FromQuery] string? estado, [FromQuery] long? guiaId)
        {
            if (!PermisosDeLectura.Any(User.HasLbPermiso))
                return StatusCode(403, new { message = "No tienes permiso para ver las diferencias de recepción." });
            try
            {
                var scope = User.GetProyectosPermitidosUnion(PermisosDeLectura);
                return Ok(await _recepcion.ListarDiferencias(estado, guiaId, scope));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Resuelve una diferencia: reposición, reclamo, merma (necesita aprobación de gerencia) o "llegó después".</summary>
        [HttpPost("diferencias/{id:long}/resolver")]
        public async Task<IActionResult> ResolverDiferencia(long id, [FromBody] DiferenciaResolverDto dto)
        {
            if (!User.HasLbPermiso("GUIA_REMISION_ENVIAR"))
                return StatusCode(403, new { message = "No tienes permiso para resolver diferencias de recepción." });
            if (string.Equals(dto.Resolucion, "MERMA", StringComparison.OrdinalIgnoreCase) && !User.HasLbPermiso("PEDIDO_APROBAR"))
                return StatusCode(403, new { message = "Dar de baja una mercadería (merma) requiere la aprobación de gerencia." });
            try
            {
                var scope = User.GetProyectosPermitidosUnion(PermisosDeLectura);
                return Ok(await _recepcion.Resolver(id, dto, CurrentUsuarioSistemaId, scope));
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
