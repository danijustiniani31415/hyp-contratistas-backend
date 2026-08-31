namespace Abril_Backend.Features.CostsModule.Shared.Models {
    /// <summary>Catálogo de estados de una solicitud de actualización de datos de contratista.</summary>
    public class ContractorUpdateState {
        public int ContractorUpdateStateId { get; set; }
        public string ContractorUpdateStateDescription { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
