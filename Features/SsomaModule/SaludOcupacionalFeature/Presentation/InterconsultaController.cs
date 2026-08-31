using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/interconsultas")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.interconsultas", "clinica.agenda")]
    public class InterconsultaController : ControllerBase
    {
        private readonly IInterconsultaService _service;
        private readonly ILogger<InterconsultaController> _logger;
        private readonly ISharePointHabService _sharePoint;
        private readonly IDbContextFactory<AppDbContext> _factory;

        public InterconsultaController(
            IInterconsultaService service,
            ILogger<InterconsultaController> logger,
            ISharePointHabService sharePoint,
            IDbContextFactory<AppDbContext> factory)
        {
            _service = service;
            _logger = logger;
            _sharePoint = sharePoint;
            _factory = factory;
        }

        private int? CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] InterconsultaFilterDto filter)
        {
            try { return Ok(await _service.List(filter)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InterconsultaController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // Solo la clínica (el médico) puede disparar estos correos en producción — el envío
        // parte de su bandeja, no de SSOMA/Habilitación, que solo tienen visibilidad de lectura
        // de este listado. Se incluye también Jefe SSOMA para que pueda probar el flujo.
        // Sin esta restricción cualquier usuario con acceso a la pantalla (por el featureKey de
        // la ruta, no por rol) podía enviarlos.
        [HttpPost("enviar-correos")]
        [Authorize(Roles = Roles.Clinica + "," + Roles.AdministradorSsoma)]
        public async Task<IActionResult> EnviarCorreos([FromBody] InterconsultaEnviarCorreoDto dto)
        {
            try { return Ok(await _service.EnviarRecordatorios(dto.Ids)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error enviando correos de interconsultas"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try { return Ok(await _service.GetById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InterconsultaController.GetById"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        [Authorize(Roles = Roles.Clinica)]
        public async Task<IActionResult> Create([FromBody] InterconsultaCreateDto dto)
        {
            try
            {
                var id = await _service.Create(dto, CurrentUserId());
                return Ok(new { id, message = "Interconsulta registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error al crear interconsulta"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = Roles.Clinica)]
        public async Task<IActionResult> Update(int id, [FromBody] InterconsultaUpdateDto dto)
        {
            try
            {
                await _service.Update(id, dto, CurrentUserId());
                return Ok(new { message = "Interconsulta actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InterconsultaController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Sube el levantamiento (informe) de la interconsulta. Solo la clínica puede hacer
        /// esto. El cambio de estado a "Atendida" y la reapertura de la programación se
        /// hacen en el PATCH .../resultado, que el frontend invoca a continuación.
        /// </summary>
        [HttpPost("{id}/documentos")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = Roles.Clinica)]
        public async Task<IActionResult> SubirDocumento(int id, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Archivo requerido." });

                using var ctx = _factory.CreateDbContext();
                var interconsulta = await ctx.SsInterconsulta.FindAsync(id)
                    ?? throw new AbrilException("Interconsulta no encontrada.", 404);

                using var stream = file.OpenReadStream();
                var path = await _sharePoint.SubirArchivoAsync(stream, file.FileName, "interconsulta");
                interconsulta.UrlInforme = path;
                interconsulta.UpdatedAt = DateTimeOffset.UtcNow;

                // El paso de "Atendida" y de reabrir la programación a "En Atención" (por el vínculo
                // real EmoResultadoId, con creación de la fila si nunca existió) lo hace
                // IInterconsultaService.UpdateResultado — el frontend llama a ese endpoint justo
                // después de subir el documento. No duplicar esa lógica aquí para no correr dos
                // heurísticas distintas sobre la misma interconsulta.
                await ctx.SaveChangesAsync();

                return Ok(new { url = path });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error subiendo documento interconsulta {Id}", id); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/resultado")]
        [Authorize(Roles = Roles.Clinica)]
        public async Task<IActionResult> PatchResultado(int id, [FromBody] InterconsultaResultadoPatchDto dto)
        {
            try
            {
                await _service.UpdateResultado(id, dto, CurrentUserId());
                return Ok(new { message = "Resultado de interconsulta actualizado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InterconsultaController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/derivacion")]
        [Authorize(Roles = Roles.Clinica)]
        public async Task<IActionResult> PatchDerivacion(int id, [FromBody] InterconsultaDerivacionPatchDto dto)
        {
            try
            {
                await _service.UpdateDerivacion(id, dto, CurrentUserId());
                return Ok(new { message = "Derivación actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en InterconsultaController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
