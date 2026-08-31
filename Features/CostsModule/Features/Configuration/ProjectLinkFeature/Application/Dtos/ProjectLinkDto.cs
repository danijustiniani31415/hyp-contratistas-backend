namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos
{
    public class ProjectLinkDto
    {
        public int ProjectLinkId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
        public int ProjectLinkTypeId { get; set; }
        public string ProjectLinkTypeDescription { get; set; } = null!;
        public string LinkUrl { get; set; } = null!;
        public bool Active { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
    }
}
