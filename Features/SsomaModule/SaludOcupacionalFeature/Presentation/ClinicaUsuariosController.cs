using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.ClinicaUsuarios;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/catalogos/clinicas/{clinicaId:int}/usuarios")]
    [Authorize]
    public class ClinicaUsuariosController : ControllerBase
    {
        private readonly IClinicaUsuarioService _service;
        private readonly ILogger<ClinicaUsuariosController> _logger;

        public ClinicaUsuariosController(IClinicaUsuarioService service, ILogger<ClinicaUsuariosController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida";

        [HttpGet]
        public async Task<IActionResult> GetUsuarios(int clinicaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (User.IsInRole(Roles.Clinica) && !User.IsInRole(Roles.AdministradorSsoma) && !ClinicaClaimsHelper.ValidarAcceso(User, clinicaId))
                    return StatusCode(403, new { message = "Acceso no autorizado." });
                return Ok(await _service.GetUsuariosByClinicaAsync(clinicaId, page, pageSize));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaUsuariosController.GetUsuarios"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{usuarioId:int}")]
        public async Task<IActionResult> GetUsuario(int clinicaId, int usuarioId)
        {
            try
            {
                if (User.IsInRole(Roles.Clinica) && !User.IsInRole(Roles.AdministradorSsoma) && !ClinicaClaimsHelper.ValidarAcceso(User, clinicaId))
                    return StatusCode(403, new { message = "Acceso no autorizado." });
                return Ok(await _service.GetUsuarioByIdAsync(clinicaId, usuarioId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaUsuariosController.GetUsuario"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUsuario(int clinicaId, [FromBody] ClinicaUsuarioCreateDto dto)
        {
            try
            {
                int? creadoPor = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;
                return Ok(await _service.CreateUsuarioAsync(clinicaId, dto, creadoPor, Ip));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaUsuariosController.CreateUsuario"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{usuarioId:int}")]
        public async Task<IActionResult> UpdateUsuario(int clinicaId, int usuarioId, [FromBody] ClinicaUsuarioUpdateDto dto)
        {
            try
            {
                int? modificadoPor = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid1) ? uid1 : null;
                return Ok(await _service.UpdateUsuarioAsync(clinicaId, usuarioId, dto, modificadoPor));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaUsuariosController.UpdateUsuario"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{usuarioId:int}/toggle")]
        public async Task<IActionResult> ToggleActivo(int clinicaId, int usuarioId)
        {
            try
            {
                int? modificadoPor = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid2) ? uid2 : null;
                await _service.ToggleActivoAsync(clinicaId, usuarioId, modificadoPor, Ip);
                return Ok(new { message = "Estado del usuario actualizado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaUsuariosController.ToggleActivo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("{usuarioId:int}")]
        public async Task<IActionResult> SoftDelete(int clinicaId, int usuarioId)
        {
            try
            {
                await _service.SoftDeleteAsync(clinicaId, usuarioId, Ip);
                return Ok(new { message = "Usuario desactivado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaUsuariosController.SoftDelete"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{usuarioId:int}/reenviar-activacion")]
        public async Task<IActionResult> ReenviarActivacion(int clinicaId, int usuarioId)
        {
            try
            {
                await _service.ReenviarActivacionAsync(clinicaId, usuarioId, Ip);
                return Ok(new { message = "Correo de activación reenviado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaUsuariosController.ReenviarActivacion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
