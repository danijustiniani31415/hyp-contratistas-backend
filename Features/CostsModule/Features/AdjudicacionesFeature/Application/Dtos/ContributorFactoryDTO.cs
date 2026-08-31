namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    public class ContributorFactoryDTO {
        public int ContractorId { get; set; }
        public int ContributorId { get; set; }
        public string? ContributorName { get; set; }
        public string? ContributorRuc { get; set; }
        public List<string> Emails { get; set; } = new();
    }
}
