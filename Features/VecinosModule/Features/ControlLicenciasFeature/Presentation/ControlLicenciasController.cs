using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Application.Dtos;
using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Presentation
{
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ControlLicenciasController : ControllerBase
    {
        private readonly IControlLicenciasService _service;
        private readonly ILogger<ControlLicenciasController> _logger;
        private readonly IConfiguration _configuration;

        public ControlLicenciasController(
            IControlLicenciasService service,
            ILogger<ControlLicenciasController> logger,
            IConfiguration configuration)
        {
            _service = service;
            _logger = logger;
            _configuration = configuration;
        }

        private int CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        /// <summary>Proyectos activos disponibles para Control de Licencias.</summary>
        [HttpGet("proyectos")]
        public async Task<IActionResult> GetProyectos()
        {
            try
            {
                var result = await _service.GetProyectos();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS PROYECTOS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Plantilla de tipos de licencia (base + propios) con el estado vigente de cada uno, para un proyecto.</summary>
        [HttpGet("proyectos/{projectId:int}")]
        public async Task<IActionResult> GetPlantilla(int projectId)
        {
            try
            {
                var result = await _service.GetPlantilla(projectId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS PLANTILLA GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Agrega un tipo de licencia propio de un proyecto (no afecta la plantilla base ni a otros proyectos).</summary>
        [HttpPost("proyectos/{projectId:int}/tipos")]
        public async Task<IActionResult> AddTipo(int projectId, [FromBody] VecinoLicenciaTipoCreateDto dto)
        {
            try
            {
                var tipo = await _service.AddTipo(projectId, dto, CurrentUserId());
                return Ok(new { tipo, message = "Tipo de licencia agregado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS TIPO ADD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Sube/reemplaza el documento vigente de un tipo de licencia. Si había uno, se archiva en el historial.</summary>
        [HttpPost("proyectos/{projectId:int}/tipos/{tipoId:int}/upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadLicencia(int projectId, int tipoId, [FromForm] IFormFile file, [FromForm] VecinoLicenciaUploadDto dto)
        {
            try
            {
                await _service.UploadLicencia(projectId, tipoId, dto, file, CurrentUserId());
                return Ok(new { message = "Licencia registrada exitosamente." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS UPLOAD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Agrega un recordatorio adicional (N días antes) a la licencia vigente de un tipo.</summary>
        [HttpPost("proyectos/{projectId:int}/tipos/{tipoId:int}/recordatorios")]
        public async Task<IActionResult> AddRecordatorio(int projectId, int tipoId, [FromBody] VecinoLicenciaRecordatorioCreateDto dto)
        {
            try
            {
                var recordatorio = await _service.AddRecordatorio(projectId, tipoId, dto, CurrentUserId());
                return Ok(new { recordatorio, message = "Recordatorio agregado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS RECORDATORIO ADD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpDelete("recordatorios/{recordatorioId:int}")]
        public async Task<IActionResult> DeleteRecordatorio(int recordatorioId)
        {
            try
            {
                await _service.DeleteRecordatorio(recordatorioId, CurrentUserId());
                return Ok(new { message = "Recordatorio eliminado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS RECORDATORIO DELETE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPatch("proyectos/{projectId:int}/tipos/{tipoId:int}/no-aplica")]
        public async Task<IActionResult> SetNoAplica(int projectId, int tipoId, [FromBody] VecinoLicenciaNoAplicaDto dto)
        {
            try
            {
                await _service.SetNoAplica(projectId, tipoId, dto.NoAplica, CurrentUserId());
                return Ok(new { message = "Estado actualizado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS NO APLICA: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        // ── Catálogo base (plantilla común a todos los proyectos) ───────────────

        /// <summary>Catálogo base: tipos visibles en todos los proyectos.</summary>
        [HttpGet("catalogo")]
        public async Task<IActionResult> GetCatalogoBase()
        {
            try
            {
                var result = await _service.GetCatalogoBase();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS CATALOGO GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Agrega un tipo a la plantilla base: aparece de inmediato en todos los proyectos.</summary>
        [HttpPost("catalogo")]
        public async Task<IActionResult> AddTipoBase([FromBody] VecinoLicenciaTipoBaseUpsertDto dto)
        {
            try
            {
                var tipo = await _service.AddTipoBase(dto, CurrentUserId());
                return Ok(new { tipo, message = "Tipo agregado a la plantilla base." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS CATALOGO ADD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Edita un tipo del catálogo (base o propio de un proyecto): descripción y días de antelación por defecto.</summary>
        [HttpPut("catalogo/{tipoId:int}")]
        public async Task<IActionResult> UpdateTipo(int tipoId, [FromBody] VecinoLicenciaTipoBaseUpsertDto dto)
        {
            try
            {
                var tipo = await _service.UpdateTipo(tipoId, dto, CurrentUserId());
                return Ok(new { tipo, message = "Tipo actualizado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS CATALOGO UPDATE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Elimina (soft delete) un tipo del catálogo base — deja de verse en todos los proyectos.</summary>
        [HttpDelete("catalogo/{tipoId:int}")]
        public async Task<IActionResult> DeleteTipo(int tipoId)
        {
            try
            {
                await _service.DeleteTipo(tipoId, CurrentUserId());
                return Ok(new { message = "Tipo eliminado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS CATALOGO DELETE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Historial de versiones anteriores del documento de un tipo de licencia en un proyecto.</summary>
        [HttpGet("proyectos/{projectId:int}/tipos/{tipoId:int}/historial")]
        public async Task<IActionResult> GetHistorial(int projectId, int tipoId)
        {
            try
            {
                var result = await _service.GetHistorial(projectId, tipoId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS HISTORIAL: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>Destinatarios de recordatorio configurados para un proyecto (por rol).</summary>
        [HttpGet("proyectos/{projectId:int}/destinatarios")]
        public async Task<IActionResult> GetDestinatarios(int projectId)
        {
            try
            {
                var result = await _service.GetDestinatarios(projectId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS DESTINATARIOS GET: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpPost("proyectos/{projectId:int}/destinatarios")]
        public async Task<IActionResult> AddDestinatario(int projectId, [FromBody] VecinoLicenciaDestinatarioUpsertDto dto)
        {
            try
            {
                var destinatario = await _service.AddDestinatario(projectId, dto, CurrentUserId());
                return Ok(new { destinatario, message = "Destinatario agregado." });
            }
            catch (AbrilException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS DESTINATARIOS ADD: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        [HttpDelete("destinatarios/{destinatarioId:int}")]
        public async Task<IActionResult> DeleteDestinatario(int destinatarioId)
        {
            try
            {
                await _service.DeleteDestinatario(destinatarioId, CurrentUserId());
                return Ok(new { message = "Destinatario eliminado." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS DESTINATARIOS DELETE: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Cron (cron-job.org): envía los recordatorios de licencias cuya fecha de recordatorio
        /// ya llegó, en todos los proyectos. Protegido con el CronSecret en el header Authorization.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("recordatorios/procesar")]
        public async Task<IActionResult> ProcesarRecordatorios()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != $"Bearer {_configuration["CronSecret"]}")
                    return Unauthorized();

                var result = await _service.ProcesarRecordatorios();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR CONTROL LICENCIAS RECORDATORIOS: {msg}", ex.ToString());
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
