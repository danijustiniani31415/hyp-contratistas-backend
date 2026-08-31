namespace Abril_Backend.Infrastructure.Models {
    public class ResidentReportResponseImage {
        public int ResidentReportResponseImageId {get; set;}
        public int ResidentReportResponseId {get; set;}
        public string ImageUrl {get;set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
        public ResidentReportResponse ResidentReportResponse { get; set; }
    }
}