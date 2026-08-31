namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Infrastructure.Models
{
    /// <summary>
    /// Un batch de rendición — produce 1 PDF que agrupa N solicitudes (una página por trabajador).
    /// Persiste la referencia al archivo subido a SharePoint.
    /// </summary>
    public class GaRendicion
    {
        public int Id { get; set; }
        /// <summary>WebUrl devuelta por SharePoint al subir el PDF.</summary>
        public string PdfUrl { get; set; } = string.Empty;
        /// <summary>Item ID de Graph (opcional, útil para re-descargas vía API).</summary>
        public string? PdfItemId { get; set; }
        /// <summary>Nombre original del archivo subido (ej. "Planilla_Rendicion_20260522_153012_u14.pdf").</summary>
        public string PdfFilename { get; set; } = string.Empty;
        /// <summary>Usuario que disparó la rendición.</summary>
        public int RendidoPorId { get; set; }
        public DateTimeOffset RendidoAt { get; set; }
        /// <summary>Número correlativo del registro de planilla — se imprime como "TI: 000NNN" en el PDF.</summary>
        public int? NumeroPlanilla { get; set; }

        // -- Copia firmada por la jefatura ------------------------------------
        // El PDF original (PdfUrl) NO se toca: la firma se estampa sobre una copia y se sube
        // aparte. Vive en la planilla y no en cada salida porque el documento es uno solo -- la
        // planilla puede cubrir varias salidas y todas comparten la misma hoja firmada.

        /// <summary>WebUrl de la copia firmada en SharePoint. Null mientras nadie la firme.</summary>
        public string? PdfFirmadoUrl { get; set; }
        public string? PdfFirmadoItemId { get; set; }
        public string? PdfFirmadoFilename { get; set; }
        /// <summary>FK a <c>app_user.user_id</c> del jefe que firmo.</summary>
        public int? FirmadoPorId { get; set; }
        public DateTimeOffset? FirmadoAt { get; set; }
    }
}
