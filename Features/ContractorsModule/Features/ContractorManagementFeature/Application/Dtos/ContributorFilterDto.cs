namespace Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos
{
    public class ContributorFilterDto
    {
        public string? ContributorName { get; set; }
        public string? ContributorRuc { get; set; }
        public int? ContractorStateId { get; set; }
        public string? LegalRepresentativeDni { get; set; }
        public string? LegalRepresentativeName { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }
        public int Page { get; set; } = 1;
    }
}
