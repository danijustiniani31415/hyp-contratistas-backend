namespace Abril_Backend.Application.DTOs {
    public class ProjectEditDTO {
        public int ProjectId {get;set;}
        public string ProjectDescription { get; set; }
        public string? LevelDescription { get; set; }
        public bool Active { get; set; }
    }
}