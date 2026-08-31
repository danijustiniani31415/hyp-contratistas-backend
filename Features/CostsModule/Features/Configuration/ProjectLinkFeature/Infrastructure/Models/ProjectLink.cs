namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Models
{
    public class ProjectLink
    {
        public int ProjectLinkId { get; set; }
        public int ProjectId { get; set; }
        public int ProjectLinkTypeId { get; set; }
        public string LinkUrl { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
