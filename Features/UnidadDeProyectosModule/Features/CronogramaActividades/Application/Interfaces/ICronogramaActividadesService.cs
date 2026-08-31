using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Interfaces
{
    public interface ICronogramaActividadesService
    {
        Task<List<ProyectoSimpleCronogramaDto>> GetProyectosAsync();
        Task<ActividadesProyectoResponseDto> GetActividadesAsync(int proyectoId, string tipoCronograma = "ANTEPROYECTO");
        Task<CrearActividadResultDto> CrearActividadAsync(int proyectoId, CrearActividadRequest request, int userId);
        Task<EditarActividadResultDto> EditarActividadAsync(int projectActivityId, EditarActividadRequest request, int userId);
        Task<CulminarActividadDto> CulminarActividadAsync(int projectActivityId, int userId);
        Task EliminarActividadAsync(int projectActivityId, int userId);
        Task<List<DebugProyectoDto>> GetDebugProyectosAsync();
        Task<ImportarMppResultDto> ImportarMppAsync(int proyectoId, IFormFile archivo, int userId, string tipoCronograma = "ANTEPROYECTO");
        Task<List<ActividadDto>> ReordenarActividadesAsync(int proyectoId, List<ReordenarItem> items);
        Task<List<ActividadDto>> CambiarJerarquiaAsync(int proyectoId, CambiarJerarquiaRequest request);
        Task<List<ActividadDto>> SubirNivelAsync(int proyectoId, int actividadId);
        Task<List<ActividadDto>> BajarNivelAsync(int proyectoId, int actividadId);

        // Feriados
        Task<List<FeriadoDto>> GetFeriadosAsync();
        Task<FeriadoDto> CrearFeriadoAsync(CrearFeriadoRequest request);
        Task EliminarFeriadoAsync(int id);

        // Línea base
        Task<ActividadDto> ActualizarLineaBaseAsync(int projectActivityId, ActualizarLineaBaseRequest request, int userId);

        // Predecesoras + cascada
        Task<ActualizarPredecesorasResultDto> ActualizarPredecesorasAsync(int activityId, List<int> predecessorIds);
        Task<CascadaResultDto> PreviewCascadaAsync(int proyectoId);
        Task<CascadaResultDto> AplicarCascadaAsync(int proyectoId);

        // Dashboard
        Task<CronogramaDashboardResponseDto> GetDashboardAsync(int? responsableId, string? estado);

        // Creación masiva
        Task<CrearActividadesMasivoResultDto> CrearActividadesMasivoAsync(int proyectoId, CrearActividadesMasivoRequest request, int userId);

        // Última pestaña
        Task<UltimaPestanaDto> GetUltimaPestanaAsync(int proyectoId, int userId);
        Task ActualizarUltimaPestanaAsync(int proyectoId, int userId, ActualizarUltimaPestanaRequest request);

        // Plantilla
        Task<AplicarPlantillaResultDto> AplicarPlantillaAsync(int proyectoId, AplicarPlantillaRequest request, int userId);
    }
}
