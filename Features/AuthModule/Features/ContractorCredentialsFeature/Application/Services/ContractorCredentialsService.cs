using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Dtos;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Interfaces;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Infrastructure.Interfaces;

namespace Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Services
{
    public class ContractorCredentialsService : IContractorCredentialsService
    {
        private readonly IContractorCredentialsRepository _repo;

        public ContractorCredentialsService(IContractorCredentialsRepository repo)
        {
            _repo = repo;
        }

        public async Task<ContractorTokenValidationDto> ValidateToken(string token)
        {
            var contractor = await _repo.GetByToken(token);
            if (contractor == null)
                throw new AbrilException("Token inválido o expirado.", 400);

            return new ContractorTokenValidationDto
            {
                ContributorName = contractor.ContributorName,
                Emails = contractor.Emails
            };
        }

        public async Task Create(ContractorCredentialsCreateDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                throw new AbrilException("Las contraseñas no coinciden.", 400);

            var contractor = await _repo.GetByToken(dto.Token);
            if (contractor == null)
                throw new AbrilException("Token inválido o expirado.", 400);

            await _repo.Create(contractor.ContractorId, dto.Email, dto.Password);
        }
    }
}
