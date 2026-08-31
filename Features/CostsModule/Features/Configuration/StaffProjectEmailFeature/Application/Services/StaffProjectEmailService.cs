using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Services
{
    public class StaffProjectEmailService : IStaffProjectEmailService
    {
        private readonly IStaffProjectEmailRepository _repository;
        private readonly IProjectRepository _projectRepository;

        public StaffProjectEmailService(IStaffProjectEmailRepository repository, IProjectRepository projectRepository)
        {
            _repository = repository;
            _projectRepository = projectRepository;
        }

        public async Task<StaffProjectEmailFormDataDto> GetFormData()
        {
            var projects = await _projectRepository.GetAllFactory();
            var types    = await _repository.GetTypesFactory();
            return new StaffProjectEmailFormDataDto { Projects = projects, Types = types };
        }

        public async Task<PagedResult<StaffProjectEmailDto>> GetPaged(StaffProjectEmailFilterDto filter)
        {
            return await _repository.GetPaged(filter);
        }

        public async Task Create(StaffProjectEmailCreateDto dto, int userId)
        {
            await _repository.Create(dto, userId);
        }

        public async Task Update(StaffProjectEmailEditDto dto, int userId)
        {
            await _repository.Update(dto, userId);
        }

        public async Task<bool> Delete(int staffProjectEmailId, int userId)
        {
            return await _repository.Delete(staffProjectEmailId, userId);
        }
    }
}
