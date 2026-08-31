using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.Sunat.Interfaces;

namespace Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repository;
        private readonly ISunatService _sunatService;

        public ProjectService(IProjectRepository repository, ISunatService sunatService)
        {
            _repository = repository;
            _sunatService = sunatService;
        }

        public async Task<PagedResult<ProjectDto>> GetPaged(int page, int pageSize, string? ruc = null, string? razonSocial = null, string? projectDescription = null, bool? active = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 200;
            return await _repository.GetPaged(page, pageSize, ruc, razonSocial, projectDescription, active);
        }

        public async Task Create(ProjectCreateDto dto, int userId)
        {
            await _repository.Create(dto, userId);
        }

        public async Task Update(ProjectEditDto dto, int userId)
        {
            await _repository.Update(dto, userId);
        }

        public async Task<bool> DeleteSoftAsync(int projectId, int userId)
        {
            return await _repository.DeleteSoftAsync(projectId, userId);
        }

        public async Task<ContributorLookupDto?> GetOrCreateCompanyByRuc(string ruc, int userId)
        {
            var trimmed = ruc.Trim();

            var existing = await _repository.FindContributorByRuc(trimmed);
            if (existing != null)
            {
                // If location fields are missing, enrich from SUNAT and persist
                if (existing.ContributorDistrict == null || existing.ContributorProvince == null || existing.ContributorDepartment == null)
                {
                    var sunatData = await _sunatService.GetByRucAsync(trimmed);
                    if (sunatData != null)
                    {
                        await _repository.UpdateContributorLocationAsync(
                            existing.ContributorId,
                            sunatData.ContributorDistrict,
                            sunatData.ContributorProvince,
                            sunatData.ContributorDepartment);

                        existing.ContributorDistrict = sunatData.ContributorDistrict;
                        existing.ContributorProvince = sunatData.ContributorProvince;
                        existing.ContributorDepartment = sunatData.ContributorDepartment;
                    }
                }

                return new ContributorLookupDto
                {
                    ContributorId = existing.ContributorId,
                    ContributorRuc = existing.ContributorRuc,
                    ContributorName = existing.ContributorName,
                    ContributorAddress = existing.ContributorAddress,
                    ContributorDistrict = existing.ContributorDistrict,
                    ContributorProvince = existing.ContributorProvince,
                    ContributorDepartment = existing.ContributorDepartment,
                    LegalEntityRegistryNumber = existing.LegalEntityRegistryNumber
                };
            }

            var sunat = await _sunatService.GetByRucAsync(trimmed);
            if (sunat == null)
                return null;

            var created = await _repository.CreateContributor(
                sunat.ContributorRuc,
                sunat.ContributorName,
                sunat.ContributorAddress,
                sunat.ContributorEconomicActivityDescription,
                sunat.ContributorDistrict,
                sunat.ContributorProvince,
                sunat.ContributorDepartment,
                userId);

            return new ContributorLookupDto
            {
                ContributorId = created.ContributorId,
                ContributorRuc = created.ContributorRuc,
                ContributorName = created.ContributorName,
                ContributorAddress = created.ContributorAddress,
                ContributorDistrict = created.ContributorDistrict,
                ContributorProvince = created.ContributorProvince,
                ContributorDepartment = created.ContributorDepartment,
                LegalEntityRegistryNumber = created.LegalEntityRegistryNumber
            };
        }

        public async Task UpdateEmails(int id, ProjectEmailsUpdateDto dto)
        {
            await _repository.UpdateEmails(id, dto);
        }

        public async Task<ProjectEmailsDto?> GetEmails(int projectId)
        {
            return await _repository.GetEmails(projectId);
        }

        public async Task<bool?> ToggleArquitecturaComercial(int projectId)
        {
            return await _repository.ToggleArquitecturaComercial(projectId);
        }

        public async Task<List<ResponsableLookupDto>> GetResponsables(string tipo)
        {
            return await _repository.GetResponsables(tipo);
        }

        public async Task<List<int>> GetMyProjectIds(int userId)
        {
            return await _repository.GetMyProjectIds(userId);
        }
    }
}
