namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    public class ProjectSubContractorSchedule {
        public int ProjectSubContractorScheduleId { get; set; }
        public string? FileUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public string? SharepointItemId { get; set; }
        public int? ProjectSubContractorFileStatusId { get; set; }
        public string? Observation { get; set; }
        public DateTimeOffset CreatedDatetime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDatetime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
        public ProjectSubContractorFileStatus? FileStatus { get; set; }
    }
}
