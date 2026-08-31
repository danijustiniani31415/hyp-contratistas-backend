namespace Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos
{
    public class ContractorUpdateDto
    {
        public string? ContributorRuc { get; set; }
        public string ContributorName { get; set; } = null!;
        public string ContributorAddress { get; set; } = null!;
        public string ContributorEconomicActivityDescription { get; set; } = null!;
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }
        public string? LegalRepresentativeDni { get; set; }
        public string? LegalRepresentativeFullName { get; set; }
        /// <summary>
        /// Lista de correos serializada en JSON dentro del multipart/form-data.
        /// Cada elemento: { contractorEmailId: number|null, email: string, active: bool }.
        /// contractorEmailId null/0 = correo nuevo. Un correo previamente existente que no
        /// aparezca en la lista se considera eliminado (soft-delete).
        /// </summary>
        public string? EmailsJson { get; set; }
        public IFormFile? LogoFile { get; set; }
        public IFormFile? BrochureFile { get; set; }
        public IFormFile? FichaRucFile { get; set; }
        public IFormFile? ReferencesListFile { get; set; }
    }
}
