namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models
{
    public class ProjectSubContractorFileStatus
    {
        public int ProjectSubContractorFileStatusId { get; set; }
        public string? ProjectSubContractorFileStatusDescription { get; set; }
        public DateTimeOffset CreatedDatetime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDatetime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
