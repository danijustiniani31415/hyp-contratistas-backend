using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Abril_Backend.Shared.Constants;
using System.Security.Claims;

namespace Abril_Backend.Features.Habilitacion.Presentation
{
    [ApiController]
    [Route("api/v1/habilitacion/empresas")]
    [Authorize]
    public class EmpresaContratistaController : ControllerBase
    {
        private readonly IEmpresaContratistaRepository _repo;
        private readonly IContratistaAuthService _authService;
        private readonly ILogger<EmpresaContratistaController> _logger;

        public EmpresaContratistaController(
            IEmpresaContratistaRepository repo,
            IContratistaAuthService authService,
            ILogger<EmpresaContratistaController> logger)
        {
            _repo = repo;
            _authService = authService;
            _logger = logger;
        }

        public class AddProyectoRequest
        {
            public int ProyectoId { get; set; }
        }

        [HttpGet("mi-proyecto-actual")]
        public async Task<IActionResult> GetMiProyectoActual()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? proyectoId = int.TryParse(userIdClaim, out var userId)
                    ? await _repo.GetProyectoActualDeUsuarioAsync(userId)
                    : null;
                return Ok(new { proyectoId });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.GetMiProyectoActual"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] string? search,
            [FromQuery] bool? activo,
            [FromQuery] bool? soloContratistas,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var (items, total) = await _repo.GetPagedAsync(search, activo, soloContratistas, page, pageSize);

                return Ok(new PagedResult<EmpresaContratistaListDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Data = items
                });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.GetPaged"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var empresa = await _repo.GetByIdAsync(id);
                if (empresa is null)
                    return NotFound(new { message = "Empresa no encontrada." });

                return Ok(empresa);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.GetById"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] EmpresaContratistaCreateDto dto)
        {
            try
            {
                if (await _repo.ExisteRucAsync(dto.Ruc ?? string.Empty))
                    return StatusCode(400, new { message = "Ya existe una empresa contratista registrada con ese RUC." });

                var creada = await _repo.CreateAsync(dto);

                try
                {
                    await _authService.SolicitarActivacionAsync(creada.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Empresa {Id} creada pero falló el envío del correo de activación.", creada.Id);
                }

                return StatusCode(201, creada);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmpresaContratistaUpdateDto dto)
        {
            try
            {
                var empresa = await _repo.GetByIdAsync(id);
                if (empresa is null)
                    return NotFound(new { message = "Empresa no encontrada." });

                await _repo.UpdateAsync(id, dto);
                return Ok(await _repo.GetByIdAsync(id));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.Update"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPatch("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] EmpresaPasswordUpdateDto dto)
        {
            try
            {
                var empresa = await _repo.GetByIdAsync(id);
                if (empresa is null)
                    return NotFound(new { message = "Empresa no encontrada." });

                await _repo.UpdatePasswordAsync(id, BCrypt.Net.BCrypt.HashPassword(dto.Password));
                return Ok(new { message = "Contraseña actualizada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.UpdatePassword"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:int}/reenviar-activacion")]
        public async Task<IActionResult> ReenviarActivacion(int id)
        {
            try
            {
                var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
                string[] permitidos = [Roles.AdministradorSsoma, Roles.AdministradorUdp];
                if (!roles.Any(r => permitidos.Contains(r, StringComparer.OrdinalIgnoreCase)))
                    return StatusCode(403, new { message = "Solo ADMINISTRADOR SSOMA o ADMINISTRADOR DE UDP pueden reenviar la activación." });

                await _authService.SolicitarActivacionAsync(id);
                return Ok(new { message = "Enlace de activación enviado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.ReenviarActivacion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}/proyectos")]
        public async Task<IActionResult> GetProyectos(int id)
        {
            try
            {
                var lista = await _repo.GetProyectosAsync(id);
                var result = lista.Select(ep => new
                {
                    id = ep.Id,
                    empresaId = ep.EmpresaId,
                    proyectoId = ep.ProyectoId,
                    proyectoNombre = ep.Proyecto?.ProjectDescription,
                    activo = ep.Activo,
                    fechaInicio = ep.FechaInicio,
                    fechaFin = ep.FechaFin,
                    createdAt = ep.CreatedAt
                }).ToList();
                return Ok(result);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.GetProyectos"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("{id:int}/proyectos")]
        public async Task<IActionResult> AddProyecto(int id, [FromBody] AddProyectoRequest body)
        {
            try
            {
                var empresa = await _repo.GetByIdAsync(id);
                if (empresa is null)
                    return NotFound(new { message = "Empresa no encontrada." });

                var ep = new SsEmpresaProyecto
                {
                    EmpresaId = id,
                    ProyectoId = body.ProyectoId,
                    Activo = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddProyectoAsync(ep);
                return StatusCode(201, new { message = "Proyecto vinculado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.AddProyecto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpDelete("{id:int}/proyectos/{proyectoId:int}")]
        public async Task<IActionResult> RemoveProyecto(int id, int proyectoId)
        {
            try
            {
                await _repo.RemoveProyectoAsync(id, proyectoId);
                return Ok(new { message = "Vinculación eliminada." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EmpresaContratistaController.RemoveProyecto"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
