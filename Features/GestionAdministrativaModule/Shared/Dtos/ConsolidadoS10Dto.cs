namespace Abril_Backend.Features.GestionAdministrativa.Shared.Dtos
{
    /// <summary>Ámbito al que se asocia el PDF Consolidado del S10. Excluyentes entre sí.</summary>
    public enum ConsolidadoS10Ambito
    {
        /// <summary>Cubre toda la planilla de rendición (todas las salidas del batch). Es el caso normal.</summary>
        Rendicion = 1,
        /// <summary>Cubre solo esa salida puntual.</summary>
        Solicitud = 2,
    }

    /// <summary>Consolidado del S10 vigente de una salida rendida, para exponerlo al frontend.</summary>
    public class ConsolidadoS10Dto
    {
        public int Id { get; set; }
        /// <summary>"Rendicion" | "Solicitud" — a qué quedó asociado el archivo.</summary>
        public string Ambito { get; set; } = string.Empty;
        public string PdfUrl { get; set; } = string.Empty;
        public string PdfFilename { get; set; } = string.Empty;
        public DateTimeOffset UploadedAt { get; set; }
    }
}
