namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    /// <summary>
    /// Vista previa de destinatarios del correo del paso 1 (para la confirmación en el frontend).
    /// No incluye los destinatarios en copia oculta (BCC).
    /// </summary>
    public class AdjudicacionRecipientsPreviewDto
    {
        /// <summary>Destinatarios directos (Para): correos del contratista.</summary>
        public List<string> To { get; set; } = new();
        /// <summary>Destinatarios en copia (CC): staff de obra + oficina central + equipo de costos y presupuestos.</summary>
        public List<string> Cc { get; set; } = new();
    }
}
