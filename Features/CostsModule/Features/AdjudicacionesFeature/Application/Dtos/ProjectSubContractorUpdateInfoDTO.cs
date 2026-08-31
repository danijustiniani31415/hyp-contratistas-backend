namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    /// <summary>
    /// Campos del paso 1 (información de la adjudicación) editables mientras la
    /// adjudicación esté en pasos 1–4, incluidos los archivos de cotización y el cuadro
    /// comparativo. Se recibe como multipart porque puede traer archivos nuevos.
    /// </summary>
    public class ProjectSubContractorUpdateInfoDTO {
        public int ProjectId { get; set; }
        public int ContractorId { get; set; }
        public int ContractTypeId { get; set; }
        public int? ContractModalityId { get; set; }
        public int PaymentMethodId { get; set; }
        public int? PaymentFormId { get; set; }
        public bool IncludesCartaFianza { get; set; }
        public decimal AdvancePercentage { get; set; }
        public decimal? AdvanceAmount { get; set; }
        public decimal Amount { get; set; }
        public int CurrencyId { get; set; }
        public bool HasIgv { get; set; }
        public int WorkItemId { get; set; }
        public int WorkItemCategoryId { get; set; }
        public int? WorkSpecialtyId { get; set; }
        public bool IsSubcontract { get; set; }
        public bool IsLabor { get; set; }
        public string? ContractWorkItemName { get; set; }

        // ── Archivos del paso 1 ──────────────────────────────────────────────
        /// <summary>Archivos de cotización nuevos a subir (se suman a los que ya existen).</summary>
        public List<IFormFile>? NewQuotationFiles { get; set; }
        /// <summary>Archivos de cuadro comparativo nuevos a subir.</summary>
        public List<IFormFile>? NewComparativeFiles { get; set; }
        /// <summary>Ids de cotizaciones a quitar (soft delete; el archivo permanece en OneDrive).</summary>
        public List<int>? RemovedQuotationFileIds { get; set; }
        /// <summary>Ids de cuadros comparativos a quitar (soft delete).</summary>
        public List<int>? RemovedComparativeFileIds { get; set; }
    }
}
