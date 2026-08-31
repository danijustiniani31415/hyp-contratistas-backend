using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Services
{
    public class WorkSpecialtyService : IWorkSpecialtyService
    {
        private readonly IWorkSpecialtyRepository _repository;

        public WorkSpecialtyService(IWorkSpecialtyRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResult<WorkSpecialtyDto>> GetPaged(WorkSpecialtyFilterDto filter)
        {
            if (filter.Page < 1) filter.Page = 1;
            return await _repository.GetPaged(filter);
        }

        public Task Create(WorkSpecialtyCreateDto dto, int userId) => _repository.Create(dto, userId);
        public Task Update(WorkSpecialtyEditDto dto, int userId) => _repository.Update(dto, userId);
        public Task<bool> Delete(int workSpecialtyId, int userId) => _repository.Delete(workSpecialtyId, userId);
    }
}
