namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Registra que ya se envió el recordatorio de agenda de una reunión, para que el job
    /// (disparado periódicamente por un cron externo) no lo reenvíe en corridas posteriores.
    /// </summary>
    public class ReunionRecordatorioLog
    {
        public int ReunionRecordatorioLogId { get; set; }
        public int ReunionId { get; set; }
        public DateTime EnviadoDateTime { get; set; }
    }
}
