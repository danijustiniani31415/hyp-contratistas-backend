using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    [ApiController]
    [Route("api/v1/evaluaciones/residentes")]
    [Authorize]
    public class EvEvaluacionResidenteController : ControllerBase
    {
        private readonly IEvEvaluacionResidenteRepository _repo;
        private readonly IEvPeriodoRepository _periodoRepo;
        private readonly ILogger<EvEvaluacionResidenteController> _logger;

        public EvEvaluacionResidenteController(
            IEvEvaluacionResidenteRepository repo,
            IEvPeriodoRepository periodoRepo,
            ILogger<EvEvaluacionResidenteController> logger)
        {
            _repo = repo;
            _periodoRepo = periodoRepo;
            _logger = logger;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EvEvaluacionCreateDto dto)
        {
            try
            {
                var periodo = await _periodoRepo.GetActivoAsync()
                    ?? throw new AbrilException("No hay período activo.", 400);

                if (DateTime.Today < periodo.FechaApertura.ToDateTime(TimeOnly.MinValue) ||
                    DateTime.Today > periodo.FechaCierre.ToDateTime(TimeOnly.MinValue))
                    throw new AbrilException("El período de evaluación no está activo.", 400);

                if (!dto.NoAplica && dto.Detalles.Any(d => !d.EsNa && (d.Puntaje is null or < 1 or > 5)))
                    throw new AbrilException("El puntaje debe estar entre 1 y 5.", 400);

                var userId = GetUserId();
                var existe = await _repo.ExisteAsync(periodo.Id, userId, dto.EvaluadoUserId, dto.AreaNombre);
                if (existe)
                    throw new AbrilException("Ya evaluaste a este residente en esta área este período.", 409);

                var eval = new EvEvaluacionResidente
                {
                    PeriodoId = periodo.Id,
                    EvaluadorUserId = userId,
                    EvaluadoUserId = dto.EvaluadoUserId,
                    ProjectId = dto.ProjectId,
                    AreaNombre = dto.AreaNombre,
                    Comentario = dto.Comentario,
                    NoAplica = dto.NoAplica,
                    NoAplicaMotivo = dto.NoAplicaMotivo
                };
                var detalles = dto.Detalles.Select(d => new EvEvaluacionResidenteDetalle
                {
                    PlantillaId = d.PlantillaId,
                    Criterio = d.Criterio,
                    Puntaje = d.Puntaje,
                    EsNa = d.EsNa
                }).ToList();

                var result = await _repo.CreateAsync(eval, detalles);
                return StatusCode(201, new { result.Id, result.Nota, message = "Evaluación guardada correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvEvaluacionResidenteController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        /// <summary>
        /// Corrige una evaluación ya enviada: solo el mismo evaluador, solo dentro de las 24h
        /// siguientes a haberla creado, solo mientras el período siga activo. Reemplaza la nota
        /// (no queda historial) y no deja ninguna marca de que fue editada.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] EvEvaluacionCreateDto dto)
        {
            try
            {
                var periodo = await _periodoRepo.GetActivoAsync()
                    ?? throw new AbrilException("No hay período activo.", 400);

                if (DateTime.Today < periodo.FechaApertura.ToDateTime(TimeOnly.MinValue) ||
                    DateTime.Today > periodo.FechaCierre.ToDateTime(TimeOnly.MinValue))
                    throw new AbrilException("El período de evaluación no está activo.", 400);

                if (!dto.NoAplica && dto.Detalles.Any(d => !d.EsNa && (d.Puntaje is null or < 1 or > 5)))
                    throw new AbrilException("El puntaje debe estar entre 1 y 5.", 400);

                var existente = await _repo.GetByIdAsync(id)
                    ?? throw new AbrilException("Evaluación no encontrada.", 404);

                var userId = GetUserId();
                if (existente.EvaluadorUserId != userId)
                    throw new AbrilException("Solo puedes corregir evaluaciones que registraste tú.", 403);

                if (DateTime.UtcNow - existente.CreatedAt > TimeSpan.FromHours(24))
                    throw new AbrilException("Ya pasaron más de 24 horas desde que la registraste — no se puede corregir.", 400);

                var puntajesValidos = dto.Detalles
                    .Where(d => !d.EsNa && d.Puntaje.HasValue)
                    .Select(d => d.Puntaje!.Value)
                    .ToList();
                var nota = puntajesValidos.Count != 0 ? Math.Round((decimal)puntajesValidos.Average() * 4, 2) : 0;

                var detalles = dto.Detalles.Select(d => new EvEvaluacionResidenteDetalle
                {
                    PlantillaId = d.PlantillaId,
                    Criterio = d.Criterio,
                    Puntaje = d.Puntaje,
                    EsNa = d.EsNa
                }).ToList();

                await _repo.UpdateAsync(id, nota, dto.Comentario, dto.NoAplica, dto.NoAplicaMotivo, detalles);
                var result = await _repo.GetDetalleAsync(id);
                return Ok(new { result!.Id, result.Nota, message = "Evaluación corregida correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvEvaluacionResidenteController.Update"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("periodo/{periodoId:int}")]
        public async Task<IActionResult> GetByPeriodo(int periodoId)
        {
            try { return Ok(await _repo.GetByPeriodoAsync(periodoId)); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvEvaluacionResidenteController.GetByPeriodo"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("mis-evaluaciones")]
        public async Task<IActionResult> GetMisEvaluaciones()
        {
            try
            {
                var periodo = await _periodoRepo.GetActivoAsync();
                if (periodo == null) return Ok(Array.Empty<object>());
                return Ok(await _repo.GetByEvaluadorAsync(GetUserId(), periodo.Id));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvEvaluacionResidenteController.GetMisEvaluaciones"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("mi-perfil")]
        public async Task<IActionResult> GetMiPerfil()
        {
            try
            {
                var periodo = await _periodoRepo.GetActivoAsync();
                if (periodo == null) return Ok(Array.Empty<object>());
                return Ok(await _repo.GetByEvaluadoAsync(GetUserId(), periodo.Id));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvEvaluacionResidenteController.GetMiPerfil"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("mi-subarea")]
        public async Task<IActionResult> GetMiSubarea()
        {
            try
            {
                var subarea = await _repo.GetMiSubareaAsync(GetUserId());
                return Ok(new { subarea });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvEvaluacionResidenteController.GetMiSubarea"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("residentes-evaluables")]
        public async Task<IActionResult> GetResidentesEvaluables()
        {
            try { return Ok(await _repo.GetResidentesEvaluablesAsync(GetUserId())); }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvEvaluacionResidenteController.GetResidentesEvaluables"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetalle(int id)
        {
            try
            {
                var r = await _repo.GetDetalleAsync(id);
                return r == null ? NotFound(new { message = "Evaluación no encontrada." }) : Ok(r);
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvEvaluacionResidenteController.GetDetalle"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
