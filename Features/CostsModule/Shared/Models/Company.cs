namespace Abril_Backend.Features.CostsModule.Shared.Models {
    public class Company
    {
        public int CompanyId { get; set; }
        public string CompanyRuc { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public string CompanyAddress { get; set; } = null!;
        public string CompanyEconomicActivityDescription { get; set; } = null!;
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
