namespace Abril_Backend.Infrastructure.Models {
    public class Schedule {
        public int ScheduleId {get; set;}
        public string ScheduleDescription {get; set;}
        public int ProjectId {get;set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}