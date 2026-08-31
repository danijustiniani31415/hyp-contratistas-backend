namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class ContractPackageUrlsDto
    {
        public string? SummarySheetUrl     { get; init; }
        public string? SummarySheetItemId  { get; init; }
        public string? ContractUrl         { get; init; }
        public string? ContractItemId      { get; init; }
        /// <summary>Cotización adjunta del paso 3. Se inserta dentro del contrato, después del marcador
        /// &lt;&lt;INSERTAR_COTIZACION_AQUI&gt;&gt;. OriginalFileName se usa para saber si ya es PDF o necesita conversión.</summary>
        public string? AttachedQuotationUrl         { get; init; }
        public string? AttachedQuotationItemId      { get; init; }
        public string? AttachedQuotationFileName    { get; init; }
        /// <summary>Ficha técnica del paso 3. Se inserta dentro del contrato, después del marcador
        /// &lt;&lt;INSERTAR_FICHA_TÉCNICA_AQUI&gt;&gt;.</summary>
        public string? FichaTecnicaUrl              { get; init; }
        public string? FichaTecnicaItemId           { get; init; }
        public string? FichaTecnicaFileName         { get; init; }
        /// <summary>Orden de servicio del paso 3. Se inserta dentro del contrato, después del marcador
        /// &lt;&lt;INSERTAR_ORDEN_DE_SERVICIO_AQUI&gt;&gt;.</summary>
        public string? ServiceOrderUrl              { get; init; }
        public string? ServiceOrderItemId           { get; init; }
        public string? ServiceOrderFileName         { get; init; }
        /// <summary>Cronograma del paso 3. Se inserta dentro del contrato, después del marcador
        /// &lt;&lt;INSERTAR_CRONOGRAMA_AQUI&gt;&gt;.</summary>
        public string? ScheduleUrl                  { get; init; }
        public string? ScheduleItemId               { get; init; }
        public string? ScheduleFileName             { get; init; }
        /// <summary>Causales de No Conformidad: ya no se sube archivo; se usa un PDF de plantilla fijo.
        /// True cuando el estado es "Aprobado" (4) → se incluye en el paquete justo después del contrato.</summary>
        public bool NonConformingOutputApproved { get; init; }
        /// <summary>Cuadro de Tolerancias: ya no se sube archivo; se usa un PDF de plantilla fijo.
        /// True cuando el estado es "Aprobado" (4) → se incluye en el paquete después de Causales.</summary>
        public bool ToleranceChartApproved { get; init; }
        /// <summary>Protección de Acabados: documento de plantilla fijo (sin archivo).
        /// True cuando el estado es "Sí aplica" (4) → se incluye en el paquete después de Cuadro de Tolerancias.</summary>
        public bool FinishProtectionApproved { get; init; }
        public string? InstructivoUrl            { get; init; }
        public string? InstructivoItemId         { get; init; }
        /// <summary>Presente solo cuando la adjudicación tiene contrato con adelanto.</summary>
        public string? PromissoryNoteUrl    { get; init; }
        public string? PromissoryNoteItemId { get; init; }
        /// <summary>Anexo del paso 3 (en la práctica, la Vigencia de Poder). Va al final del paquete,
        /// después del pagaré. OriginalFileName se usa para saber si ya es PDF o necesita conversión.</summary>
        public string? AnexoUrl      { get; init; }
        public string? AnexoItemId   { get; init; }
        public string? AnexoFileName { get; init; }
        /// <summary>Número de contrato para armar el nombre del archivo (ej. 17 → _C017).</summary>
        public int? ContractNumber { get; init; }
    }
}
