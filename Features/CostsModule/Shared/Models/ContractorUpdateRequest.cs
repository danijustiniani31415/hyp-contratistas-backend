namespace Abril_Backend.Features.CostsModule.Shared.Models {
    /// <summary>
    /// Solicitud de actualización de datos sobre un contratista ya APROBADO. Guarda en
    /// staging los datos propuestos (empresa + archivos + correos) sin tocar los datos
    /// vigentes del contributor/contractor hasta que el área de costos la apruebe.
    /// </summary>
    public class ContractorUpdateRequest {
        public int ContractorUpdateRequestId { get; set; }
        public int ContractorId { get; set; }
        public int ContractorUpdateStateId { get; set; }

        // ── Datos propuestos del contributor ───────────────────────────────
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

        // ── Archivos propuestos ────────────────────────────────────────────
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

        public List<ContractorUpdateRequestEmail> Emails { get; set; } = new();
    }
}
