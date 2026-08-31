namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta
{
    /// <summary>
    /// Aviso que se muestra al Administrador/Coordinador SSOMA de un proyecto cuando ingresa al
    /// sistema: interconsultas pendientes y EMOs vencidos de los trabajadores actualmente en SUS
    /// proyectos (los que tiene cargados como <c>EmailCoordAdmin</c>/<c>EmailCoordSsoma</c>).
    /// Se calcula en vivo en cada login — no se persiste ni se repite por cron.
    /// </summary>
    public class AlertaLoginSsomaResultDto
    {
        public bool TieneAlertas { get; set; }
        public List<AlertaLoginProyectoDto> Proyectos { get; set; } = new();
    }

    public class AlertaLoginProyectoDto
    {
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public List<AlertaLoginItemDto> Interconsultas { get; set; } = new();
        public List<AlertaLoginItemDto> EmosVencidos { get; set; } = new();
    }

    public class AlertaLoginItemDto
    {
        public string WorkerNombre { get; set; } = string.Empty;
        public string? RazonSocial { get; set; }
        /// <summary>Días de retraso (interconsulta) o días vencido (EMO).</summary>
        public int Dias { get; set; }
    }
}
