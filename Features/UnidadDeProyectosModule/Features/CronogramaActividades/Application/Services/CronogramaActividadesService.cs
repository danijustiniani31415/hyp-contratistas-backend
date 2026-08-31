using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Services
{
    public class CronogramaActividadesService : ICronogramaActividadesService
    {
        private readonly ICronogramaActividadesRepository _repository;
        private readonly ICronogramaSchedulingService _scheduling;

        public CronogramaActividadesService(
            ICronogramaActividadesRepository repository,
            ICronogramaSchedulingService scheduling)
        {
            _repository = repository;
            _scheduling = scheduling;
        }

        public Task<List<ProyectoSimpleCronogramaDto>> GetProyectosAsync()
            => _repository.GetProyectosAsync();

        public Task<ActividadesProyectoResponseDto> GetActividadesAsync(int proyectoId, string tipoCronograma = "ANTEPROYECTO")
            => _repository.GetActividadesAsync(proyectoId, tipoCronograma);

        public async Task<CrearActividadResultDto> CrearActividadAsync(int proyectoId, CrearActividadRequest request, int userId)
        {
            var actividad = await _repository.CrearActividadAsync(proyectoId, request, userId);
            var padres = await _scheduling.RecalcularFechasPadresAsync(proyectoId);
            return new CrearActividadResultDto
            {
                Actividad = actividad,
                PadresActualizados = padres.Count > 0 ? padres : null
            };
        }

        public async Task<EditarActividadResultDto> EditarActividadAsync(int projectActivityId, EditarActividadRequest request, int userId)
        {
            var actividad = await _repository.EditarActividadAsync(projectActivityId, request, userId);
            var padres = await _scheduling.RecalcularFechasPadresAsync(actividad.ProjectId);

            if (request.PredecessorIds != null)
            {
                var limpias = request.PredecessorIds
                    .Where(p => p != projectActivityId)
                    .Distinct()
                    .ToList();

                if (await _scheduling.DetectCycleAsync(actividad.ProjectId, projectActivityId, limpias))
                    throw new AbrilException(
                        "La dependencia genera un ciclo entre actividades y no es válida.", 400);

                await _repository.SetPredecesorasAsync(projectActivityId, limpias);
                actividad.Predecesoras = limpias;
            }

            // La cascada debe recalcularse siempre que se editan fechas o predecesoras:
            // mover la fecha de una actividad puede afectar a sus sucesoras (vía
            // ActivityPredecessors) aunque este request no haya tocado PredecessorIds.
            CascadaResultDto? cascada = null;
            if (request.PredecessorIds != null || request.PlannedStartDate.HasValue || request.PlannedEndDate.HasValue)
            {
                var resultado = await _scheduling.AplicarCascadaAsync(actividad.ProjectId);
                if (resultado.Cambios.Count > 0)
                {
                    cascada = resultado;

                    // La cascada puede haber movido la propia actividad editada (p. ej. al
                    // asignarle una predecesora): reflejar sus fechas post-cascada, no las
                    // pre-cascada que trae "actividad" desde EditarActividadAsync.
                    var cambioPropio = resultado.Cambios.FirstOrDefault(c => c.ProjectActivityId == projectActivityId);
                    if (cambioPropio != null)
                    {
                        actividad.PlannedStartDate = cambioPropio.InicioNuevo;
                        actividad.PlannedEndDate = cambioPropio.FinNuevo;
                    }
                }
            }

            return new EditarActividadResultDto
            {
                Actividad = actividad,
                Cascada = cascada,
                PadresActualizados = padres.Count > 0 ? padres : null
            };
        }

        public Task<CulminarActividadDto> CulminarActividadAsync(int projectActivityId, int userId)
            => _repository.CulminarActividadAsync(projectActivityId, userId);

        public Task EliminarActividadAsync(int projectActivityId, int userId)
            => _repository.EliminarActividadAsync(projectActivityId, userId);

        public Task<List<DebugProyectoDto>> GetDebugProyectosAsync()
            => _repository.GetDebugProyectosAsync();

        public async Task<ImportarMppResultDto> ImportarMppAsync(int proyectoId, IFormFile archivo, int userId, string tipoCronograma = "ANTEPROYECTO")
        {
            var result = await _repository.ImportarMppAsync(proyectoId, archivo, userId, tipoCronograma);
            // Tras importar, los nodos padre deben reflejar MIN/MAX de sus hijos (cualquier nivel)
            await _scheduling.RecalcularFechasPadresAsync(proyectoId);
            return result;
        }

        public Task<List<ActividadDto>> ReordenarActividadesAsync(int proyectoId, List<ReordenarItem> items)
            => _repository.ReordenarActividadesAsync(proyectoId, items);

        public Task<List<ActividadDto>> CambiarJerarquiaAsync(int proyectoId, CambiarJerarquiaRequest request)
            => _repository.CambiarJerarquiaAsync(proyectoId, request);

        public Task<List<ActividadDto>> SubirNivelAsync(int proyectoId, int actividadId)
            => _repository.SubirNivelAsync(proyectoId, actividadId);

        public Task<List<ActividadDto>> BajarNivelAsync(int proyectoId, int actividadId)
            => _repository.BajarNivelAsync(proyectoId, actividadId);

        // ─────────────────────────── Feriados ───────────────────────────

        public Task<List<FeriadoDto>> GetFeriadosAsync()
            => _repository.GetFeriadosAsync();

        public Task<FeriadoDto> CrearFeriadoAsync(CrearFeriadoRequest request)
            => _repository.CrearFeriadoAsync(request);

        public Task EliminarFeriadoAsync(int id)
            => _repository.EliminarFeriadoAsync(id);

        // ─────────────────────────── Predecesoras + cascada ───────────────────────────

        public Task<ActividadDto> ActualizarLineaBaseAsync(int projectActivityId, ActualizarLineaBaseRequest request, int userId)
            => _repository.ActualizarLineaBaseAsync(projectActivityId, request, userId);

        public async Task<ActualizarPredecesorasResultDto> ActualizarPredecesorasAsync(int activityId, List<int> predecessorIds)
        {
            var proyectoId = await _repository.GetProyectoIdDeActividadAsync(activityId);

            var limpias = (predecessorIds ?? new List<int>())
                .Where(p => p != activityId).Distinct().ToList();

            // Bloquear dependencias circulares antes de persistir
            if (await _scheduling.DetectCycleAsync(proyectoId, activityId, limpias))
                throw new AbrilException(
                    "La dependencia genera un ciclo entre actividades y no es válida.", 400);

            await _repository.SetPredecesorasAsync(activityId, limpias);

            // Preview de la cascada que se aplicaría (sin persistir todavía)
            var preview = await _scheduling.RecalcularCascadaAsync(proyectoId);

            return new ActualizarPredecesorasResultDto
            {
                ProjectActivityId = activityId,
                Predecesoras = await _repository.GetPredecesorasAsync(activityId),
                PreviewCascada = preview
            };
        }

        public Task<CascadaResultDto> PreviewCascadaAsync(int proyectoId)
            => _scheduling.RecalcularCascadaAsync(proyectoId);

        public Task<CascadaResultDto> AplicarCascadaAsync(int proyectoId)
            => _scheduling.AplicarCascadaAsync(proyectoId);

        public Task<CronogramaDashboardResponseDto> GetDashboardAsync(int? responsableId, string? estado)
            => _repository.GetDashboardAsync(responsableId, estado);

        public Task<CrearActividadesMasivoResultDto> CrearActividadesMasivoAsync(int proyectoId, CrearActividadesMasivoRequest request, int userId)
            => _repository.CrearActividadesMasivoAsync(proyectoId, request, userId);

        // ─────────────────────────── Última pestaña ───────────────────────────

        public async Task<UltimaPestanaDto> GetUltimaPestanaAsync(int proyectoId, int userId)
        {
            var tipoCronograma = await _repository.GetUltimaPestanaAsync(proyectoId, userId);
            return new UltimaPestanaDto { TipoCronograma = tipoCronograma };
        }

        public Task ActualizarUltimaPestanaAsync(int proyectoId, int userId, ActualizarUltimaPestanaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TipoCronograma))
                throw new AbrilException("El tipo de cronograma es obligatorio.", 400);

            return _repository.ActualizarUltimaPestanaAsync(proyectoId, userId, request.TipoCronograma);
        }

        // ─────────────────────────── Plantilla ───────────────────────────

        public Task<AplicarPlantillaResultDto> AplicarPlantillaAsync(int proyectoId, AplicarPlantillaRequest request, int userId)
        {
            if (string.IsNullOrWhiteSpace(request.TipoCronograma))
                throw new AbrilException("El tipo de cronograma es obligatorio.", 400);

            return _repository.AplicarPlantillaAsync(proyectoId, request.TipoCronograma, userId);
        }
    }
}
