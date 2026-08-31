namespace Abril_Backend.Infrastructure.Models {
    public class Milestone {
        public int MilestoneId {get; set;}
        public string MilestoneDescription {get; set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}