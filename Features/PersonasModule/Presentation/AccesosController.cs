using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PersonasModule.Presentation
{
    /// <summary>
    /// Vista única de accesos: todos los usuarios del sistema con sus roles y el alcance de cada uno (toda la empresa o un
    /// proyecto). Es solo lectura; dar o quitar un rol usa los endpoints de personas (/{id}/asignaciones).
    /// </summary>
    [ApiController]
    [Route("api/v1/accesos")]
    [Authorize]
    public class AccesosController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        public AccesosController(IDbContextFactory<AppDbContext> factory) => _factory = factory;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (!User.HasLbPermiso("PERSONA_GESTIONAR") && !User.HasLbPermiso("ROLES_GESTIONAR"))
                return StatusCode(403, new { message = "No tienes permiso para administrar accesos." });
            try
            {
                using var ctx = _factory.CreateDbContext();
                var roles = await ctx.Rol.AsNoTracking().Where(r => r.Activo).OrderBy(r => r.Nombre)
                    .Select(r => new { r.Id, r.Codigo, r.Nombre, r.Descripcion, r.EsGlobal }).ToListAsync();

                var usuarios = await ctx.UsuarioSistema.AsNoTracking()
                    .OrderBy(u => u.Persona!.Apellidos).ThenBy(u => u.Persona!.Nombres)
                    .Select(u => new
                    {
                        PersonaId = u.PersonaId,
                        Nombre = u.Persona!.Apellidos + " " + u.Persona.Nombres,
                        Documento = u.Persona.NumeroDocumento,
                        EmailLogin = u.EmailLogin,
                        u.Estado,
                        u.UltimoAcceso,
                        Asignaciones = u.Asignaciones.Where(a => a.FechaFin == null).Select(a => new
                        {
                            a.Id,
                            a.RolId,
                            RolNombre = a.Rol!.Nombre,
                            a.ProyectoId,
                            ProyectoNombre = a.Proyecto != null ? a.Proyecto.Nombre : null,
                        }).ToList(),
                    }).ToListAsync();

                return Ok(new { roles, usuarios });
            }
            catch (Exception) { return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
