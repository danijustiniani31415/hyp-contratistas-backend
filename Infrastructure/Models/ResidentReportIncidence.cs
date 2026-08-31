using Abril_Backend.Shared.Models;

namespace Abril_Backend.Infrastructure.Models {
    public class ResidentReportIncidence {
        public int ResidentReportIncidenceId {get; set;}
        public string ResidentReportIncidenceDescription {get;set;}
        public int ProjectId {get;set;}
        public int StateId {get;set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
        public Project Project { get; set; }
        public State StateNavigation { get; set; }
        public List<ResidentReportIncidenceImage> Images { get; set; } = new();
        public List<ResidentReportResponse> ResidentReportResponses { get; set; } = new();
    }
}