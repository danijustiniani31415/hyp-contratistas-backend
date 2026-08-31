using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Services
{
    public class HolidayService : IHolidayService
    {
        private readonly IHolidayRepository _repository;
        public HolidayService(IHolidayRepository repository) => _repository = repository;

        public async Task<HolidayInitialDto> GetInitial(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            return new HolidayInitialDto
            {
                Types = await _repository.GetTypes(),
                Holidays = await _repository.GetPaged(page, pageSize),
            };
        }

        public Task<PagedResult<HolidayDto>> GetPaged(int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            return _repository.GetPaged(page, pageSize);
        }

        public Task Create(HolidayCreateDto dto) => _repository.Create(dto);
        public Task Update(HolidayEditDto dto) => _repository.Update(dto);
        public Task<bool> DeleteSoftAsync(int holidayId) => _repository.DeleteSoftAsync(holidayId);
    }
}
