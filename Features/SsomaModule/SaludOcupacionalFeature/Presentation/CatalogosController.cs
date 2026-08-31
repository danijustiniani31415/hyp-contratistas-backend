using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/catalogos")]
    [Authorize(Roles = Roles.AdministradorSsoma)]
    public class CatalogosController : ControllerBase
    {
        private readonly ICatalogosService _service;
        private readonly ILogger<CatalogosController> _logger;

        public CatalogosController(ICatalogosService service, ILogger<CatalogosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ===== Clinicas =====
        [AllowAnonymous]
        [HttpGet("clinicas")]
        public async Task<IActionResult> GetClinicas([FromQuery] bool soloActivos = true)
        {
            try { return Ok(await _service.ListClinicas(soloActivos)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [AllowAnonymous]
        [HttpGet("clinicas/{id:int}")]
        public async Task<IActionResult> GetClinica(int id)
        {
            try { return Ok(await _service.GetClinicaById(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("clinicas")]
        public async Task<IActionResult> CreateClinica([FromBody] ClinicaUpsertDto dto)
        {
            try { return Ok(await _service.CreateClinica(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("clinicas/{id:int}")]
        public async Task<IActionResult> UpdateClinica(int id, [FromBody] ClinicaUpsertDto dto)
        {
            try { return Ok(await _service.UpdateClinica(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ===== Medicos =====
        [AllowAnonymous]
        [HttpGet("medicos")]
        public async Task<IActionResult> GetMedicos([FromQuery] bool soloActivos = true)
        {
            try { return Ok(await _service.ListMedicos(soloActivos)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("medicos")]
        public async Task<IActionResult> CreateMedico([FromBody] MedicoOcupacionalUpsertDto dto)
        {
            try { return Ok(await _service.CreateMedico(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("medicos/{id:int}")]
        public async Task<IActionResult> UpdateMedico(int id, [FromBody] MedicoOcupacionalUpsertDto dto)
        {
            try { return Ok(await _service.UpdateMedico(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ===== EMO Tipos =====
        [AllowAnonymous]
        [HttpGet("emo-tipos")]
        public async Task<IActionResult> GetEmoTipos([FromQuery] bool soloActivos = true)
        {
            try { return Ok(await _service.ListEmoTipos(soloActivos)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("emo-tipos")]
        public async Task<IActionResult> CreateEmoTipo([FromBody] EmoTipoUpsertDto dto)
        {
            try { return Ok(await _service.CreateEmoTipo(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("emo-tipos/{id:int}")]
        public async Task<IActionResult> UpdateEmoTipo(int id, [FromBody] EmoTipoUpsertDto dto)
        {
            try { return Ok(await _service.UpdateEmoTipo(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ===== Examen Tipos =====
        [AllowAnonymous]
        [HttpGet("examen-tipos")]
        public async Task<IActionResult> GetExamenTipos([FromQuery] bool soloActivos = true)
        {
            try { return Ok(await _service.ListExamenTipos(soloActivos)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("examen-tipos")]
        public async Task<IActionResult> CreateExamenTipo([FromBody] ExamenTipoUpsertDto dto)
        {
            try { return Ok(await _service.CreateExamenTipo(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("examen-tipos/{id:int}")]
        public async Task<IActionResult> UpdateExamenTipo(int id, [FromBody] ExamenTipoUpsertDto dto)
        {
            try { return Ok(await _service.UpdateExamenTipo(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ===== Restriccion Tipos =====
        [AllowAnonymous]
        [HttpGet("restriccion-tipos")]
        public async Task<IActionResult> GetRestriccionTipos([FromQuery] bool soloActivos = true)
        {
            try { return Ok(await _service.ListRestriccionTipos(soloActivos)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("restriccion-tipos")]
        public async Task<IActionResult> CreateRestriccionTipo([FromBody] RestriccionTipoUpsertDto dto)
        {
            try { return Ok(await _service.CreateRestriccionTipo(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("restriccion-tipos/{id:int}")]
        public async Task<IActionResult> UpdateRestriccionTipo(int id, [FromBody] RestriccionTipoUpsertDto dto)
        {
            try { return Ok(await _service.UpdateRestriccionTipo(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ===== Agente de Riesgo =====
        [AllowAnonymous]
        [HttpGet("agentes-riesgo")]
        public async Task<IActionResult> GetAgentesRiesgo([FromQuery] bool soloActivos = true)
        {
            try { return Ok(await _service.ListAgentesRiesgo(soloActivos)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("agentes-riesgo")]
        public async Task<IActionResult> CreateAgenteRiesgo([FromBody] AgenteRiesgoUpsertDto dto)
        {
            try { return Ok(await _service.CreateAgenteRiesgo(dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("agentes-riesgo/{id:int}")]
        public async Task<IActionResult> UpdateAgenteRiesgo(int id, [FromBody] AgenteRiesgoUpsertDto dto)
        {
            try { return Ok(await _service.UpdateAgenteRiesgo(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ===== Empresas =====
        // [AllowAnonymous], igual que el resto de catálogos GET de esta clase (clinicas, medicos,
        // emo-tipos, etc.): lo consumen RAC/Inspecciones/Habilitación/Configuración desde muchos
        // roles distintos, no solo AdministradorSsoma. Un intento anterior de poner [Authorize] a
        // secas para exigir login sin admin rompió producción: en ASP.NET Core los [Authorize] de
        // clase y de método se COMBINAN (AND), no se sobrescriben — el [Authorize(Roles=
        // AdministradorSsoma)] de la clase seguía aplicando igual, así que cualquier usuario sin
        // ese rol específico (la mayoría de quienes crean RAC) recibía 403 al abrir "Nuevo RAC".
        [AllowAnonymous]
        [HttpGet("empresas")]
        public async Task<IActionResult> GetEmpresas([FromQuery] bool soloActivas = true)
        {
            try { return Ok(await _service.ListEmpresas(soloActivas)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Consulta de RUC a SUNAT para el alta de una razón social.</summary>
        [Authorize]
        [HttpGet("empresas/ruc/{ruc}")]
        public async Task<IActionResult> GetEmpresaByRuc(string ruc)
        {
            try
            {
                var result = await _service.GetEmpresaByRuc(ruc);
                if (result is null)
                    return NotFound(new { message = "No se encontró información para el RUC proporcionado." });
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>Alta de una razón social (contribuyente).</summary>
        [Authorize]
        [HttpPost("empresas")]
        public async Task<IActionResult> CreateEmpresa([FromBody] EmpresaCreateDto dto)
        {
            try
            {
                int? userId = null;
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (claim != null && int.TryParse(claim.Value, out var uid)) userId = uid;

                var result = await _service.CreateEmpresa(dto, userId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        // ===== Clinica Emails =====
        [HttpGet("clinicas/{id:int}/emails")]
        public async Task<IActionResult> GetClinicaEmails(int id)
        {
            try { return Ok(await _service.ListClinicaEmails(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("clinicas/{id:int}/emails")]
        public async Task<IActionResult> CreateClinicaEmail(int id, [FromBody] ClinicaEmailCreateDto dto)
        {
            try { return Ok(await _service.CreateClinicaEmail(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("clinicas/{id:int}/emails/{emailId:int}")]
        public async Task<IActionResult> DeleteClinicaEmail(int id, int emailId)
        {
            try { await _service.DeleteClinicaEmail(id, emailId); return NoContent(); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en CatalogosController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
