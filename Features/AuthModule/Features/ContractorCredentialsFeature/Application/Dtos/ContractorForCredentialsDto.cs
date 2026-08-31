namespace Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Dtos
{
    public class ContractorForCredentialsDto
    {
        public int ContractorId { get; set; }
        public string ContributorName { get; set; } = null!;
        public List<string> Emails { get; set; } = new();
    }
}
