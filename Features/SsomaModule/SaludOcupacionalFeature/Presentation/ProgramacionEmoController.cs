using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Filters;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/programaciones")]
    [Authorize]
    [RequireFeature("ssoma.salud-ocupacional.programaciones", "clinica.agenda", "clinica.programaciones")]
    public class ProgramacionEmoController : ControllerBase
    {
        private readonly IProgramacionEmoService _service;
        private readonly ILogger<ProgramacionEmoController> _logger;

        public ProgramacionEmoController(IProgramacionEmoService service, ILogger<ProgramacionEmoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int? CurrentUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] ProgramacionFilterDto filter)
        {
            // Usuarios internos (no clínica) ven todas las programaciones aunque haya interconsulta pendiente
            filter.IncluirConInterconsulta = !User.IsInRole(Roles.Clinica);
            try { return Ok(await _service.List(filter)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProgramacionCreateDto dto)
        {
            try
            {
                var id = await _service.Create(dto, CurrentUserId());
                return Ok(new { id, message = "Programación registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProgramacionUpdateDto dto)
        {
            try
            {
                await _service.Update(id, dto, CurrentUserId());
                return Ok(new { message = "Programación actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/estado")]
        public async Task<IActionResult> PatchEstado(int id, [FromBody] ProgramacionEstadoPatchDto dto)
        {
            try
            {
                await _service.UpdateEstado(id, dto.Estado, dto.EmoResultadoId, CurrentUserId());
                return Ok(new { message = "Estado de programación actualizado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/clinica-accion")]
        public async Task<IActionResult> ClinicaAccion(int id, [FromBody] ProgramacionClinicaAccionDto dto)
        {
            try
            {
                await _service.ClinicaAccion(id, dto, CurrentUserId());
                return Ok(new { message = "Acción registrada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> GetResumen([FromQuery] ProgramacionFilterDto filter)
        {
            filter.IncluirConInterconsulta = !User.IsInRole(Roles.Clinica);
            try { return Ok(await _service.GetResumen(filter)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController.GetResumen"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Vista previa de a quién le llegan los DOS correos del flujo, según la Configuración
        /// de EMOs: el de la programación manual (al guardar) y el de la programación aceptada
        /// por la clínica (después, si la clínica acepta). La usa el modal "Programar EMO con
        /// clínica" para mostrarlos antes de guardar. Se vuelve a pedir al cambiar de clínica
        /// porque los correos de contacto dependen de ella.
        /// </summary>
        [HttpGet("destinatarios")]
        public async Task<IActionResult> GetDestinatarios([FromQuery] int workerId, [FromQuery] int? clinicaId)
        {
            try { return Ok(await _service.GetDestinatarios(workerId, clinicaId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController.GetDestinatarios"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Razones sociales del grupo con sus cupos disponibles. La pide el modal "Programar EMO
        /// con clínica" SOLO cuando el trabajador todavía no tiene razón social —el ingreso directo
        /// FFT, que va de la solicitud al EMO sin pasar por la asignación de Reclutamiento— para
        /// ofrecerle el desplegable en lugar del campo de solo lectura. En el caso normal la
        /// pantalla no la pide y no cuesta ningún roundtrip.
        /// </summary>
        [HttpGet("razones-sociales")]
        public async Task<IActionResult> GetRazonesSociales()
        {
            try { return Ok(await _service.GetRazonesSociales()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController.GetRazonesSociales"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Correo de inasistencias del día — botón de la Agenda de Clínica. Solo la clínica
        /// (el médico) lo dispara, mismo criterio que InterconsultaController.EnviarCorreos:
        /// el envío parte de su bandeja, no de SSOMA/Habilitación.
        /// </summary>
        [HttpPost("inasistencias/enviar-correos")]
        [Authorize(Roles = Roles.Clinica)]
        public async Task<IActionResult> EnviarInasistencias([FromQuery] DateOnly? fecha)
        {
            try
            {
                var f = fecha ?? DateOnly.FromDateTime(DateTime.Today);
                return Ok(await _service.EnviarInasistencias(f));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController.EnviarInasistencias"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("habilitacion")]
        public async Task<IActionResult> GetHabilitacion(
            [FromQuery] string? estado,
            [FromQuery] int? proyectoId,
            [FromQuery] string? fecha,
            [FromQuery] bool? soloNoNotificados)
        {
            try
            {
                var filtros = new ProgramacionHabilitacionFiltrosDto
                {
                    Estado             = estado,
                    ProyectoId         = proyectoId,
                    Fecha              = fecha,
                    SoloNoNotificados  = soloNoNotificados,
                };
                var result = await _service.GetHabilitacionAsync(filtros);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error GetHabilitacion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/notificado")]
        public async Task<IActionResult> PatchNotificado(int id, [FromBody] PatchNotificadoDto dto)
        {
            try
            {
                await _service.PatchNotificadoAsync(id, dto.Notificado);
                return NoContent();
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error PatchNotificado"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/deshacer-checkin")]
        public async Task<IActionResult> DeshacerCheckIn(int id)
        {
            try
            {
                await _service.UndoCheckInAsync(id);
                return Ok(new { message = "Ingreso deshecho. Estado revertido a 'Aceptado por Clínica'." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ProgramacionEmoController.DeshacerCheckIn"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
