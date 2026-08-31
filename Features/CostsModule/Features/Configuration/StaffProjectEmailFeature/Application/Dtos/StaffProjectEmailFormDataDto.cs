using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Dtos
{
    public class StaffProjectEmailFormDataDto
    {
        public List<ProjectSimpleDTO> Projects { get; set; } = new();
        public List<StaffProjectEmailTypeDto> Types { get; set; } = new();
    }
}
