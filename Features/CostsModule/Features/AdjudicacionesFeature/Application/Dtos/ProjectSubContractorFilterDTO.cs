namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    public class ProjectSubContractorFilterDTO {
        public int? ProjectId { get; set; }
        public string? ContributorName { get; set; }
        public string? ContributorRuc { get; set; }
        public int? ContractTypeId { get; set; }
        public int? ContractModalityId { get; set; }
        public int? PaymentMethodId { get; set; }
        public int? ProjectSubContractorStatusId { get; set; }
        public int? CreatedUserId { get; set; }
        public int Page { get; set; } = 1;
        /// <summary>
        /// Restricción de Oficina Técnica: solo adjudicaciones de estos proyectos
        /// (null = sin restricción). Lo establece el servicio, nunca viene del cliente.
        /// </summary>
        public List<int>? AllowedProjectIds { get; set; }
    }
}
