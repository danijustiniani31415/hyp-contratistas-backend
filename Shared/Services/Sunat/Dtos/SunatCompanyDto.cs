namespace Abril_Backend.Shared.Services.Sunat.Dtos
{
    public class SunatContributorDto
    {
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        public string ContributorAddress { get; set; } = null!;
        public string ContributorEconomicActivityDescription { get; set; } = null!;
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
    }
}
