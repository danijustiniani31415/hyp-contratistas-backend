namespace Abril_Backend.Features.CostsModule.Shared.Models {
    public class ContractorState {
        public int ContractorStateId { get; set; }
        public string ContractorStateDescription { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
