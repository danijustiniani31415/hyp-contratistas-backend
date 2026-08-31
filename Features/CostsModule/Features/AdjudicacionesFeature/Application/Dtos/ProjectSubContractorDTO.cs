namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {

    public class ProjectSubContractorFileDto
    {
        /// <summary>
        /// Id de la fila del archivo. Solo se llena en los archivos de cotización y cuadro
        /// comparativo (colecciones editables del paso 1), donde el frontend lo necesita para
        /// pedir su eliminación. Null en los documentos de paso 3 en adelante, que son 1 a 1.
        /// </summary>
        public int? FileId { get; set; }
        public string FileUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }
        public int? StatusId { get; set; }
        public string? StatusDescription { get; set; }
        public string? Observation { get; set; }
    }

    public class ProjectSubContractorDTO {
        public int ProjectSubContractorId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; }
        public int ContractorId { get; set; }
        public int ContributorId { get; set; }
        public string ContributorName { get; set; }
        public int ContractTypeId { get; set; }
        public string ContractTypeDescription { get; set; }
        public int? ContractModalityId { get; set; }
        public string? ContractModalityDescription { get; set; }
        public int PaymentMethodId { get; set; }
        public string PaymentMethodDescription { get; set; }
        public int? PaymentFormId { get; set; }
        public string? PaymentFormDescription { get; set; }
        public bool IncludesCartaFianza { get; set; }
        public decimal? AdvancePercentage {get;set;}
        public decimal? AdvanceAmount {get;set;}
        public decimal Amount { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public bool AmountHasIgv { get; set; }
        public List<string> ContractorEmails { get; set; } = new();
        public int WorkItemId { get; set; }
        public string WorkItemDescription { get; set; }
        public bool IsSubcontract { get; set; }
        public bool IsLabor { get; set; }
        public string? ContractWorkItemName { get; set; }
        public int WorkItemCategoryId { get; set; }
        public string WorkItemCategoryDescription { get; set; }
        /// <summary>Estado del instructivo de la partida de control (1=automático, 2=manual, 3=sin instructivo).</summary>
        public int? WorkItemCategoryInstructivosSyncStatus { get; set; }
        /// <summary>Nombre del instructivo asociado a la partida de control, si existe.</summary>
        public string? WorkItemCategoryInstructivosFolderName { get; set; }
        /// <summary>Formas de valorización (cláusula 5.1) de la partida, ordenadas por SortOrder.</summary>
        public List<WorkItemValorizationFormSimpleDTO> WorkItemValorizationForms { get; set; } = new();
        public int? WorkSpecialtyId { get; set; }
        public string? WorkSpecialtyDescription { get; set; }
        public int ProjectSubContractorStatusId { get; set; }
        public string ProjectSubContractorStatusDescription { get; set; }
        public DateOnly? SigningDate { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int?   TermDays               { get; set; }
        public int?   ContractNumber            { get; set; }
        public int?   PromissoryNoteNumber      { get; set; }
        public int?   GuaranteeFundPercentage   { get; set; }
        public int?   GuaranteeFundDays         { get; set; }
        public int?   GuaranteeValidityDays     { get; set; }
        public int    PaymentDays               { get; set; }
        public bool?  ArrivedWithObservations   { get; set; }
        public string? ArrivalObservation       { get; set; }
        // Procesos de firma (paso 6)
        public bool Step6SignedCostos { get; set; }
        public bool Step6SignedGerenteInmobiliario { get; set; }
        public bool Step6SignedGerenteGeneral { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public string? CreatedUserFullName { get; set; }
        public List<ProjectSubContractorFileDto> QuotationFiles { get; set; } = new();
        public List<ProjectSubContractorFileDto> ComparativeFiles { get; set; } = new();
        // Documentos del contrato (paso 3)
        public ProjectSubContractorFileDto? Contract { get; set; }
        public ProjectSubContractorFileDto? SummarySheet { get; set; }
        public ProjectSubContractorFileDto? Budget { get; set; }
        public ProjectSubContractorFileDto? Schedule { get; set; }
        public ProjectSubContractorFileDto? AttachedQuotation { get; set; }
        public ProjectSubContractorFileDto? ServiceOrder { get; set; }
        public ProjectSubContractorFileDto? PromissoryNote { get; set; }
        // Documentos escaneados (paso 7)
        public ProjectSubContractorFileDto? ScannedDoc1 { get; set; }
        public ProjectSubContractorFileDto? ScannedDoc2 { get; set; }
        public ProjectSubContractorFileDto? ScannedDoc3 { get; set; }
        // Paquete de contrato completo (paso 4 — autogenerado)
        public ProjectSubContractorFileDto? Package { get; set; }
        // Instructivo (paso 3 — obtenido desde OneDrive de Calidad)
        public ProjectSubContractorFileDto? Instructivo { get; set; }
        // Salidas no conforme y cuadro de tolerancias (paso 3 — solo estado, usan plantilla)
        public ProjectSubContractorFileDto? NonConformingOutput { get; set; }
        public ProjectSubContractorFileDto? ToleranceChart { get; set; }
        // Protección de Acabados (paso 3 — solo estado, usa plantilla). Solo expone StatusId.
        public ProjectSubContractorFileDto? FinishProtection { get; set; }
        // Ficha técnica y anexos (paso 3 — solo subida)
        public ProjectSubContractorFileDto? FichaTecnica { get; set; }
        public ProjectSubContractorFileDto? Anexo { get; set; }
    }
}