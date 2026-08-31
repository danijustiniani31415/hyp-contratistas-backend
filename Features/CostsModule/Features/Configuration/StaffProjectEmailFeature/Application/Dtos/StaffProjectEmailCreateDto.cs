namespace Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Dtos
{
    public class StaffProjectEmailCreateDto
    {
        public int ProjectId { get; set; }
        public string Email { get; set; } = null!;
        public int StaffProjectEmailTypeId { get; set; }
    }
}
