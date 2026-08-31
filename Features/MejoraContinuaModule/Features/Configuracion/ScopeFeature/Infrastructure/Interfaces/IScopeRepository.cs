using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Dtos;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Infrastructure.Interfaces
{
    public interface IScopeRepository
    {
        Task<List<ScopeItemDTO>> GetScopeTreeAsync(int lessonAreaId);
        Task UpsertScopeAsync(ScopeItemUpsertDTO dto);
        Task<List<ScopeTemplateDTO>> GetTemplatesAsync();
        Task CreateTemplateAsync(ScopeTemplateCreateDTO dto);
        Task UpdateTemplateAsync(ScopeTemplateUpdateDTO dto);
        Task DeleteTemplateAsync(int scopeTemplateId);
        Task<List<ScopeItemDTO>> GetScopeForLessonAsync(int lessonAreaId);
    }
}
