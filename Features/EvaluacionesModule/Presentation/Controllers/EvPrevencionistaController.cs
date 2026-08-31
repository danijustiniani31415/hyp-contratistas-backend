using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    // Flujo C: el supervisor de campo del contratista (sesión tipo=CONTRATISTA, login
    // ya existente en el portal dashboard-contratista) evalúa al Prevencionista/
    // Coordinador SSOMA asignado a su(s) proyecto(s). El evaluado ve su propio
    // promedio y comentarios en /mi-perfil, sin nunca recibir la identidad del
    // contratista que lo calificó; el Jefe SSOMA sí la ve en /dashboard.
    [ApiController]
    [Route("api/v1/evaluaciones/prevencionistas")]
    [Authorize]
    public class EvPrevencionistaController : ControllerBase
    {
        private readonly IEvPrevencionistaRepository _repo;
        private readonly IEvPeriodoRepository _periodoRepo;
        private readonly ILogger<EvPrevencionistaController> _logger;

        public EvPrevencionistaController(
            IEvPrevencionistaRepository repo,
            IEvPeriodoRepository periodoRepo,
            ILogger<EvPrevencionistaController> logger)
        {
            _repo = repo;
            _periodoRepo = periodoRepo;
            _logger = logger;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        private int GetEmpresaId() =>
            int.TryParse(User.FindFirst("empresaId")?.Value, out var id) ? id : 0;


        [HttpGet("inicio")]
        [Authorize(Roles = Roles.Contratista)]
        public async Task<IActionResult> GetInicio()
        {
            try
            {
                var proyectoIds = await _repo.ResolverProyectoIdsActualesAsync(GetUserId(), GetEmpresaId());
                return Ok(await _repo.GetInicioAsync(GetUserId(), GetEmpresaId(), proyectoIds));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPrevencionistaController.GetInicio"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        [Authorize(Roles = Roles.Contratista)]
        public async Task<IActionResult> Create([FromBody] EvPrevencionistaEvaluacionCreateDto dto)
        {
            try
            {
                var periodo = await _periodoRepo.GetActivoAsync()
                    ?? throw new AbrilException("No hay período de evaluación activo.", 400);

                if (dto.Detalles.Count == 0)
                    throw new AbrilException("Debe calificar todos los criterios.", 400);

                if (dto.Detalles.Any(d => d.Puntaje is < 1 or > 5))
                    throw new AbrilException("El puntaje debe estar entre 1 y 5.", 400);

                var proyectoIds = await _repo.ResolverProyectoIdsActualesAsync(GetUserId(), GetEmpresaId());
                if (!proyectoIds.Contains(dto.ProyectoId))
                    throw new AbrilException("No tiene acceso a este proyecto.", 403);

                var empresaId = GetEmpresaId();
                var evaluadorSsUsuarioId = await _repo.ResolverEvaluadorSsUsuarioIdAsync(GetUserId(), empresaId)
                    ?? throw new AbrilException("No se pudo identificar su usuario de contratista.", 400);

                var existe = await _repo.ExisteAsync(periodo.Id, dto.EvaluadoUserId, dto.ProyectoId, evaluadorSsUsuarioId);
                if (existe)
                    throw new AbrilException("Ya evaluó a esta persona en este proyecto en este período.", 409);

                var eval = new EvEvaluacionPrevencionista
                {
                    PeriodoId = periodo.Id,
                    ProyectoId = dto.ProyectoId,
                    EvaluadoUserId = dto.EvaluadoUserId,
                    EvaluadorContributorId = empresaId,
                    EvaluadorSsContratistaUsuarioId = evaluadorSsUsuarioId,
                    Comentario = dto.Comentario
                };

                var detalles = dto.Detalles.Select(d => new EvEvaluacionPrevencionistaDetalle
                {
                    PlantillaId = d.PlantillaId,
                    Criterio = d.Criterio,
                    Puntaje = d.Puntaje
                }).ToList();

                var result = await _repo.CreateAsync(eval, detalles);
                return StatusCode(201, new { result.Id, result.Nota, message = "Evaluación registrada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPrevencionistaController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("mi-perfil")]
        [Authorize]
        public async Task<IActionResult> GetMiPerfil([FromQuery] int? periodoId)
        {
            try
            {
                var userId = GetUserId();
                var categoria = await _repo.ObtenerCategoriaPuestoAsync(userId);
                if (categoria != CategoriaIds.CoordinadorSsoma && categoria != CategoriaIds.Prevencionista)
                    return StatusCode(403, new { message = "No tiene acceso a esta pantalla." });

                return Ok(await _repo.GetMiPerfilAsync(userId, periodoId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPrevencionistaController.GetMiPerfil"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("dashboard")]
        [Authorize]
        public async Task<IActionResult> GetDashboard([FromQuery] int? periodoId, [FromQuery] int? proyectoId)
        {
            try
            {
                if (!await _repo.EsJefeSsomaAsync(GetUserId()))
                    return StatusCode(403, new { message = "No tiene acceso a esta pantalla." });

                return Ok(await _repo.GetDashboardAsync(periodoId, proyectoId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvPrevencionistaController.GetDashboard"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
