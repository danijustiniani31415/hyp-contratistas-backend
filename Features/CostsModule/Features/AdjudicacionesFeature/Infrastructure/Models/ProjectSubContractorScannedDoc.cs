namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    public class ProjectSubContractorScannedDoc {
        public int ProjectSubContractorScannedDocId { get; set; }
        public int ProjectSubContractorId { get; set; }
        public int Slot { get; set; }
        public string? FileUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public string? SharepointItemId { get; set; }
        public DateTimeOffset CreatedDatetime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDatetime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
