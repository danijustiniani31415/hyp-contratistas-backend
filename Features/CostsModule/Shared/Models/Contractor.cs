namespace Abril_Backend.Features.CostsModule.Shared.Models {
    public class Contractor
    {
        public int ContractorId { get; set; }
        public int ContributorId { get; set; }
        public int ContractorStateId { get; set; }
        public string? ActivationToken { get; set; }
        public DateTime? ActivationTokenExpiry { get; set; }
        public string? LogoFileUrl { get; set; }
        public string? BrochureFileUrl { get; set; }
        public string? FichaRucFileUrl { get; set; }
        public string? ReferencesListFileUrl { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }

        public Contributor Contributor { get; set; } = null!;
        public ContractorState ContractorState { get; set; } = null!;
        public List<ContractorEmail> Emails { get; set; } = new();
        public List<ContractorUser> Users { get; set; } = new();
    }
}
