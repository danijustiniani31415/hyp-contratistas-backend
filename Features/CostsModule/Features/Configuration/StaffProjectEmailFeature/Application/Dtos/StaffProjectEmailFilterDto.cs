namespace Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Dtos
{
    public class StaffProjectEmailFilterDto
    {
        public int? ProjectId { get; set; }
        public string? Email { get; set; }
        public int? StaffProjectEmailTypeId { get; set; }
        public int Page { get; set; } = 1;
    }
}
