namespace Abril_Backend.Application.DTOs {
    public class ScheduleDTO {
        public int ScheduleId {get; set;}
        public string ScheduleDescription {get; set;}
        public int ProjectId {get;set;}
        public string ProjectDescription {get;set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public string? CreatedUserFullName {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
    }
}