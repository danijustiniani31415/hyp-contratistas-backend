using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Services
{
    public class CostosPresupuestosEmailService : ICostosPresupuestosEmailService
    {
        private readonly ICostosPresupuestosEmailRepository _repository;

        public CostosPresupuestosEmailService(ICostosPresupuestosEmailRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResult<CostosPresupuestosEmailDto>> GetPaged(CostosPresupuestosEmailFilterDto filter)
        {
            if (filter.Page < 1) filter.Page = 1;
            return await _repository.GetPaged(filter);
        }

        public async Task<List<string>> GetActiveEmails()
        {
            return await _repository.GetActiveEmails();
        }

        public async Task Create(CostosPresupuestosEmailCreateDto dto, int userId)
        {
            await _repository.Create(dto, userId);
        }

        public async Task Update(CostosPresupuestosEmailEditDto dto, int userId)
        {
            await _repository.Update(dto, userId);
        }

        public async Task<bool> Delete(int id, int userId)
        {
            return await _repository.Delete(id, userId);
        }
    }
}
