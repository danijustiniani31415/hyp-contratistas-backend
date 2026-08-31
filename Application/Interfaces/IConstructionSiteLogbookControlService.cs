using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Application.Interfaces
{
    public interface IConstructionSiteLogbookControlService
    {
        Task<bool> Create(ConstructionSiteLogbookControlCreateDTO dto, int userId);
        Task<PagedResult<ConstructionSiteLogbookControlGetDTO>> GetPaged(int page, DateOnly? periodDate, int? userId);
        Task<ConstructionSiteLogbookControlFiltersDTO> GetFiltersData();
    }
}