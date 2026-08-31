using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Interfaces
{
    public interface IChecklistRepository
    {
        // --- Plantillas (catálogo maestro) ---
        Task<List<ChecklistPlantillaListDto>> GetPlantillasAsync();
        Task<ChecklistPlantillaDetalleDto?> GetPlantillaDetalleAsync(int plantillaId);
        Task<SsChecklistPlantilla> CreatePlantillaAsync(ChecklistPlantillaUpsertDto dto, int userId);
        Task UpdatePlantillaAsync(int plantillaId, ChecklistPlantillaUpsertDto dto);
        Task<SsChecklistPlantillaItem> AddItemToPlantillaAsync(int plantillaId, ChecklistPlantillaItemCreateDto dto);
        Task UpdatePlantillaItemAsync(int itemId, ChecklistPlantillaItemEditDto dto);

        // --- Checklists de proyecto ---
        Task<ChecklistProyectoResumenDto> GetResumenProyectoAsync(int proyectoId);
        Task<ChecklistProyectoDetalleDto?> GetChecklistDetalleAsync(int checklistProyectoId);
        Task<SsChecklistProyecto> ActivarChecklistAsync(int proyectoId, int plantillaId, int userId);
        Task SeedChecklistsObligatoriosAsync(int proyectoId, int userId);

        // --- Items de proyecto ---
        Task<(decimal porcentaje, bool recienCompletado)> ToggleItemAsync(int checklistProyectoItemId, ChecklistItemToggleDto dto, int userId);

        // Para la notificación: obtener email del gerente y datos del proyecto
        Task<(string? emailGerente, string nombreProyecto, string nombreChecklist)> GetDatosNotificacionAsync(int checklistProyectoId);
        Task MarcarNotificacionEnviadaAsync(int checklistProyectoId);

        // Retorna el checklistProyectoId dueño del item
        Task<int> GetChecklistProyectoIdByItemAsync(int checklistProyectoItemId);
    }
}
