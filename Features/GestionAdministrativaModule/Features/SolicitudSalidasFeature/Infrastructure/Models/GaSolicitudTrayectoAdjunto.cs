namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models
{
    /// <summary>
    /// Documento adjunto (prueba) asociado a un trayecto específico de una solicitud de salida.
    /// Un trayecto cuyo motivo tiene requiere_adjunto = true puede tener N documentos.
    /// El archivo vive en la carpeta configurada de SharePoint/OneDrive (ga_adjunto_folder).
    ///
    /// Nota: las columnas Adjunto* embebidas en <see cref="GaSolicitudTrayecto"/> son el modelo
    /// anterior (1 adjunto por trayecto) y se conservan para no perder los adjuntos históricos.
    /// Los adjuntos nuevos se guardan siempre acá.
    /// </summary>
    public class GaSolicitudTrayectoAdjunto
    {
        public int Id { get; set; }
        /// <summary>FK al trayecto al que pertenece (no a la solicitud directamente).</summary>
        public int TrayectoId { get; set; }
        /// <summary>webUrl del archivo en SharePoint/OneDrive (para abrirlo desde el detalle).</summary>
        public string AdjuntoUrl { get; set; } = string.Empty;
        public string? AdjuntoItemId { get; set; }
        public string? AdjuntoDriveId { get; set; }
        public string AdjuntoFilename { get; set; } = string.Empty;
        public int? UploadedById { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }
}
