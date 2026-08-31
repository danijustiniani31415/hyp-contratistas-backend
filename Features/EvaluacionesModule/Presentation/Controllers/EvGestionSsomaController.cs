using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Abril_Backend.Features.Evaluaciones.Presentation.Controllers
{
    // Flujo D: evaluaciones internas de gestión SSOMA (liderazgo/gestión de personas):
    //   D1. Jefe SSOMA          -> Prevencionistas (todos)
    //   D2. Jefe SSOMA          -> Coordinadores SSOMA (todos)
    //   D3. Coordinador SSOMA   -> Prevencionistas de su mismo proyecto
    //   D4. Prevencionista      -> su Coordinador SSOMA del mismo proyecto (ANÓNIMA)
    // D1-D3 son identificadas. D4 es anónima: ver nota en EvEvaluacionGestionSsoma.
    [ApiController]
    [Route("api/v1/evaluaciones/gestion-ssoma")]
    [Authorize]
    public class EvGestionSsomaController : ControllerBase
    {
        private readonly IEvGestionSsomaRepository _repo;
        private readonly IEvPeriodoRepository _periodoRepo;
        private readonly ILogger<EvGestionSsomaController> _logger;

        public EvGestionSsomaController(
            IEvGestionSsomaRepository repo,
            IEvPeriodoRepository periodoRepo,
            ILogger<EvGestionSsomaController> logger)
        {
            _repo = repo;
            _periodoRepo = periodoRepo;
            _logger = logger;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        /// <summary>
        /// Jefe SSOMA, Coordinador SSOMA y Prevencionista se resuelven TODOS por el puesto
        /// real (workers.puesto_id -> puesto.categoria_id / PuestoIds.JefeSsoma) — ningún
        /// user_role de por medio. El rol de sistema 9 (antes usado para Jefe SSOMA) estaba
        /// asignado a ~50 cuentas sin relación con SSOMA, así que no servía como filtro.
        /// </summary>
        private async Task<bool> ParticipaDeGestionSsomaAsync(int userId)
        {
            if (await _repo.EsJefeSsomaAsync(userId)) return true;
            var categoria = await _repo.ObtenerCategoriaPuestoAsync(userId);
            return categoria == CategoriaIds.CoordinadorSsoma || categoria == CategoriaIds.Prevencionista;
        }

        [HttpGet("inicio")]
        [Authorize]
        public async Task<IActionResult> GetInicio()
        {
            try
            {
                var userId = GetUserId();
                if (!await ParticipaDeGestionSsomaAsync(userId))
                    return StatusCode(403, new { message = "No tiene acceso a esta evaluación." });

                return Ok(await _repo.GetInicioAsync(userId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvGestionSsomaController.GetInicio"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] EvGestionSsomaEvaluacionCreateDto dto)
        {
            try
            {
                var userId = GetUserId();
                if (!await ParticipaDeGestionSsomaAsync(userId))
                    return StatusCode(403, new { message = "No tiene acceso a esta evaluación." });

                var periodo = await _periodoRepo.GetActivoAsync()
                    ?? throw new AbrilException("No hay período de evaluación activo.", 400);

                if (dto.Detalles.Count == 0)
                    throw new AbrilException("Debe calificar todos los criterios.", 400);

                if (dto.Detalles.Any(d => d.Puntaje is < 1 or > 5))
                    throw new AbrilException("El puntaje debe estar entre 1 y 5.", 400);

                var contexto = await _repo.ResolverContextoEvaluacionAsync(userId, dto.EvaluadoUserId);
                if (!contexto.Valido)
                    throw new AbrilException(contexto.Error ?? "No corresponde esta evaluación.", 400);

                var yaEvalue = contexto.EsAnonimo
                    ? await _repo.YaEvaluoAnonimoAsync(periodo.Id, userId)
                    : await _repo.ExisteAsync(periodo.Id, userId, contexto.EvaluadoUserId);
                if (yaEvalue)
                    throw new AbrilException("Ya registró esta evaluación en este período.", 409);

                var puntajes = dto.Detalles.Select(d => d.Puntaje).ToList();
                var nota = Math.Round((decimal)puntajes.Average() * 4, 2);
                var detalles = dto.Detalles.Select(d => (d.PlantillaId, d.Criterio, d.Puntaje)).ToList();

                if (contexto.EsAnonimo)
                    await _repo.RegistrarAnonimoAsync(
                        periodo.Id, userId, contexto.EvaluadoUserId, contexto.ProyectoId,
                        dto.Fortalezas, dto.OportunidadesMejora, detalles, nota);
                else
                    await _repo.RegistrarAsync(
                        periodo.Id, userId, contexto.EvaluadorRol, contexto.EvaluadoUserId, contexto.EvaluadoRol,
                        contexto.ProyectoId, dto.Fortalezas, dto.OportunidadesMejora, detalles, nota);

                return StatusCode(201, new { message = "Evaluación registrada correctamente. Gracias por tu respuesta." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvGestionSsomaController.Create"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("pendientes")]
        [Authorize]
        public async Task<IActionResult> GetPendientes([FromQuery] int? periodoId)
        {
            try
            {
                if (!await _repo.EsJefeSsomaAsync(GetUserId()))
                    return StatusCode(403, new { message = "No tiene acceso a esta pantalla." });

                var periodo = periodoId ?? (await _periodoRepo.GetActivoAsync())?.Id;
                if (periodo == null) return Ok(new EvGestionSsomaCumplimientoDto());
                return Ok(await _repo.GetCumplimientoAsync(periodo.Value));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvGestionSsomaController.GetPendientes"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpGet("resultados")]
        [Authorize]
        public async Task<IActionResult> GetResultados([FromQuery] int? periodoId)
        {
            try
            {
                if (!await _repo.EsJefeSsomaAsync(GetUserId()))
                    return StatusCode(403, new { message = "No tiene acceso a esta pantalla." });

                return Ok(await _repo.GetResultadosAsync(periodoId));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en EvGestionSsomaController.GetResultados"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }
    }
}
