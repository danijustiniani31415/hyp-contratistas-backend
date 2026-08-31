namespace Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos
{
    public class ContractorWithEmailsDto
    {
        public int ContractorId { get; set; }
        public string ContributorName { get; set; } = null!;
        public string ContributorRuc { get; set; } = null!;
        public int ContractorStateId { get; set; }
        public List<string> Emails { get; set; } = new();
    }
}
