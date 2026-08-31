using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Responsables;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    /// <summary>
    /// Administradores/coordinadores responsables por razón social y por proyecto — usados por
    /// los correos automáticos de EMOs/Interconsultas (ver InterconsultaService/EmoAlertaService).
    /// Restringido a Jefe SSOMA y Administrador de Administración: son los roles que corrigen
    /// estos datos cuando cambia la persona responsable.
    /// </summary>
    [ApiController]
    [Route("api/v1/habilitacion/responsables")]
    [Authorize(Roles = Roles.AdministradorSsoma + "," + Roles.AdministradorAdministracion)]
    public class ResponsablesController : ControllerBase
    {
        private readonly IResponsablesRepository _repo;
        private readonly ILogger<ResponsablesController> _logger;

        public ResponsablesController(IResponsablesRepository repo, ILogger<ResponsablesController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try { return Ok(await _repo.GetAll()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ResponsablesController.GetAll"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("razones-sociales/{id:int}")]
        public async Task<IActionResult> UpdateRazonSocial(int id, [FromBody] ResponsableRazonSocialUpdateDto dto)
        {
            try { await _repo.UpdateRazonSocial(id, dto); return Ok(new { message = "Administrador actualizado." }); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ResponsablesController.UpdateRazonSocial"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("proyectos/{id:int}")]
        public async Task<IActionResult> UpdateProyecto(int id, [FromBody] ResponsableProyectoUpdateDto dto)
        {
            try { await _repo.UpdateProyecto(id, dto); return Ok(new { message = "Coordinador de administración actualizado." }); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ResponsablesController.UpdateProyecto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
