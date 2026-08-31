using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Models;
namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IScheduleRepository
    {
        Task<object> GetPaged(int page);
        Task<Schedule> Create(ScheduleCreateDTO dto, int userId);
    }
}