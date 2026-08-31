namespace Abril_Backend.Application.DTOs {
    public class ProjectCreateDTO {
        public string ProjectDescription { get; set; }
        public string? LevelDescription { get; set; }
        public bool Active { get; set; }
    }
}