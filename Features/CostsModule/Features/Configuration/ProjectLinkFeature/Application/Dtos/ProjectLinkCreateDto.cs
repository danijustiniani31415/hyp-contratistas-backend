namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos
{
    public class ProjectLinkCreateDto
    {
        public int ProjectId { get; set; }
        public int ProjectLinkTypeId { get; set; }
        public string LinkUrl { get; set; } = null!;
    }
}
