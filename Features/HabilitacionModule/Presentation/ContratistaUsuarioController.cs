using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.ContratistaUsuarios;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/contratista-usuarios")]
    [Authorize]
    public class ContratistaUsuarioController : ControllerBase
    {
        private readonly IContratistaUsuarioService _service;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<ContratistaUsuarioController> _logger;

        public ContratistaUsuarioController(
            IContratistaUsuarioService service,
            IDbContextFactory<AppDbContext> dbFactory,
            ILogger<ContratistaUsuarioController> logger)
        {
            _service = service;
            _dbFactory = dbFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuarios([FromQuery] int contractorId)
        {
            try
            {
                var result = await _service.GetUsuariosAsync(contractorId);
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaUsuarioController.GetUsuarios"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        public async Task<IActionResult> InvitarUsuario(
            [FromQuery] int contractorId,
            [FromBody] ContratistaUsuarioCreateDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _service.InvitarUsuarioAsync(contractorId, dto, userId);
                return StatusCode(201, new { message = "Usuario invitado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaUsuarioController.InvitarUsuario"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> ActualizarUsuario(
            int id,
            [FromQuery] int contractorId,
            [FromBody] ContratistaUsuarioUpdateDto dto)
        {
            try
            {
                await _service.ActualizarUsuarioAsync(id, contractorId, dto);
                return Ok(new { message = "Usuario actualizado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaUsuarioController.ActualizarUsuario"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{contractorId}/buscar-workers")]
        public async Task<IActionResult> BuscarWorkers(int contractorId, [FromQuery] string? search)
        {
            try
            {
                using var ctx = _dbFactory.CreateDbContext();
                var workerIdsConUsuario = ctx.SsContratistaUsuarios
                    .Where(u => u.ContractorId == contractorId && u.Activo && u.WorkerId != null)
                    .Select(u => u.WorkerId!.Value);

                var workerIdsVinculadosActivos = ctx.WorkerVinculacion
                    .Where(v => v.EmpresaId == contractorId && v.FechaFin == null)
                    .Select(v => v.WorkerId);

                var query = ctx.Worker
                    .Include(w => w.Person)
                    .Where(w => workerIdsVinculadosActivos.Contains(w.Id)
                             && w.WorkersEstadoId == WorkersEstadoIds.Activo
                             && !workerIdsConUsuario.Contains(w.Id));

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    query = query.Where(w =>
                        (w.Person != null && (
                            (w.Person.DocumentIdentityCode != null && w.Person.DocumentIdentityCode.ToLower().Contains(s)) ||
                            (w.Person.FullName != null && w.Person.FullName.ToLower().Contains(s))
                        )));
                }

                var result = await query
                    .Select(w => new {
                        w.Id,
                        Dni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                        NombreCompleto = w.Person != null ? w.Person.FullName : null,
                        w.EmailCorporativo
                    })
                    .Take(10)
                    .ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando workers contractorId={Id}", contractorId);
                return StatusCode(500, new { message = "Error del servidor." });
            }
        }

        [HttpPatch("{id:int}/desactivar")]
        public async Task<IActionResult> DesactivarUsuario(int id, [FromQuery] int contractorId)
        {
            try
            {
                await _service.DesactivarUsuarioAsync(id, contractorId);
                return Ok(new { message = "Usuario desactivado correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ContratistaUsuarioController.DesactivarUsuario"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
