namespace Abril_Backend.Infrastructure.Models {
    public class ConstructionSiteLogbookControl {
        public int ConstructionSiteLogbookControlId {get; set;}
        public int ProjectId {get; set;}
        public string FileUrl {get;set;}
        public string FileDescription { get;set; }
        public DateOnly PeriodDate {get;set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}