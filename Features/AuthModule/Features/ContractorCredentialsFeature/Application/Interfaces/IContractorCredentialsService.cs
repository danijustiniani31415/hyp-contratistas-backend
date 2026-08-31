using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Dtos;

namespace Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Interfaces
{
    public interface IContractorCredentialsService
    {
        Task<ContractorTokenValidationDto> ValidateToken(string token);
        Task Create(ContractorCredentialsCreateDto dto);
    }
}
