using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Interfaces
{
    public interface IChecklistService
    {
        // Plantillas
        Task<List<ChecklistPlantillaListDto>> GetPlantillasAsync();
        Task<ChecklistPlantillaDetalleDto?> GetPlantillaDetalleAsync(int plantillaId);
        Task<ChecklistPlantillaDetalleDto> CreatePlantillaAsync(ChecklistPlantillaUpsertDto dto, int userId);
        Task UpdatePlantillaAsync(int plantillaId, ChecklistPlantillaUpsertDto dto);
        Task<ChecklistPlantillaItemDto> AddItemToPlantillaAsync(int plantillaId, ChecklistPlantillaItemCreateDto dto);
        Task UpdatePlantillaItemAsync(int itemId, ChecklistPlantillaItemEditDto dto);

        // Proyecto
        Task<ChecklistProyectoResumenDto> GetResumenProyectoAsync(int proyectoId);
        Task<ChecklistProyectoDetalleDto?> GetChecklistDetalleAsync(int checklistProyectoId);
        Task<ChecklistProyectoDetalleDto> ActivarChecklistAsync(int proyectoId, int plantillaId, int userId);
        Task SeedChecklistsObligatoriosAsync(int proyectoId, int userId);

        // Items
        Task<(decimal porcentaje, string estado)> ToggleItemAsync(int checklistProyectoItemId, ChecklistItemToggleDto dto, int userId);
    }
}
