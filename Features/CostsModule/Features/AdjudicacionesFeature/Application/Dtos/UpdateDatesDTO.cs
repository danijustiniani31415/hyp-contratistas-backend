namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    public class UpdateDatesDTO {
        public DateOnly SigningDate { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int? ContractNumber { get; set; }
        public int? PromissoryNoteNumber { get; set; }
        public int? GuaranteeFundPercentage { get; set; }
        public int? GuaranteeFundDays { get; set; }
        public int? GuaranteeValidityDays { get; set; }
        public int? PaymentDays { get; set; }
    }
}
