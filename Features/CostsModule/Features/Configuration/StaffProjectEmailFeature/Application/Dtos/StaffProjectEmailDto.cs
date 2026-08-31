namespace Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Dtos
{
    public class StaffProjectEmailDto
    {
        public int StaffProjectEmailId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int StaffProjectEmailTypeId { get; set; }
        public string StaffProjectEmailTypeDescription { get; set; } = null!;
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
    }
}
