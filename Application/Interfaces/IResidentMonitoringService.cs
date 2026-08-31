using Abril_Backend.Application.DTOs;
namespace Abril_Backend.Application.Interfaces
{
    public interface IResidentMonitoringService
    {
        Task<TrackingResultDto> GetTrackingAsync(TrackingQueryDto query);
        Task<TrackingFiltersDto> GetFilters();
    }
}