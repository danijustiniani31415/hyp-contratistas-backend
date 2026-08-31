namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class AdjudicacionSummarySheetDataDto
    {
        public int ProjectSubContractorId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
        public string? Abbreviation { get; set; }
        public string ContributorName { get; set; } = null!;
        public string ContributorRuc { get; set; } = null!;
        public string? ContributorAddress { get; set; }
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
        public string? LegalRepresentativeFullName { get; set; }
        public string? LegalRepresentativeDni { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }
        public string? ProjectRazonSocial { get; set; }
        public string? ProjectContributorRuc { get; set; }
        public string? ProjectDistrict { get; set; }
        public string? ProjectProvince { get; set; }
        public string? ProjectDepartment { get; set; }
        /// <summary>Dirección de la obra (project.project_location). Solo la calle/av., sin distrito ni provincia.</summary>
        public string? ProjectLocation { get; set; }
        /// <summary>N° de niveles: project.level_description, con fallback a project.num_niveles.</summary>
        public string? Niveles { get; set; }
        /// <summary>
        /// "Desc. de pisos" del proyecto (project.level_description), ej. "3 SÓTANOS 23 PISOS".
        /// Sin el fallback a num_niveles de <see cref="Niveles"/>: el contrato necesita el texto
        /// descriptivo, no un número suelto.
        /// </summary>
        public string? LevelDescription { get; set; }
        public string? ProjectLegalEntityRegistryNumber { get; set; }
        public int WorkItemId { get; set; }
        public string WorkItemDescription { get; set; } = null!;
        /// <summary>Nombre final de la partida para el contrato ({{PARTIDA}}). Null = usar WorkItemDescription.</summary>
        public string? ContractWorkItemName { get; set; }
        public string ContractTypeDescription { get; set; } = null!;
        public int? ContractModalityId { get; set; }
        public int PaymentMethodId { get; set; }
        public string PaymentMethodDescription { get; set; } = null!;
        public string? PaymentFormDescription { get; set; }
        public bool IncludesCartaFianza { get; set; }
        public string CurrencyCode { get; set; } = null!;
        public decimal Amount { get; set; }
        public bool HasIgv { get; set; }
        public decimal? AdvancePercentage { get; set; }
        public decimal? AdvanceAmount { get; set; }
        public int? TermDays { get; set; }
        public DateOnly? SigningDate { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? ContractNumber { get; set; }
        public int? PromissoryNoteNumber { get; set; }
        public int? GuaranteeFundPercentage { get; set; }
        public int? GuaranteeFundDays { get; set; }
        public int? GuaranteeValidityDays { get; set; }
        /// <summary>Forma de pago en días hábiles (paso 2) — "pago a x días hábiles".</summary>
        public int PaymentDays { get; set; }

        /// <summary>Cláusulas especiales de la partida de control (9.x / 7.x), ordenadas por SortOrder.</summary>
        public List<string> SpecialClauses { get; set; } = [];

        /// <summary>Cláusulas de Anexo 3 (Suministro) de la partida de control, ordenadas por SortOrder.</summary>
        public List<string> SpecialClausesAnexo3 { get; set; } = [];

        /// <summary>Cláusulas de Anexo 4 (Suministro) de la partida de control, ordenadas por SortOrder.</summary>
        public List<string> SpecialClausesAnexo4 { get; set; } = [];

        /// <summary>
        /// Formas de valorización (cláusula 5.1) de la partida, ordenadas por SortOrder.
        /// Si la partida tiene formas registradas, reemplazan la oración por defecto
        /// "Las valorizaciones se determinan a partir del inicio de los trabajos…".
        /// </summary>
        public List<(decimal Percentage, string Concept)> ValorizationForms { get; set; } = [];
    }
}
