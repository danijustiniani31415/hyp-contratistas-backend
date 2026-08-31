using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Models;
namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IResidentMonitoringRepository
    {
        Task<IEnumerable<TrackingRawDto>> GetTrackingDataAsync(
            int? projectId,
            int? residentUserId,
            int? month,
            int? year);
    }
}