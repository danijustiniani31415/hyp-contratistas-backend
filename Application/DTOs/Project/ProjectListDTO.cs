namespace Abril_Backend.Application.DTOs {
    public class ProjectDTO {
        public int ProjectId {get; set;}
        public string ProjectDescription {get; set;}
        public string? LevelDescription {get; set;}
        public List<string> ResidentFullNames {get; set;} = new();
        public string? FotoUrl {get; set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
    }
}