namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models
{
    /// <summary>
    /// Captura (imagen + monto) asociada a un trayecto específico de una solicitud de salida.
    /// </summary>
    public class GaSolicitudCaptura
    {
        public int Id { get; set; }
        /// <summary>FK al trayecto al que pertenece (no a la solicitud directamente).</summary>
        public int TrayectoId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? ImageItemId { get; set; }
        public string Filename { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public int UploadedById { get; set; }
        public DateTimeOffset UploadedAt { get; set; }
    }
}
