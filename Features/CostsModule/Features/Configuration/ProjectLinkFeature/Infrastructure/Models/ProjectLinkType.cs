namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Models
{
    public class ProjectLinkType
    {
        public int ProjectLinkTypeId { get; set; }
        public string ProjectLinkTypeDescription { get; set; } = null!;
        public bool State { get; set; }
    }
}
