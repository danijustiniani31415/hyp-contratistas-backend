using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Features.PersonasModule;

namespace Abril_Backend.Features.PersonasModule.Presentation
{
    /// <summary>
    /// Pantalla de administración Roles y Permisos (CONTEXT_LOGISTICA.md sección 4): el admin
    /// decide qué rol ve qué módulo con checkboxes, sin SQL manual y sin tocar personas una por
    /// una — ver comentario en RolPermisoService. Todo el controller exige ROLES_GESTIONAR
    /// (incluida la lectura): quién tiene qué permiso es información sensible, no solo su edición.
    /// </summary>
    [ApiController]
    [Route("api/v1/roles")]
    [Authorize]
    public class RolPermisoController : ControllerBase
    {
        private readonly IRolPermisoService _service;

        public RolPermisoController(IRolPermisoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ListRoles()
        {
            if (!User.HasLbPermiso("ROLES_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para ver roles y permisos." });
            try { return Ok(await _service.ListRoles()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("permisos")]
        public async Task<IActionResult> ListPermisos()
        {
            if (!User.HasLbPermiso("ROLES_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para ver roles y permisos." });
            try { return Ok(await _service.ListPermisos()); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRolDetalle(short id)
        {
            if (!User.HasLbPermiso("ROLES_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para ver roles y permisos." });
            try { return Ok(await _service.GetRolDetalle(id)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}/permisos")]
        public async Task<IActionResult> ActualizarPermisos(short id, [FromBody] ActualizarPermisosRolDto dto)
        {
            if (!User.HasLbPermiso("ROLES_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para administrar roles y permisos." });
            try { return Ok(await _service.ActualizarPermisos(id, dto)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
