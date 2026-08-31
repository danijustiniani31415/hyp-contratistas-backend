namespace Abril_Backend.Application.DTOs {
    public class ScheduleCreateDTO {
        public string ScheduleDescription {get; set;}
        public int ProjectId {get;set;}
        public bool Active {get;set;}
    }
}