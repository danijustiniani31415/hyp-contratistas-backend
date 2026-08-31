namespace Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Dtos
{
    public class ProjectLinkFormDataDto
    {
        public List<ProjectSimpleDto> Projects { get; set; } = new();
        public List<ProjectLinkTypeDto> Types { get; set; } = new();
    }

    public class ProjectSimpleDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
    }
}
