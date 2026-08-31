namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos
{
    public class ProjectLinkUpdateDto
    {
        public int ProjectLinkId { get; set; }
        public string LinkUrl { get; set; } = null!;
        public bool Active { get; set; }
    }
}
