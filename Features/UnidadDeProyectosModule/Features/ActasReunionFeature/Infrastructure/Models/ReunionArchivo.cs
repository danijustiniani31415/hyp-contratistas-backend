namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>Archivo adjunto a una reunión (diapositivas, documentos, evidencias, etc.).</summary>
    public class ReunionArchivo
    {
        public int ReunionArchivoId { get; set; }
        public int ReunionId { get; set; }
        public string ArchivoUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
