namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class SendObservationEmailDto
    {
        public string GraphAccessToken { get; set; } = null!;

        /// <summary>Etiqueta legible del documento (p.ej. "Contrato", "Presupuesto").</summary>
        public string DocumentLabel { get; set; } = null!;

        /// <summary>Texto de observación tal como fue escrito por el revisor.</summary>
        public string? Observation { get; set; }
    }
}
