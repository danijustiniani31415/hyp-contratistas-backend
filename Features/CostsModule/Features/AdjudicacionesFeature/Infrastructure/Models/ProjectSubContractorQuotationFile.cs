namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    public class ProjectSubContractorQuotationFile {
        public int ProjectSubContractorQuotationFileId {get; set;}
        public int ProjectSubContractorId {get; set;}
        public string FileUrl {get; set;}
        public string? OriginalFileName {get; set;}
        public string? SharepointItemId { get; set; }
        public DateTimeOffset CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTimeOffset? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
        public ProjectSubContractor ProjectSubContractor { get; set; }
    }
}