namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    public class PaymentForm {
        public int PaymentFormId { get; set; }
        public string PaymentFormDescription { get; set; } = null!;
        public bool State { get; set; }
        public DateTimeOffset CreatedDatetime { get; set; }
        public DateTimeOffset? UpdatedDatetime { get; set; }
    }
}
