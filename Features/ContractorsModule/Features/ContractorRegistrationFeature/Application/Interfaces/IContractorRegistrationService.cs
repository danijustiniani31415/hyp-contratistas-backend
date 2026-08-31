using Abril_Backend.Features.Contractors.ContractorRegistration.Application.Dtos;
using Abril_Backend.Shared.Services.Sunat.Dtos;

namespace Abril_Backend.Features.Contractors.ContractorRegistration.Application.Interfaces
{
    public interface IContractorRegistrationService
    {
        Task<List<ContractorPersonTypeDto>> GetPersonTypes();
        Task Create(ContributorCreateDto dto, int? userId, string? accessToken = null);
        Task<SunatContributorDto?> GetByRuc(string ruc);
        Task<ContractorRucStatusDto> GetRucStatus(string ruc);
    }
}
