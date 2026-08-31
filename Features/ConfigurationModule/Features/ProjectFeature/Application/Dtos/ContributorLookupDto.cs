namespace Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos
{
    public class ContributorLookupDto
    {
        public int ContributorId { get; set; }
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        public string ContributorAddress { get; set; } = null!;
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }
    }

    public class ResponsableLookupDto
    {
        public int Id { get; set; }
        public string ApellidoNombre { get; set; } = string.Empty;
    }
}
