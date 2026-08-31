using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Services
{
    public class ProjectLinkService : IProjectLinkService
    {
        private readonly IProjectLinkRepository _repository;

        public ProjectLinkService(IProjectLinkRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Proyectos permitidos para el usuario (null = sin restricción).
        /// Oficina Técnica solo opera sobre los proyectos donde su correo está
        /// registrado en Configuración → Correo de staff por proyecto.
        /// </summary>
        private async Task<List<int>?> GetAllowedProjectIdsAsync(int userId, bool restrictToOwnProjects)
        {
            if (!restrictToOwnProjects) return null;
            return await _repository.GetUserProjectIdsAsync(userId);
        }

        private static AbrilException NotOwnProject() =>
            new(
                "Solo puede gestionar links de los proyectos que tiene asignados. " +
                "Verifique que su correo esté registrado para el proyecto en Configuración → Correo de staff por proyecto.", 422);

        public async Task<PagedResult<ProjectLinkDto>> GetPaged(ProjectLinkFilterDto filter, int userId, bool restrictToOwnProjects)
        {
            if (filter.Page < 1) filter.Page = 1;
            var allowed = await GetAllowedProjectIdsAsync(userId, restrictToOwnProjects);
            return await _repository.GetPaged(filter, allowed);
        }

        public async Task<ProjectLinkFormDataDto> GetFormData(int userId, bool restrictToOwnProjects)
        {
            var allowed = await GetAllowedProjectIdsAsync(userId, restrictToOwnProjects);
            return await _repository.GetFormData(allowed);
        }

        public async Task Create(ProjectLinkCreateDto dto, int userId, bool restrictToOwnProjects)
        {
            var allowed = await GetAllowedProjectIdsAsync(userId, restrictToOwnProjects);
            if (allowed != null && !allowed.Contains(dto.ProjectId))
                throw NotOwnProject();

            await _repository.Create(dto, userId);
        }

        public async Task Update(ProjectLinkUpdateDto dto, int userId, bool restrictToOwnProjects)
        {
            var allowed = await GetAllowedProjectIdsAsync(userId, restrictToOwnProjects);
            if (allowed != null)
            {
                var projectId = await _repository.GetProjectIdAsync(dto.ProjectLinkId)
                    ?? throw new AbrilException("El link no existe.");
                if (!allowed.Contains(projectId))
                    throw NotOwnProject();
            }

            await _repository.Update(dto, userId);
        }

        public async Task<bool> Delete(int projectLinkId, int userId, bool restrictToOwnProjects)
        {
            var allowed = await GetAllowedProjectIdsAsync(userId, restrictToOwnProjects);
            if (allowed != null)
            {
                var projectId = await _repository.GetProjectIdAsync(projectLinkId);
                if (projectId == null) return false;
                if (!allowed.Contains(projectId.Value))
                    throw NotOwnProject();
            }

            return await _repository.Delete(projectLinkId, userId);
        }
    }
}
