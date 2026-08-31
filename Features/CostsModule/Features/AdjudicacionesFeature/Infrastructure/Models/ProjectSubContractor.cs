using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Shared.Models;
namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    public class ProjectSubContractor {
        public int ProjectSubContractorId {get; set;}
        public int ProjectId {get; set;}
        public int ContractorId {get; set;}
        public int ContractTypeId {get; set;}
        public int? ContractModalityId { get; set; }
        public int PaymentMethodId {get; set;}
        public int? PaymentFormId { get; set; }
        // Suministro + contrato con adelanto: indica si el contrato incluye carta de fianza.
        public bool IncludesCartaFianza { get; set; }
        public decimal AdvancePercentage {get;set;}
        public decimal? AdvanceAmount {get; set;}
        public decimal Amount {get; set;}
        public int CurrencyId {get; set;}
        public bool HasIgv {get; set;}
        public string ContractorEmail {get; set;}
        public int WorkItemId {get; set;}
        public int WorkItemCategoryId {get; set;}
        // Especialidad de la partida (opcional). FK a work_specialty.
        public int? WorkSpecialtyId {get; set;}
        // Flags que normalizan cómo se nombra la partida en el contrato.
        // Subcontrato (SC) y mano de obra (M.O.) — afectan el nombre final ({{PARTIDA}}).
        public bool IsSubcontract { get; set; }
        public bool IsLabor { get; set; }
        // Nombre final de la partida tal como debe figurar en el contrato ({{PARTIDA}}).
        // Se genera dinámicamente en el formulario pero es editable; null = usar WorkItem.WorkItemDescription.
        public string? ContractWorkItemName { get; set; }
        public int ProjectSubContractorStatusId {get;set;}

        // Nombre de la carpeta de la adjudicación en OneDrive ("ADJUDICACIÓN N° X"), autoincremental
        // dentro de la carpeta del contratista (especialidad+partida+contratista). Se asigna la primera
        // vez que se guarda un documento y se reutiliza para que todos los documentos vayan a la misma carpeta.
        public string? AdjudicacionFolderName { get; set; }

        // Expediente dates (step 2)
        public DateOnly? SigningDate { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? TermDays { get; set; }

        // Número de contrato y pagaré (paso 2)
        public int? ContractNumber { get; set; }
        public int? PromissoryNoteNumber { get; set; }

        // Fondo de garantía (paso 2)
        public int? GuaranteeFundPercentage { get; set; }
        public int? GuaranteeFundDays { get; set; }
        // Periodo de validez de garantía en días (paso 2)
        public int? GuaranteeValidityDays { get; set; }
        // Forma de pago en días hábiles (paso 2) — usado en "pago a x días hábiles" de la hoja resumen
        public int PaymentDays { get; set; } = 7;

        // Llegada a Of. Central (paso 5)
        public bool? ArrivedWithObservations { get; set; }
        public string? ArrivalObservation { get; set; }

        // Procesos de firma (paso 6) — estado persistido de cada checkbox
        public bool Step6SignedCostos { get; set; }
        public bool Step6SignedGerenteInmobiliario { get; set; }
        public bool Step6SignedGerenteGeneral { get; set; }

        // Document FKs (nullable — populated as the expediente progresses)
        public int? ProjectSubContractorContractId { get; set; }
        public int? ProjectSubContractorSummarySheetId { get; set; }
        public int? ProjectSubContractorBudgetId { get; set; }
        public int? ProjectSubContractorScheduleId { get; set; }
        public int? ProjectSubContractorAttachedQuotationId { get; set; }
        public int? ProjectSubContractorServiceOrderId { get; set; }
        public int? ProjectSubContractorPromissoryNoteId { get; set; }
        public int? ProjectSubContractorPackageId { get; set; }
        public int? ProjectSubContractorInstructivoId { get; set; }
        public int? ProjectSubContractorNonConformingOutputId { get; set; }
        public int? ProjectSubContractorToleranceChartId { get; set; }
        public int? ProjectSubContractorFichaTecnicaId { get; set; }
        public int? ProjectSubContractorAnexoId { get; set; }

        // Documentos de plantilla fija (no se sube archivo). Solo guardan el estado:
        // 4 = Sí aplica → va en el paquete; 1 = No aplica → no va.
        public int? NonConformingOutputStatusId { get; set; }
        public int? ToleranceChartStatusId { get; set; }
        public int? FinishProtectionStatusId { get; set; }

        public DateTimeOffset CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTimeOffset? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
        public Project Project { get; set; }
        public Contractor Contractor { get; set; } = null!;
        public ProjectSubContractorContract? Contract { get; set; }
        public ProjectSubContractorSummarySheet? SummarySheet { get; set; }
        public ProjectSubContractorBudget? Budget { get; set; }
        public ProjectSubContractorSchedule? Schedule { get; set; }
        public ProjectSubContractorAttachedQuotation? AttachedQuotation { get; set; }
        public ProjectSubContractorServiceOrder? ServiceOrder { get; set; }
        public ProjectSubContractorPromissoryNote? PromissoryNote { get; set; }
        public ProjectSubContractorPackage? Package { get; set; }
        public ProjectSubContractorInstructivo? Instructivo { get; set; }
        public ProjectSubContractorNonConformingOutput? NonConformingOutput { get; set; }
        public ProjectSubContractorToleranceChart? ToleranceChart { get; set; }
        public ProjectSubContractorFichaTecnica? FichaTecnica { get; set; }
        public ProjectSubContractorAnexo? Anexo { get; set; }
        public List<ProjectSubContractorQuotationFile> QuotationFiles { get; set; } = new();
        public List<ProjectSubContractorComparativeFile> ComparativeFiles { get; set; } = new();
    }
}