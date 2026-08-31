using Abril_Backend.Application.DTOs;
namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class ProjectSubContractorPagedWithFiltersDTO
    {
        public PagedResult<ProjectSubContractorDTO> Paged { get; set; } = null!;
        public ProjectSubContractorFormDataDTO Filters { get; set; } = null!;
    }
}
