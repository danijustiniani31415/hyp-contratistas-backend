using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Application.Interfaces
{
    public interface ILessonAreaService
    {
        Task<List<LessonAreaConfigItemDTO>> GetAllAsync();
        Task<List<LessonAreaConfigItemDTO>> GetAllWithScopeAsync();
        Task<List<LessonAreaConfigItemDTO>> GetAllForFilterAsync();
        Task<ToggleLessonAreaResultDTO> ToggleAsync(int areaScopeId);
        Task<SetLessonAreaFlagResultDTO> SetIncludeInFormAsync(int areaScopeId, bool value);
        Task<SetLessonAreaFlagResultDTO> SetIncludeDescendantsAsync(int areaScopeId, bool value);
        Task<SetLessonAreaFlagResultDTO> SetIncludeAsIndependentAsync(int areaScopeId, bool value);
    }
}
