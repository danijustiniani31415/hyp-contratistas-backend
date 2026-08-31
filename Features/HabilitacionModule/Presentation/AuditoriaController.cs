using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Auditoria;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/auditoria")]
    [Authorize]
    public class AuditoriaController : ControllerBase
    {
        private static readonly string[] RolesPermitidos = [Roles.AdministradorSsoma, Roles.AdministradorSistema];

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IHabTrabajadorRepository _habTrabajadorRepo;
        private readonly ILogger<AuditoriaController> _logger;

        public AuditoriaController(
            IDbContextFactory<AppDbContext> factory,
            IHabTrabajadorRepository habTrabajadorRepo,
            ILogger<AuditoriaController> logger)
        {
            _factory = factory;
            _habTrabajadorRepo = habTrabajadorRepo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditoria(
            [FromQuery] string? tabla,
            [FromQuery] int? registroId,
            [FromQuery] int? usuarioId,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!UsuarioTienePermiso())
                    return StatusCode(403, new { message = "Solo ADMINISTRADOR SSOMA o ADMINISTRADOR DEL SISTEMA pueden consultar auditoría." });

                using var ctx = _factory.CreateDbContext();
                var query = ctx.AuditoriaCambios.AsQueryable();

                if (!string.IsNullOrWhiteSpace(tabla)) query = query.Where(a => a.Tabla == tabla);
                if (registroId.HasValue) query = query.Where(a => a.RegistroId == registroId.Value);
                if (usuarioId.HasValue) query = query.Where(a => a.UsuarioId == usuarioId.Value);
                if (desde.HasValue) query = query.Where(a => a.CreatedAt >= desde.Value);
                if (hasta.HasValue) query = query.Where(a => a.CreatedAt <= hasta.Value);

                var total = await query.CountAsync();

                var rows = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new AuditoriaCambioDto
                    {
                        Id = a.Id,
                        Tabla = a.Tabla,
                        RegistroId = a.RegistroId,
                        Accion = a.Accion,
                        DatosAnteriores = a.DatosAnteriores,
                        DatosNuevos = a.DatosNuevos,
                        UsuarioId = a.UsuarioId,
                        UsuarioNombre = a.UsuarioNombre,
                        EmpresaContratistaId = a.EmpresaContratistaId,
                        IpAddress = a.IpAddress,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                var result = new PagedResult<AuditoriaCambioDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Data = rows
                };

                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AuditoriaController.GetAuditoria"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("versiones-documento")]
        public async Task<IActionResult> GetVersionesDocumento([FromQuery] int habTrabajadorId)
        {
            try
            {
                if (!UsuarioTienePermiso())
                    return StatusCode(403, new { message = "Solo ADMINISTRADOR SSOMA o ADMINISTRADOR DEL SISTEMA pueden consultar versiones." });

                var versiones = await _habTrabajadorRepo.GetVersionesDocumentoAsync(habTrabajadorId);
                return Ok(versiones);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en AuditoriaController.GetVersionesDocumento"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private bool UsuarioTienePermiso()
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            return roles.Any(r => RolesPermitidos.Contains(r, StringComparer.OrdinalIgnoreCase));
        }
    }
}
