using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Dtos;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Services
{
    public class ScopeService : IScopeService
    {
        private readonly IScopeRepository _repo;
        public ScopeService(IScopeRepository repo) => _repo = repo;

        public Task<List<ScopeItemDTO>> GetScopeTreeAsync(int lessonAreaId) => _repo.GetScopeTreeAsync(lessonAreaId);
        public Task UpsertScopeAsync(ScopeItemUpsertDTO dto) => _repo.UpsertScopeAsync(dto);
        public Task<List<ScopeTemplateDTO>> GetTemplatesAsync() => _repo.GetTemplatesAsync();
        public Task CreateTemplateAsync(ScopeTemplateCreateDTO dto) => _repo.CreateTemplateAsync(dto);
        public Task UpdateTemplateAsync(ScopeTemplateUpdateDTO dto) => _repo.UpdateTemplateAsync(dto);
        public Task DeleteTemplateAsync(int scopeTemplateId) => _repo.DeleteTemplateAsync(scopeTemplateId);
        public Task<List<ScopeItemDTO>> GetScopeForLessonAsync(int lessonAreaId) => _repo.GetScopeForLessonAsync(lessonAreaId);
    }
}
