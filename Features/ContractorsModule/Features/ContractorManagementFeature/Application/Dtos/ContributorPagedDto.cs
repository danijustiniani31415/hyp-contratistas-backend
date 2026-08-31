namespace Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos
{
    public class ContributorPagedDto
    {
        public int ContractorId { get; set; }
        public int ContributorId { get; set; }
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        public string ContributorAddress { get; set; } = null!;
        public string ContributorEconomicActivityDescription { get; set; } = null!;
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
        public string? LegalRepresentativeDni { get; set; }
        public string? LegalRepresentativeFullName { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }
        public int ContractorStateId { get; set; }
        public string ContractorStateDescription { get; set; } = null!;
        public string? LogoFileUrl { get; set; }
        public string? BrochureFileUrl { get; set; }
        public string? FichaRucFileUrl { get; set; }
        public string? ReferencesListFileUrl { get; set; }
        public bool HasUser { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public List<string> Emails { get; set; } = new();
        /// <summary>Correos con su id y estado 'active', usado por el modal de edición.</summary>
        public List<ContractorEmailItemDto> EmailDetails { get; set; } = new();
        public List<ContractorUserItemDto> Users { get; set; } = new();
        /// <summary>Datos propuestos cuando el contratista está en estado 4 (actualización pendiente); null en otro caso.</summary>
        public ContractorPendingUpdateDto? PendingUpdate { get; set; }
    }
}
