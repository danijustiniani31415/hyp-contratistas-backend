namespace Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos
{
    /// <summary>
    /// Datos propuestos en una solicitud de actualización pendiente (estado 4 del contratista).
    /// Permite la comparación lado a lado contra los datos vigentes en Gestión de Contratistas.
    /// </summary>
    public class ContractorPendingUpdateDto
    {
        public int ContractorUpdateRequestId { get; set; }
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        public string? ContributorAddress { get; set; }
        public string? ContributorEconomicActivityDescription { get; set; }
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
        public string? LegalRepresentativeDni { get; set; }
        public string? LegalRepresentativeFullName { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }
        public string? LogoFileUrl { get; set; }
        public string? BrochureFileUrl { get; set; }
        public string? FichaRucFileUrl { get; set; }
        public string? ReferencesListFileUrl { get; set; }
        public System.DateTime CreatedDateTime { get; set; }
        public List<string> Emails { get; set; } = new();
    }
}
