using System.Security.Claims;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Application.Interfaces;
using Abril_Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Presentation
{
    [ApiController]
    [Route("api/v1/gestion-administrativa/configuracion/revisores-areas")]
    [Authorize]
    public class AreaRevisorController : ControllerBase
    {
        /// <summary>
        /// Roles que configuran los revisores de áreas, en el formato separado por comas que
        /// espera <c>[Authorize(Roles = ...)]</c>. Es el mismo par que decide <c>verTodas</c>
        /// en la carga inicial: acá ver todas las áreas y editarlas van juntos.
        /// </summary>
        private const string RolesQueEditan =
            Roles.AdministradorSolicitudSalidas + "," + Roles.UsuarioGth;

        private readonly IAreaRevisorService _service;
        private readonly ILogger<AreaRevisorController> _logger;

        public AreaRevisorController(IAreaRevisorService service, ILogger<AreaRevisorController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>
        /// Carga inicial: gerencias y áreas estándar (primer nodo de su tipo en cada rama) con sus n revisores + opciones.
        /// ADMINISTRADOR DE SOLICITUD DE SALIDAS y USUARIO DE GTH ven todas las áreas y pueden
        /// editarlas; un trabajador con categoría Jefe/Coordinador/Gerente ve solo su área (de
        /// lectura); el resto no ve ninguna.
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

                // Ver todas y editar coinciden: los dos roles que configuran esta pantalla.
                var verTodas = User.IsInRole(Roles.AdministradorSolicitudSalidas)
                               || User.IsInRole(Roles.UsuarioGth);
                return Ok(await _service.GetInitialDataAsync(userId.Value, verTodas));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AreaRevisorController.GetInitialData");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Reemplaza el conjunto de revisores de un área (nodo area_scope) o de un proyecto
        /// dentro del área (dto.ProjectId con valor). Editan el ADMINISTRADOR DE SOLICITUD DE
        /// SALIDAS y el USUARIO DE GTH.
        /// </summary>
        [HttpPut("{areaScopeId:int}")]
        [Authorize(Roles = RolesQueEditan)]
        public async Task<IActionResult> UpdateRevisores(int areaScopeId, [FromBody] AreaRevisoresUpdateDto dto)
        {
            try
            {
                await _service.UpdateAreaRevisoresAsync(areaScopeId, dto?.ProjectId, dto?.Revisores ?? new List<AreaRevisorAsignacionDto>());
                return Ok(new { message = "Revisores del área actualizados exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AreaRevisorController.UpdateRevisores");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }

        /// <summary>
        /// Marca/desmarca "filtrar por proyecto" para un área. Al activarse, el área se
        /// subdivide por proyecto y sus revisores se asignan por proyecto.
        /// Editan el ADMINISTRADOR DE SOLICITUD DE SALIDAS y el USUARIO DE GTH.
        /// </summary>
        [HttpPut("{areaScopeId:int}/filtro-proyecto")]
        [Authorize(Roles = RolesQueEditan)]
        public async Task<IActionResult> SetFiltroProyecto(int areaScopeId, [FromBody] AreaFiltroProyectoUpdateDto dto)
        {
            try
            {
                await _service.SetFiltroProyectoAsync(areaScopeId, dto?.FiltraPorProyecto ?? false);
                return Ok(new { message = "Configuración del área actualizada exitosamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en AreaRevisorController.SetFiltroProyecto");
                return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." });
            }
        }
    }
}
