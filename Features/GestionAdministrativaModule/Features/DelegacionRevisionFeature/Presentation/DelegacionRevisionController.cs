using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Interfaces;
using Abril_Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/delegacion-revision")]
    [Authorize(Roles = Roles.AdministradorSolicitudSalidas)]
    public class DelegacionRevisionController : ControllerBase
    {
        private readonly IDelegacionRevisionService _service;
        private readonly ILogger<DelegacionRevisionController> _logger;

        public DelegacionRevisionController(IDelegacionRevisionService service, ILogger<DelegacionRevisionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Carga inicial: asignaciones (área o área+proyecto) en las que el usuario logueado es
        /// revisor, con sus revisores y las opciones de trabajadores designables de esa área.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetInitialData()
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                    ? id : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                return Ok(await _service.GetInitialDataAsync(userId.Value));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DelegacionRevisionController.GetInitialData");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Reemplaza los revisores de una asignación del usuario (área o área+proyecto vía dto.ProjectId).
        /// El usuario debe ser revisor vivo de esa asignación y no puede quitarse a sí mismo.
        /// </summary>
        [HttpPut("{areaScopeId:int}")]
        public async Task<IActionResult> Update(int areaScopeId, [FromBody] DelegacionUpdateDto dto)
        {
            try
            {
                var userId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                    ? id : (int?)null;
                if (userId == null)
                    return Unauthorized(new { message = "Usuario no autenticado." });

                await _service.UpdateAsync(userId.Value, areaScopeId, dto?.ProjectId, dto?.Revisores ?? new List<DelegacionAsignacionDto>());
                return Ok(new { message = "Revisores actualizados exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DelegacionRevisionController.Update");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
