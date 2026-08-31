namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class AdjudicacionNotificationDataDto
    {
        public int ProjectSubContractorId { get; set; }
        public int ProjectSubContractorStatusId { get; set; }
        public string ProjectDescription { get; set; } = null!;
        public string WorkItemDescription { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        /// <summary>Todos los correos registrados en contractor_email para el contratista de esta adjudicación.</summary>
        public List<string> ContractorEmails { get; set; } = new();
        /// <summary>Correos de tipo "Staff de obra" — van a la matriz de comunicaciones Y al CC.</summary>
        public List<string> StaffEmails { get; set; } = new();
        /// <summary>Correos de tipo "Oficina central" — van SOLO al CC, no a la matriz.</summary>
        public List<string> OficinaCentralEmails { get; set; } = new();
        public List<ProjectSubContractorFileDto> QuotationFiles { get; set; } = new();
        public List<ProjectSubContractorFileDto> ComparativeFiles { get; set; } = new();
    }
}
