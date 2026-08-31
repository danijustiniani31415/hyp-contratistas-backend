namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    public class ContractModality {
        public int ContractModalityId { get; set; }
        public string ContractModalityDescription { get; set; } = null!;
        public bool State { get; set; }
        public DateTimeOffset CreatedDatetime { get; set; }
        public DateTimeOffset? UpdatedDatetime { get; set; }
    }
}
