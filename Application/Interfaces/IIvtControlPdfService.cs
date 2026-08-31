using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Application.Interfaces
{
    public interface IIvtControlPdfService
    {
        Task<bool> Create(IvtControlPdfCreateDTO dto, int userId);
        Task<PagedResult<IvtControlPdfGetDTO>> GetPaged(int page, DateOnly? periodDate, int? userId);
        Task<IvtControlPdfFiltersDTO> GetFiltersData();
    }
}