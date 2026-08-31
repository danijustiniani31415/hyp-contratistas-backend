namespace Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Dtos
{
    /// <summary>Fila de la tabla de facturas.</summary>
    public class InvoiceDto
    {
        public int InvoiceId { get; set; }
        public DateOnly IssueDate { get; set; }
        public string Serie { get; set; } = null!;
        public string Correlativo { get; set; } = null!;
        /// <summary>Número completo para mostrar (serie-correlativo).</summary>
        public string InvoiceNumber => $"{Serie}-{Correlativo}";
        public int? ContributorId { get; set; }
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        public int? AbrilContributorId { get; set; }
        public string? AbrilContributorName { get; set; }
        /// <summary>Nombre de la hoja del Excel (razón social corta de Abril), si vino de importación.</summary>
        public string? AbrilName { get; set; }
        public string Description { get; set; } = null!;
        public int InvoicePaymentFormId { get; set; }
        public string InvoicePaymentFormDescription { get; set; } = null!;
        public decimal Total { get; set; }
        public int? CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public string? CurrencySymbol { get; set; }
        public int? InvoiceStatusId { get; set; }
        public string? InvoiceStatusDescription { get; set; }
        public int? InvoiceObservationReasonId { get; set; }
        public string? InvoiceObservationReasonDescription { get; set; }
        public string? DocumentUrl { get; set; }
        public string? SignedDocumentUrl { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }

    /// <summary>Detalle completo de una factura (modal de ver/editar).</summary>
    public class InvoiceDetailDto
    {
        public int InvoiceId { get; set; }
        public DateOnly IssueDate { get; set; }
        public string Serie { get; set; } = null!;
        public string Correlativo { get; set; } = null!;
        public string InvoiceNumber => $"{Serie}-{Correlativo}";
        public int? ContributorId { get; set; }
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        public int? AbrilContributorId { get; set; }
        public string? AbrilContributorName { get; set; }
        public string? AbrilContributorRuc { get; set; }
        public string Description { get; set; } = null!;
        public int InvoicePaymentFormId { get; set; }
        public string InvoicePaymentFormDescription { get; set; } = null!;
        public decimal Total { get; set; }
        public int? CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public string? CurrencySymbol { get; set; }
        public int? InvoiceStatusId { get; set; }
        public string? InvoiceStatusDescription { get; set; }
        public int? InvoiceObservationReasonId { get; set; }
        public string? InvoiceObservationReasonDescription { get; set; }
        public int? InvoiceFolderId { get; set; }
        public string? InvoiceFolderName { get; set; }
        public string? DocumentUrl { get; set; }
        public string? SignedDocumentUrl { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }

    /// <summary>Grupo de facturas por razón social de Abril (vista de bloques).</summary>
    public class InvoiceBlockGroupDto
    {
        public string AbrilName { get; set; } = null!;
        public int Count { get; set; }
        public List<InvoiceDto> Items { get; set; } = new();
    }

    // ── Dashboard ────────────────────────────────────────────────────────────
    public class InvoiceChartItemDto
    {
        public string Label { get; set; } = null!;
        public decimal Total { get; set; }
        public int Count { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
    }

    public class InvoiceCurrencyTotalDto
    {
        public string CurrencyCode { get; set; } = null!;
        public string? CurrencySymbol { get; set; }
        public decimal Total { get; set; }
        public int Count { get; set; }
    }

    public class InvoiceDashboardDto
    {
        public int TotalCount { get; set; }
        public List<InvoiceCurrencyTotalDto> TotalsByCurrency { get; set; } = new();
        public List<InvoiceChartItemDto> ByMonth { get; set; } = new();
        public List<InvoiceChartItemDto> ByPaymentForm { get; set; } = new();
        public List<InvoiceChartItemDto> ByAbril { get; set; } = new();
        public List<InvoiceChartItemDto> TopSuppliers { get; set; } = new();
    }

    /// <summary>Carga inicial del dashboard: desplegables de filtros + datos de los gráficos.</summary>
    public class InvoiceDashboardInitDto
    {
        public List<InvoiceSupplierDto> Suppliers { get; set; } = new();
        public List<InvoicePaymentFormDto> PaymentForms { get; set; } = new();
        public List<InvoiceSupplierDto> AbrilCompanies { get; set; } = new();
        public List<InvoiceCurrencyDto> Currencies { get; set; } = new();
        public InvoiceDashboardDto Dashboard { get; set; } = new();
    }

    // ── Importación desde Excel de órdenes de pago ─────────────────────────────
    /// <summary>Una fila del Excel ya parseada por el frontend.</summary>
    public class InvoiceImportRowDto
    {
        public string? PaymentOrderNumber { get; set; }
        public string? Description { get; set; }
        public string? ProveedorName { get; set; }
        public string? DocumentType { get; set; }
        public string Serie { get; set; } = "";
        public string Correlativo { get; set; } = "";
        /// <summary>Fecha en ISO (yyyy-MM-dd).</summary>
        public string? IssueDate { get; set; }
        /// <summary>Código de moneda ya normalizado (PEN / USD).</summary>
        public string? CurrencyCode { get; set; }
        public decimal Total { get; set; }
        public decimal? AuthorizedAmount { get; set; }
        public string? Observation { get; set; }
        /// <summary>Razón social de Abril (nombre de la hoja).</summary>
        public string? AbrilName { get; set; }
        /// <summary>Nombre del archivo arrastrado que se asoció a esta fila (o null).</summary>
        public string? MatchedFileName { get; set; }
    }

    public class InvoiceImportResultDto
    {
        public int Inserted { get; set; }
        public int WithFile { get; set; }
        public int WithoutFile { get; set; }
    }

    /// <summary>Moneda para el desplegable del formulario (reusa la tabla currency).</summary>
    public class InvoiceCurrencyDto
    {
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; } = null!;
        public string CurrencyDescription { get; set; } = null!;
        public string? CurrencySymbol { get; set; }
    }

    /// <summary>Proveedor (contribuyente) para el desplegable del formulario.</summary>
    public class InvoiceSupplierDto
    {
        public int? ContributorId { get; set; }
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
    }

    /// <summary>Forma de pago para el desplegable del formulario.</summary>
    public class InvoicePaymentFormDto
    {
        public int InvoicePaymentFormId { get; set; }
        public string InvoicePaymentFormDescription { get; set; } = null!;
    }

    /// <summary>Motivo de observación para el desplegable del modal "Observar".</summary>
    public class InvoiceObservationReasonDto
    {
        public int InvoiceObservationReasonId { get; set; }
        public string InvoiceObservationReasonDescription { get; set; } = null!;
    }

    /// <summary>Acción en bloque (aprobar/rechazar/observar) sobre facturas seleccionadas.</summary>
    public class InvoiceBulkActionDto
    {
        public List<int> InvoiceIds { get; set; } = new();
        /// <summary>Requerido solo para la acción "Observar".</summary>
        public int? InvoiceObservationReasonId { get; set; }
    }

    /// <summary>Carga inicial de la pantalla: datos de filtros/desplegables + primera página de la tabla.</summary>
    public class InvoiceInitDto
    {
        public List<InvoiceSupplierDto> Suppliers { get; set; } = new();
        public List<InvoicePaymentFormDto> PaymentForms { get; set; } = new();
        /// <summary>Razones sociales que maneja Abril (contribuyentes con es_abril = true).</summary>
        public List<InvoiceSupplierDto> AbrilCompanies { get; set; } = new();
        public List<InvoiceCurrencyDto> Currencies { get; set; } = new();
        /// <summary>Motivos de observación para el modal "Observar".</summary>
        public List<InvoiceObservationReasonDto> ObservationReasons { get; set; } = new();
        public Abril_Backend.Application.DTOs.PagedResult<InvoiceDto> Invoices { get; set; } = new();
    }

    /// <summary>Filtros de búsqueda de la tabla.</summary>
    public class InvoiceFilterDto
    {
        public string? Search { get; set; }
        public string? Serie { get; set; }
        public string? Correlativo { get; set; }
        /// <summary>Razón social del proveedor (contribuyente).</summary>
        public int? ContributorId { get; set; }
        /// <summary>RUC del proveedor.</summary>
        public string? ContributorRuc { get; set; }
        /// <summary>Razón social de Abril (es_abril = true).</summary>
        public int? AbrilContributorId { get; set; }
        /// <summary>RUC de la razón social de Abril.</summary>
        public string? AbrilContributorRuc { get; set; }
        public int? InvoicePaymentFormId { get; set; }
        /// <summary>Moneda (currency_id): 1 = Soles (PEN), 2 = Dólares (USD).</summary>
        public int? CurrencyId { get; set; }
        public decimal? TotalMin { get; set; }
        public decimal? TotalMax { get; set; }
        public DateOnly? IssueDateFrom { get; set; }
        public DateOnly? IssueDateTo { get; set; }
        public int Page { get; set; } = 1;
        /// <summary>
        /// Columna por la que ordenar la tabla. Si es null/desconocida se usa el orden original
        /// (más recientes primero). Valores: issueDate, invoiceNumber, contributorName,
        /// contributorRuc, abrilContributorName, description, invoicePaymentFormDescription,
        /// total, invoiceStatusDescription.
        /// </summary>
        public string? SortBy { get; set; }
        /// <summary>Dirección del orden: "asc" o "desc" (por defecto "asc").</summary>
        public string? SortDir { get; set; }
    }

    /// <summary>Datos de creación de una factura (se reciben como multipart/form-data).</summary>
    public class InvoiceCreateDto
    {
        public DateOnly IssueDate { get; set; }
        public string Serie { get; set; } = null!;
        public string Correlativo { get; set; } = null!;
        public int ContributorId { get; set; }
        public string Description { get; set; } = null!;
        public int InvoicePaymentFormId { get; set; }
        public decimal Total { get; set; }
        public int CurrencyId { get; set; }
        /// <summary>Razón social de Abril (es_abril = true) a la que pertenece la factura.</summary>
        public int AbrilContributorId { get; set; }
        public IFormFile? DocumentFile { get; set; }
    }

    /// <summary>Datos de edición de una factura (multipart/form-data; documento opcional).</summary>
    public class InvoiceUpdateDto
    {
        public int InvoiceId { get; set; }
        public DateOnly IssueDate { get; set; }
        public string Serie { get; set; } = null!;
        public string Correlativo { get; set; } = null!;
        public int ContributorId { get; set; }
        public string Description { get; set; } = null!;
        public int InvoicePaymentFormId { get; set; }
        public decimal Total { get; set; }
        public int CurrencyId { get; set; }
        public int AbrilContributorId { get; set; }
        /// <summary>Si se adjunta, reemplaza el documento (se vuelve a subir a OneDrive).</summary>
        public IFormFile? DocumentFile { get; set; }
    }

    /// <summary>Alta de un nuevo proveedor desde el modal de consulta RUC.</summary>
    public class InvoiceSupplierCreateDto
    {
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        public string ContributorAddress { get; set; } = null!;
        public string? ContributorEconomicActivityDescription { get; set; }
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
    }
}
