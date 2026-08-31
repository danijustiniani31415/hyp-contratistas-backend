namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    /// <summary>Paso 5 — Oficina Central envía las observaciones de la llegada a Oficina Técnica.</summary>
    public class SendStep5ObservationsEmailDto
    {
        public string GraphAccessToken { get; set; } = null!;
        public string? Observation { get; set; }
    }

    /// <summary>Paso 5 — Oficina Técnica notifica a Oficina Central que las observaciones fueron levantadas.</summary>
    public class SendStep5LevantamientoEmailDto
    {
        public string GraphAccessToken { get; set; } = null!;
        public string? Message { get; set; }
    }

    /// <summary>Datos para los correos del paso 5 (observaciones / levantamiento).</summary>
    public class Step5EmailDataDto
    {
        public string ProjectDescription  { get; set; } = null!;
        public string ContributorName     { get; set; } = null!;
        public string WorkItemDescription { get; set; } = null!;
        public string? ArrivalObservation { get; set; }
        /// <summary>Correos de tipo "Oficina Técnica" (StaffProjectEmailType = 3) del proyecto.</summary>
        public List<string> OficinaTecnicaEmails { get; set; } = new();
        /// <summary>Correos de tipo "Oficina central" (StaffProjectEmailType = 2) del proyecto.</summary>
        public List<string> OficinaCentralEmails { get; set; } = new();
    }
}
