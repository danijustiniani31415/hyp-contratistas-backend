using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Dtos;

namespace Abril_Backend.Features.AuthModule.ContractorCredentials.Infrastructure.Interfaces
{
    public interface IContractorCredentialsRepository
    {
        Task<ContractorForCredentialsDto?> GetByToken(string token);
        Task Create(int contractorId, string email, string password);
    }
}
