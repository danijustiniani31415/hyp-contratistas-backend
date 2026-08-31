namespace Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Dtos
{
    public class StaffProjectEmailEditDto
    {
        public int StaffProjectEmailId { get; set; }
        public string Email { get; set; } = null!;
        public int StaffProjectEmailTypeId { get; set; }
        public bool Active { get; set; }
    }
}
